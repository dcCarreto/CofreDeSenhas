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
        private async void AuditarCofre_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhasAtuais.Count == 0)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Message.AuditNoPasswords"),
                    Idioma.Texto("Message.AuditTitle"));
                return;
            }

            var conteudoOriginal = BtnAuditoria.Content;
            BtnAuditoria.IsEnabled = false;
            BtnAuditoria.Content = "…";

            try
            {
                var resultado = ExecutarAuditoria();
                FiltrarSenhas(reordenar: false);
                AtualizarContador();

                await CaixaMensagem.MostrarAsync(this, MontarMensagemAuditoria(resultado), Idioma.Texto("Message.AuditTitle"),
                    resultado.TotalComAchados == 0 ? TipoMensagem.Info : TipoMensagem.Aviso);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.AuditError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
            finally
            {
                BtnAuditoria.Content = conteudoOriginal;
                BtnAuditoria.IsEnabled = true;
            }
        }

        private ResultadoAuditoriaCofre ExecutarAuditoria()
        {
            var resultado = _servicoAuditoria.Auditar(_senhasAtuais, ObterSenhaPlain);
            _resultadoAuditoria = resultado;
            _itensAuditoria.Clear();
            foreach (var item in resultado.Itens)
                _itensAuditoria[item.Senha.Id] = item;

            return resultado;
        }

        private void LimparAuditoria()
        {
            _resultadoAuditoria = null;
            _itensAuditoria.Clear();
            _vazamentosPorId.Clear();
            _filtroSeguranca = null;
            AtualizarChipFiltroSeguranca();
        }

        private static string MontarMensagemAuditoria(ResultadoAuditoriaCofre resultado)
        {
            if (resultado.TotalComAchados == 0)
            {
                var msg = Idioma.Formatar("Message.AuditSuccess", resultado.TotalSenhas);
                if (resultado.NaoAuditadas > 0)
                    msg += "\n" + Idioma.Formatar("Message.AuditIncomplete", resultado.NaoAuditadas);
                return msg;
            }

            var linhas = new List<string>
            {
                Idioma.Formatar("Message.AuditFoundHeader", resultado.TotalComAchados, resultado.TotalSenhas),
                Idioma.Formatar("Message.AuditWeakLine", resultado.TotalFracas),
                Idioma.Formatar("Message.AuditRepeatedLine", resultado.TotalRepetidas),
                Idioma.Formatar("Message.AuditOldLine", resultado.TotalAntigas)
            };

            if (resultado.NaoAuditadas > 0)
                linhas.Add(Idioma.Formatar("Message.AuditUnreadableLine", resultado.NaoAuditadas));

            linhas.Add("");
            linhas.Add(Idioma.Texto("Message.AuditMarked"));
            linhas.Add(Idioma.Formatar("Message.AuditOldDefinition", ServicoAuditoriaSenha.DiasSenhaAntigaPadrao));

            return string.Join(Environment.NewLine, linhas);
        }

        private async void VerificarVazamentos_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhasAtuais.Count == 0)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Message.BreachNoPasswords"),
                    Idioma.Texto("Message.BreachTitle"));
                return;
            }

            var conteudoOriginal = BtnVazamentos.Content;
            BtnVazamentos.IsEnabled = false;
            BtnVazamentos.Content = "…";

            try
            {
                var (verificadas, comprometidas) = await VerificarVazamentosDoVaultAsync();

                string msg = comprometidas == 0
                    ? Idioma.Formatar("Message.BreachSuccess", verificadas)
                    : Idioma.Formatar("Message.BreachWarning", comprometidas, verificadas);

                await CaixaMensagem.MostrarAsync(this, msg, Idioma.Texto("Message.BreachDoneTitle"),
                    comprometidas == 0 ? TipoMensagem.Info : TipoMensagem.Aviso);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.BreachNetworkError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Message.NetworkErrorTitle"), TipoMensagem.Erro);
            }
            finally
            {
                BtnVazamentos.Content = conteudoOriginal;
                BtnVazamentos.IsEnabled = true;
            }
        }

        private async void RelatorioSeguranca_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhasAtuais.Count == 0)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Message.AuditNoPasswords"),
                    Idioma.Texto("SecurityReport.Title"));
                return;
            }

            try
            {
                ExecutarAuditoria();
                var relatorio = ServicoRelatorioSeguranca.Gerar(_senhasAtuais, _resultadoAuditoria!, _vazamentosPorId,
                    CertificadoBancoNaoExigido());
                bool jaVerificouVazamentos = _vazamentosPorId.Count > 0;

                var dlg = new JanelaRelatorioSeguranca(relatorio, jaVerificouVazamentos, GerarRelatorioAtualizadoAsync);
                await AbrirDialogoAsync<bool>(dlg);

                if (dlg.CategoriaSelecionada is { } categoria)
                {
                    SairDaLixeira();
                    _somenteFavoritos = false;
                    _somenteRecentes = false;
                    _filtroSeguranca = categoria;
                    PintarFiltroFavoritos();
                    AtualizarNavegacao();
                }

                AtualizarChipFiltroSeguranca();
                FiltrarSenhas();
                AtualizarContador();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.AuditError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async Task<RelatorioSegurancaCofre> GerarRelatorioAtualizadoAsync()
        {
            await VerificarVazamentosDoVaultAsync();
            return ServicoRelatorioSeguranca.Gerar(_senhasAtuais, _resultadoAuditoria!, _vazamentosPorId,
                CertificadoBancoNaoExigido());
        }

        private static bool CertificadoBancoNaoExigido() =>
            Preferencias.UltimoBanco is { Conectado: true, ExigirCertificadoValido: false };

        private async Task<(int Verificadas, int Comprometidas)> VerificarVazamentosDoVaultAsync()
        {
            int verificadas = 0;
            int comprometidas = 0;

            foreach (var senha in _senhasAtuais)
            {
                var plain = ObterSenhaPlain(senha);
                if (string.IsNullOrEmpty(plain)) continue;

                int contagem = await _servicoVazamento.VerificarAsync(plain);
                _vazamentosPorId[senha.Id] = contagem;
                if (contagem > 0) comprometidas++;
                verificadas++;
            }

            foreach (var linha in _linhasSenha)
                if (_vazamentosPorId.TryGetValue(linha.Senha.Id, out var contagem))
                    linha.Vazamentos = contagem;

            return (verificadas, comprometidas);
        }
    }
}
