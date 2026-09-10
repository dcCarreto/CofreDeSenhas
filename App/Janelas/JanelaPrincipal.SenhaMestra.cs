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
        private async void AlterarSenhaMestra_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new JanelaAlterarSenhaMestra();
            if (!await AbrirDialogoAsync<bool>(dlg))
                return;

            // A senha do servidor de banco (se houver) está cifrada com a chave atual —
            // precisa ser decifrada antes da troca e recifrada com a chave nova depois,
            // senão a reconexão automática passa a falhar silenciosamente no próximo
            // login (MontarConexaoDoPerfil não consegue mais decifrá-la).
            string? senhaServidorPlano = null;
            try
            {
                if (_criptografia != null && !string.IsNullOrEmpty(Preferencias.UltimoBanco?.SenhaCifrada))
                    senhaServidorPlano = _criptografia.Descriptografar(Preferencias.UltimoBanco.SenhaCifrada);
            }
            catch { }

            // Reconcilia com a pasta de sincronização compartilhada usando a chave
            // ainda antiga, antes dela deixar de bater com a senha nova — evita que a
            // republicação feita mais abaixo (já com a chave nova) sobrescreva o que
            // outro dispositivo tenha colocado lá desde a última sincronização.
            await SincronizarAsync(silencioso: true);

            var servico = new ServicoMudancaSenhaMestra();
            byte[] chaveNova;
            try
            {
                chaveNova = await servico.AlterarAsync(dlg.SenhaAtual, dlg.NovaSenha);
            }
            catch (ErroLocalizavel ex)
            {
                await CaixaMensagem.MostrarAsync(this, ErrosUi.MensagemAmigavel(ex), Idioma.Texto("Master.ChangeTitle"), TipoMensagem.Aviso);
                return;
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Master.ChangeError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
                return;
            }

            // A troca já terminou com sucesso — auth.dat/vault (e a pasta de sync, mais
            // abaixo) já estão na chave nova, mas _servicoSenha/_servicoSincronizacao
            // continuam vinculados à chave ANTIGA até o restart. Qualquer gravação no
            // cofre nesse intervalo — o ciclo de sync automático, ou uma ação manual do
            // usuário enquanto o QrBackup e o aviso de reinício esperam a decisão dele
            // sem prazo — regrava senhas.json.enc com a chave antiga por cima do que
            // acabou de ser salvo com a nova, deixando o cofre com metades em chaves
            // diferentes e ilegível depois do restart. Para o timer e congela a janela
            // até Reiniciar() encerrar o processo; nada disso precisa voltar depois.
            _timerSincronizacao.Stop();
            DesabilitarInteracaoAteReiniciar();

            if (senhaServidorPlano != null && Preferencias.UltimoBanco != null)
            {
                Preferencias.UltimoBanco.SenhaCifrada = new ServicoCriptografia(chaveNova).Criptografar(senhaServidorPlano);
                Preferencias.Salvar();
            }

            var afetaOutrosDispositivos = await RepublicarAposTrocaDeSenhaMestraAsync(chaveNova, dlg.NovaSenha, senhaServidorPlano);

            var biometriaEstavaHabilitada = _biometria.EstaHabilitado;
            await _biometria.DesabilitarAsync();
            await QrBackup.OferecerSalvarAsync(this, dlg.NovaSenha);
            await RecuperacaoUi.RenovarAposTrocaDeSenhaAsync(this, chaveNova);

            var mensagem = Idioma.Texto("Master.ChangedRestart");
            if (biometriaEstavaHabilitada)
                mensagem += "\n\n" + Idioma.Texto("Biometric.DisabledAfterMasterChange");
            if (servico.UltimosAvisos.Count > 0)
                mensagem += "\n\n" + Idioma.Texto("Master.ItemsDiscardedWarning");
            if (afetaOutrosDispositivos)
                mensagem += "\n\n" + Idioma.Texto("Master.OtherDevicesWarning");

            await CaixaMensagem.MostrarAsync(this,
                mensagem,
                Idioma.Texto("Master.ChangeTitle"));
            Reiniciar();
        }

        // internal (em vez de private) só para expor um seam de teste direto sem
        // precisar dirigir o diálogo JanelaAlterarSenhaMestra nem os efeitos colaterais
        // de UI (QR code, biometria, reinício) que o resto do fluxo dispara — ver
        // App.Testes (InternalsVisibleTo), mesmo padrão já usado em ConectarAsync.
        //
        // Sem isto, o banco conectado e a pasta de sincronização continuam com o
        // conteúdo (e o hmac, no caso do banco) cifrados com a chave antiga — nem este
        // dispositivo consegue lê-los de volta depois de reiniciar com a senha nova, e
        // "Restaurar de um banco de dados" fica travado no salt/verificador antigos pra
        // sempre (RepositorioSenhaEspelhado só concilia dados, nunca a chave de
        // cifragem usada pra gravá-los). Retorna true se o cofre está conectado a um
        // banco ou pasta compartilhada com outros dispositivos, que precisam trocar a
        // senha mestra também para continuar lendo o que este dispositivo publicar.
        internal async Task<bool> RepublicarAposTrocaDeSenhaMestraAsync(byte[] chaveNova, string novaSenhaPlano, string? senhaServidorPlano)
        {
            var afetaOutrosDispositivos = _conectadoAoBanco || Preferencias.Sincronizacao != null;

            if (_conectadoAoBanco && Preferencias.UltimoBanco is { } perfilBanco && _criptografia != null)
            {
                try
                {
                    var criptografiaNova = new ServicoCriptografia(chaveNova);
                    var cfgNova = new ConexaoBanco
                    {
                        Tipo = perfilBanco.Tipo,
                        Host = perfilBanco.Host,
                        Porta = perfilBanco.Porta,
                        Banco = perfilBanco.Banco,
                        Usuario = perfilBanco.Usuario,
                        SenhaServidor = senhaServidorPlano,
                        ExigirCertificadoValido = perfilBanco.ExigirCertificadoValido,
                        ExigirIntegridade = perfilBanco.ExigirIntegridade
                    };

                    var todasAtuais = (await _servicoSenha.ListarTodosAsync()).Concat(await _servicoSenha.ListarLixeiraAsync());
                    var itensRecifrados = todasAtuais
                        .Select(s => RecifrarComNovaChave(s, criptografiaNova))
                        .Where(s => s != null)
                        .Cast<Senha>();

                    var repoBancoNovo = new RepositorioSenhaBanco(cfgNova, criptografiaNova);
                    await repoBancoNovo.GravarVariasPorChaveAsync(itensRecifrados);

                    if (new AutenticacaoMestra().TentarLerParametros(out var salt, out var verificador, out var kdf, out var custo, out var memoriaKb, out var paralelismo))
                        await new ServicoBancoDados().PublicarAuthAsync(cfgNova, new AuthBanco(salt, verificador, kdf, custo, memoriaKb, paralelismo));
                }
                catch
                {
                    // Melhor esforço: se a republicação falhar, o cofre local já está
                    // trocado e funcional; a próxima reconexão manual ou sincronização
                    // tenta de novo, e o usuário já é avisado sobre os outros
                    // dispositivos por quem chama este método.
                }
            }

            if (Preferencias.Sincronizacao is { } perfilSync)
            {
                try
                {
                    var saltSync = Convert.FromBase64String(perfilSync.Salt);
                    var chaveSyncNova = ServicoSincronizacao.DerivarChave(novaSenhaPlano, saltSync, perfilSync.Kdf,
                        perfilSync.Iteracoes, perfilSync.MemoriaKb, perfilSync.Paralelismo);
                    var servicoSyncNovo = new ServicoSincronizacao(new ServicoCriptografia(chaveSyncNova));

                    var caminho = Path.Combine(perfilSync.Pasta, ServicoSincronizacao.NomeArquivo);
                    await servicoSyncNovo.EscreverAsync(caminho, saltSync, perfilSync.Kdf, perfilSync.Iteracoes,
                        perfilSync.MemoriaKb, perfilSync.Paralelismo, await ConstruirListaExportavelAsync());
                }
                catch
                {
                    // Melhor esforço, mesmo raciocínio do bloco do banco acima.
                }
            }

            return afetaOutrosDispositivos;
        }

        private async void RegerarQrCode_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new JanelaConfirmarSenhaMestra();
            if (!await AbrirDialogoAsync<bool>(dlg))
                return;

            await QrBackup.OferecerSalvarAsync(this, dlg.SenhaConfirmada);
        }

        private async void AtivarOuGerarChaveRecuperacao()
        {
            // Re-auth com a senha mestra antes de expor / rotacionar o segredo.
            var dlg = new JanelaConfirmarSenhaMestra(
                Idioma.Texto("Recovery.SettingsRow"),
                Idioma.Texto("Recovery.RecoverInstruction"),
                Idioma.Texto("Recovery.Enable"));
            if (!await AbrirDialogoAsync<bool>(dlg))
                return;

            var segredo = new ServicoRecuperacao().Habilitar(_chaveMestra);
            await CaixaMensagem.MostrarAsync(this, Idioma.Texto("Recovery.Saved"),
                Idioma.Texto("Recovery.SettingsRow"), TipoMensagem.Info);
            await RecuperacaoUi.MostrarChaveAsync(this, segredo);
        }

        private async void DesativarChaveRecuperacao()
        {
            var confirmou = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Texto("Recovery.DisableConfirm"),
                Idioma.Texto("Recovery.SettingsRow"), TipoMensagem.Aviso);
            if (confirmou)
                new ServicoRecuperacao().Desabilitar();
        }

        // internal só pra teste chamar direto sem precisar abrir o MenuFlyout de
        // configurações (os itens dele só existem na árvore visual depois de aberto de
        // verdade) — ver App.Testes (InternalsVisibleTo).
        internal async void LimparCofre_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhasAtuais.Count == 0)
                return;

            if (_criptografia == null)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Db.FeatureUnavailable"), Idioma.Texto("Vault.ClearTitle"), TipoMensagem.Aviso);
                return;
            }

            // Mesma reautenticação que Excluir Cofre já exige — sem isto, "Limpar
            // cofre" bastava um clique de confirmação, sem senha nenhuma, pra esvaziar
            // o cofre inteiro pra lixeira numa sessão já desbloqueada e sem vigilância.
            var dlgSenha = new JanelaConfirmarSenhaMestra(
                Idioma.Texto("Vault.ClearTitle"),
                Idioma.Texto("Vault.DeleteReauthInstruction"),
                Idioma.Texto("Vault.DeleteReauthButton"));
            if (!await AbrirDialogoAsync<bool>(dlgSenha))
                return;

            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Texto("Vault.ClearConfirm"),
                Idioma.Texto("Vault.ClearTitle"), TipoMensagem.Aviso);

            if (!confirmar)
                return;

            try
            {
                await _servicoSenha.LimparCofreAsync();
                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Vault.ClearError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async void ExcluirCofre_Click(object? sender, RoutedEventArgs e)
        {
            if (_criptografia == null)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Db.FeatureUnavailable"), Idioma.Texto("Vault.DeleteTitle"), TipoMensagem.Aviso);
                return;
            }

            var dlg = new JanelaConfirmarSenhaMestra(
                Idioma.Texto("Vault.DeleteTitle"),
                Idioma.Texto("Vault.DeleteReauthInstruction"),
                Idioma.Texto("Vault.DeleteReauthButton"));
            if (!await AbrirDialogoAsync<bool>(dlg))
                return;

            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Texto("Vault.DeleteConfirm"),
                Idioma.Texto("Vault.DeleteTitle"), TipoMensagem.Aviso);
            if (!confirmar)
                return;

            try
            {
                await _biometria.DesabilitarAsync();
                _servicoAnexos?.ApagarTudo();

                var persistencia = new PersistenciaLocal(_criptografia);
                await persistencia.ApagarTudoAsync();

                var authPadrao = new AutenticacaoMestra();
                authPadrao.ExcluirSenhaMestra();
                new ControleTentativasLogin(authPadrao.PastaApp).Limpar();
                HistoricoPontuacaoSeguranca.Limpar();

                Preferencias.UltimoBanco = null;
                Preferencias.Sincronizacao = null;
                Preferencias.Salvar();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Vault.DeleteError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
                return;
            }

            await CaixaMensagem.MostrarAsync(this,
                Idioma.Texto("Vault.DeletedRestart"),
                Idioma.Texto("Vault.DeleteTitle"));
            Reiniciar();
        }

    }
}
