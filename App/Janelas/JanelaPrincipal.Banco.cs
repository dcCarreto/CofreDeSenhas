using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CofreDeSenhas.Controles;
using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaPrincipal
    {
        private async void ConectarBanco_Click(object? sender, RoutedEventArgs e)
        {
            if (_criptografia == null)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Db.FeatureUnavailable"), Idioma.Texto("Db.SelectTitle"), TipoMensagem.Aviso);
                return;
            }

            var seletor = new JanelaSelecionarBanco();
            if (!await AbrirDialogoAsync<bool>(seletor) || seletor.Selecionado is not { } tipo)
                return;

            var dlg = new JanelaConexaoBanco(tipo);
            if (!await AbrirDialogoAsync<bool>(dlg) || dlg.Conexao is not { } cfg)
                return;

            await ConectarAsync(cfg, persistir: true, silencioso: false);
        }

        // internal (não private) só pra permitir testar a orquestração de conexão sem
        // precisar dirigir os dois diálogos (JanelaSelecionarBanco/JanelaConexaoBanco)
        // que normalmente ficam na frente dela — ver App.Testes (InternalsVisibleTo).
        internal Task ConectarAsync(ConexaoBanco cfg, bool persistir, bool silencioso)
        {
            var minhaGeracao = ++_geracaoConexao;
            var minhaTarefa = ConectarAposAsync(_tarefaConexaoAtual, cfg, persistir, silencioso, minhaGeracao);
            _tarefaConexaoAtual = minhaTarefa;
            return minhaTarefa;
        }

        private async Task ConectarAposAsync(Task tarefaAnterior, ConexaoBanco cfg, bool persistir, bool silencioso, int minhaGeracao)
        {
            // Uma falha da tentativa anterior (incluindo, em tese, o próprio diálogo de
            // erro dela) não pode propagar aqui — senão essa nova tarefa também fica
            // faltada, e como _tarefaConexaoAtual nunca é resetada, toda tentativa futura
            // de conexão ficaria permanentemente travada reencontrando o erro antigo.
            try { await tarefaAnterior; } catch { }

            try
            {
                var repoBanco = new RepositorioSenhaBanco(cfg, _criptografia);
                var espelho = _repositorioLocal != null
                    ? new RepositorioSenhaEspelhado(_repositorioLocal, repoBanco,
                        reconciliacaoJaRealizada: Preferencias.UltimoBanco?.ReconciliacaoInicialConcluida ?? false)
                    : null;
                IRepositorioSenha repoAtivo = (IRepositorioSenha?)espelho ?? repoBanco;
                var servico = new ServicoSenha(repoAtivo, _criptografia!);

                await servico.ListarTodosAsync();
                await PublicarAuthNoBancoSeNecessarioAsync(cfg);

                // Enquanto os awaits acima estavam em voo, o usuário pode ter clicado
                // "Desconectar" ou iniciado outra tentativa de conexão — essa é a mais
                // recente e deve prevalecer; aplicar o resultado desta reconectaria o
                // cofre contra a vontade mais atual do usuário.
                if (minhaGeracao != _geracaoConexao)
                    return;

                _servicoSenha = servico;
                _repositorioEspelhado = espelho;
                _conectadoAoBanco = true;

                if (persistir)
                {
                    Preferencias.UltimoBanco = new PerfilBanco
                    {
                        Tipo = cfg.Tipo,
                        Host = cfg.Host,
                        Porta = cfg.Porta,
                        Banco = cfg.Banco,
                        Usuario = cfg.Usuario,
                        SenhaCifrada = cfg.Tipo == TipoBanco.SQLite || string.IsNullOrEmpty(cfg.SenhaServidor)
                            ? null
                            : _criptografia!.Criptografar(cfg.SenhaServidor),
                        Conectado = true,
                        ReconciliacaoInicialConcluida = espelho?.ReconciliacaoRealizadaNestaSessao == true,
                        ExigirCertificadoValido = cfg.ExigirCertificadoValido,
                        ExigirIntegridade = cfg.ExigirIntegridade
                    };
                    Preferencias.Salvar();
                }
                else if (espelho?.ReconciliacaoRealizadaNestaSessao == true && Preferencias.UltimoBanco != null)
                {
                    Preferencias.UltimoBanco.ReconciliacaoInicialConcluida = true;
                    Preferencias.Salvar();
                }

                AtualizarEstadoConexao(cfg.Descricao);

                // Sem isto, um conflito de sincronização (em especial integridade
                // violada — possível adulteração do banco compartilhado) só existia na
                // lista em memória de UltimosConflitos: se o usuário não abrisse a tela
                // de conflitos antes de reconectar ou fechar o app, o registro sumia
                // pra sempre sem deixar rastro nenhum pra revisar depois.
                foreach (var conflito in espelho?.UltimosConflitos ?? Array.Empty<ConflitoSincronizacao>())
                    Diagnostico.Registrar(
                        $"{conflito.Tipo} em \"{conflito.NomeServico}\" (id {conflito.SenhaId})",
                        "ConflitoSincronizacao");

                await CarregarSenhasAsync();

                if (!silencioso)
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Formatar("Db.ConnectedMessage", cfg.Descricao),
                        Idioma.Texto("Db.Database"));
            }
            catch (Exception ex)
            {
                // Mesmo raciocínio do "return" acima: se o usuário já desconectou ou
                // começou outra tentativa, nem o estado nem um diálogo de erro fazem
                // sentido pra uma conexão que ele já abandonou.
                if (minhaGeracao != _geracaoConexao)
                    return;

                _servicoSenha = _servicoSenhaLocal;
                _repositorioEspelhado = null;
                _conectadoAoBanco = false;
                AtualizarEstadoConexao(null, falhaReconexao: silencioso);

                if (!silencioso)
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Formatar("Db.ConnectError", ErrosUi.MensagemAmigavel(ex)),
                        Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private static async Task PublicarAuthNoBancoSeNecessarioAsync(ConexaoBanco cfg)
        {
            try
            {
                var bd = new ServicoBancoDados();
                if (await bd.TabelaAuthExisteAsync(cfg))
                    return;

                if (!new AutenticacaoMestra().TentarLerParametros(out var salt, out var verificador, out var kdf, out var custo, out var memoriaKb, out var paralelismo))
                    return;

                await bd.CriarTabelaAuthAsync(cfg);
                await bd.PublicarAuthAsync(cfg, new AuthBanco(salt, verificador, kdf, custo, memoriaKb, paralelismo));
            }
            catch
            {
                // Melhor esforço: se a publicação falhar, a conexão/espelhamento normal
                // continua funcionando do mesmo jeito de sempre, só a restauração a
                // partir deste banco fica indisponível até uma tentativa futura funcionar.
            }
        }

        private ConexaoBanco? MontarConexaoDoPerfil(PerfilBanco perfil)
        {
            var cfg = new ConexaoBanco
            {
                Tipo = perfil.Tipo,
                Host = perfil.Host,
                Porta = perfil.Porta,
                Banco = perfil.Banco,
                Usuario = perfil.Usuario,
                ExigirCertificadoValido = perfil.ExigirCertificadoValido,
                ExigirIntegridade = perfil.ExigirIntegridade
            };

            if (!string.IsNullOrEmpty(perfil.SenhaCifrada))
            {
                try { cfg.SenhaServidor = _criptografia!.Descriptografar(perfil.SenhaCifrada); }
                catch { return null; }
            }

            return cfg;
        }

        private async void DesconectarBanco_Click(object? sender, RoutedEventArgs e)
        {
            // Invalida qualquer tentativa de conexão ainda em voo (ver
            // ConectarAposAsync) — sem isto, uma conexão iniciada antes de
            // "Desconectar" podia terminar depois e reconectar o cofre por cima
            // desta escolha, que é a mais recente.
            _geracaoConexao++;

            _servicoSenha = _servicoSenhaLocal;
            _repositorioEspelhado = null;
            _conectadoAoBanco = false;

            if (Preferencias.UltimoBanco != null)
            {
                Preferencias.UltimoBanco.Conectado = false;
                Preferencias.UltimoBanco.SenhaCifrada = null;
                Preferencias.Salvar();
            }

            AtualizarEstadoConexao(null);
            await CarregarSenhasAsync();
        }

        private void AtualizarEstadoConexao(string? descricao, bool falhaReconexao = false)
        {
            _descricaoConexaoAtual = descricao;
            _falhaReconexaoAtual = falhaReconexao;

            string conexao;
            if (_conectadoAoBanco && descricao != null)
            {
                conexao = Idioma.Formatar("Vault.Connection.Connected", descricao);
                PontoConexao.Fill = Tema.Pincel(Tema.StatusConnected);
            }
            else if (falhaReconexao)
            {
                conexao = Idioma.Texto("Vault.Connection.DatabaseUnavailable");
                PontoConexao.Fill = Tema.Pincel(Tema.StatusWarning);
            }
            else
            {
                conexao = Idioma.Texto("Vault.Connection.Local");
                PontoConexao.Fill = Tema.Pincel(Tema.StatusLocal);
            }

            LblConexao.Text = TextoBloqueioAutomatico();
            ToolTip.SetTip(LblConexao, conexao);

            AutomationProperties.SetName(LblConexao,
                $"{LblConexao.Text}. {Idioma.Texto("A11y.ConnectionStatus")}: {conexao}");

            BtnConflitosSincronizacao.IsVisible = (_repositorioEspelhado?.UltimosConflitos.Count ?? 0) > 0;
        }
    }
}
