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
        private async void Linha_SolicitouDetalhes(object? sender, Senha senha)
        {
            // Mesma trava de Salvar/Fechar/Excluir — sem ela, clicar em outra linha
            // enquanto um Salvar/Excluir do item atual ainda está em voo troca o
            // painel pro novo item sem bloqueio nenhum (a lista continua totalmente
            // interativa durante esse voo, só os botões do próprio painel são
            // desabilitados). Quando a operação original terminar, ela reabre/fecha o
            // painel por cima do que o usuário já estava vendo/editando no novo item.
            if (_detalhesOperacaoEmAndamento)
                return;

            if (!await ConfirmarDescarteDetalhesAsync())
                return;
            AbrirDetalhes(senha);
        }

        // internal só pra teste abrir o painel diretamente sem precisar simular o
        // clique na linha da lista — ver App.Testes (InternalsVisibleTo).
        internal void AbrirDetalhes(Senha senha)
        {
            if (_modoPrivacidade)
                return;

            _senhaDetalhe = senha;
            _senhaDetalhePlain = ObterSenhaPlain(senha) ?? "";
            _senhaDetalheOriginal = _senhaDetalhePlain;
            _senhaDetalheDataAtualizacaoAoAbrir = senha.DataAtualizacao;
            _senhaDetalheVisivel = false;

            TxtDetalheServico.Text = senha.NomeServico;
            TxtDetalheUsuario.Text = senha.Usuario;
            TxtDetalheUrl.Text = senha.Url ?? "";
            TxtDetalheNotas.Text = senha.Notas ?? "";

            LblDetalheUsuario.Text = TemplatesCredencial.RotuloUsuario(senha.Tipo);
            LblDetalheSenha.Text = TemplatesCredencial.RotuloSenha(senha.Tipo);
            AutomationProperties.SetName(TxtDetalheUsuario, LblDetalheUsuario.Text);
            AutomationProperties.SetName(TxtDetalheSenha, LblDetalheSenha.Text);

            TxtDetalheEtiquetas.Text = Etiquetas.Formatar(senha.Etiquetas);
            CmbDetalheCategoria.ItemsSource = CategoriasUI.Rotulos;
            CmbDetalheCategoria.SelectedIndex = (int)senha.Categoria;

            AtualizarDetalheVisual();
            AtualizarHistoricoDetalhes();
            AtualizarSenhaDetalhe();
            AtualizarTotpDetalhes();
            ExibirPainel(PainelDetalhes);

            _snapshotDetalhes = (TxtDetalheServico.Text ?? "", TxtDetalheUsuario.Text ?? "",
                TxtDetalheUrl.Text ?? "", TxtDetalheNotas.Text ?? "", TxtDetalheEtiquetas.Text ?? "",
                CmbDetalheCategoria.SelectedIndex);
        }

        // internal só pra teste checar o resultado direto, sem depender do diálogo de
        // confirmação real — ver App.Testes (InternalsVisibleTo).
        internal bool DetalhesTemAlteracoesNaoSalvas()
        {
            if (_snapshotDetalhes is not { } s)
                return false;

            if (TxtDetalheServico.Text != s.Servico || TxtDetalheUsuario.Text != s.Usuario ||
                TxtDetalheUrl.Text != s.Url || TxtDetalheNotas.Text != s.Notas ||
                TxtDetalheEtiquetas.Text != s.Etiquetas || CmbDetalheCategoria.SelectedIndex != s.Categoria)
                return true;

            // Compara contra a baseline imutável, não contra _senhaDetalhePlain (que
            // RevelarSenhaDetalhes_Click atualiza a cada ocultada) — assim uma edição
            // feita enquanto a senha estava visível continua detectável mesmo depois
            // de ocultá-la de novo, sem precisar que o campo esteja visível agora.
            var senhaAtual = _senhaDetalheVisivel ? (TxtDetalheSenha.Text ?? "") : _senhaDetalhePlain;
            return senhaAtual != _senhaDetalheOriginal;
        }

        // internal só pra teste checar a detecção direto, sem depender do diálogo de
        // confirmação real — mesmo padrão de DetalhesTemAlteracoesNaoSalvas.
        internal bool DetalhesTemAlteracaoConcorrente()
        {
            if (_senhaDetalhe == null)
                return false;

            var atual = _senhasAtuais.Concat(_itensLixeira).FirstOrDefault(s => s.Id == _senhaDetalhe.Id);
            return atual != null && atual.DataAtualizacao != _senhaDetalheDataAtualizacaoAoAbrir;
        }

        private async Task<bool> ConfirmarDescarteDetalhesAsync()
        {
            if (!DetalhesTemAlteracoesNaoSalvas())
                return true;

            return await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Texto("Entry.Detail.DiscardChangesConfirm"),
                Idioma.Texto("Entry.Detail.DiscardChangesTitle"), TipoMensagem.Aviso);
        }

        private void AtualizarHistoricoDetalhes()
        {
            if (_senhaDetalhe == null)
                return;

            LblDetalheCriada.Text = Idioma.Formatar("Entry.Usage.Created", FormatarDataDetalhe(_senhaDetalhe.DataCriacao));
            LblDetalheAtualizada.Text = Idioma.Formatar("Entry.Usage.Updated", FormatarDataDetalhe(_senhaDetalhe.DataAtualizacao));
            LblDetalheCopiaSenha.Text = Idioma.Formatar("Entry.Usage.CopyPasswordLabel", FormatarDataOuNunca(_senhaDetalhe.DataUltimaCopiaSenha));
            LblDetalheCopiaUsuario.Text = Idioma.Formatar("Entry.Usage.CopyUserLabel", FormatarDataOuNunca(_senhaDetalhe.DataUltimaCopiaUsuario));
            LblDetalheCopiaTotp.Text = Idioma.Formatar("Entry.Usage.CopyTotpLabel", FormatarDataOuNunca(_senhaDetalhe.DataUltimaCopiaTotp));
            LblDetalheCopiaTotp.IsVisible = _senhaDetalhe.TotpSegredo != null;
        }

        private static string FormatarDataDetalhe(DateTime data) =>
            data.ToLocalTime().ToString("dd MMM yyyy", Idioma.CulturaAtual);

        private static string FormatarDataOuNunca(DateTime? data) =>
            data.HasValue ? FormatarDataDetalhe(data.Value) : Idioma.Texto("Entry.Usage.Never");

        private void AtualizarDetalheVisual()
        {
            if (_senhaDetalhe == null || AvatarDetalhe == null)
                return;

            var icone = IconesServico.Obter(TxtDetalheServico.Text ?? _senhaDetalhe.NomeServico, TxtDetalheUrl.Text);
            AvatarDetalhe.Background = Tema.Pincel(icone.Fundo);
            TxtAvatarDetalhe.Text = icone.Texto;
            TxtAvatarDetalhe.Foreground = Tema.Pincel(icone.Frente);
            ToolTip.SetTip(AvatarDetalhe, TxtDetalheServico.Text ?? _senhaDetalhe.NomeServico);

            var (bg, fg) = Acessibilidade.CoresCategoria(_senhaDetalhe.Categoria);
            BadgeDetalheCategoria.Background = Tema.Pincel(bg);
            TxtDetalheCategoria.Foreground = Tema.Pincel(fg);
            TxtDetalheCategoria.Text = _senhaDetalhe.Categoria == Categoria.Other && _senhaDetalhe.Etiquetas.Count > 0
                ? string.Join(", ", _senhaDetalhe.Etiquetas)
                : CategoriasUI.Rotulo(_senhaDetalhe.Categoria);
        }

        private void AtualizarSenhaDetalhe()
        {
            TxtDetalheSenha.Text = _senhaDetalheVisivel
                ? _senhaDetalhePlain
                : new string('•', Math.Max(8, _senhaDetalhePlain.Length));
            TxtDetalheSenha.IsReadOnly = !_senhaDetalheVisivel;
            BtnDetalheRevelar.Content = Recursos.ImagemIcone(_senhaDetalheVisivel ? "IconeOcultar" : "IconeRevelar", 22);
            ToolTip.SetTip(BtnDetalheRevelar, Idioma.Texto(_senhaDetalheVisivel ? "Row.HidePassword" : "Row.RevealPassword"));
        }

        private void AtualizarTotpDetalhes()
        {
            var segredo = _senhaDetalhe != null ? ObterTotpPlain(_senhaDetalhe) : null;
            if (string.IsNullOrEmpty(segredo) || !_totp.SegredoValido(segredo))
            {
                PainelDetalheTotp.IsVisible = false;
                _timerTotpDetalhe.Parar();
                return;
            }

            try
            {
                var codigo = _totp.Gerar(segredo);
                LblDetalheCodigoTotp.Text = TotpPreview.FormatarCodigo(codigo.Codigo);
                var contagem = Idioma.Formatar("Entry.TotpExpiresIn", codigo.SegundosRestantes);
                AnelDetalheTotp.Data = TotpPreview.ConstruirAnelProgresso(codigo.SegundosRestantes, PeriodoTotpDetalhe, raio: 9, centro: 12);
                AutomationProperties.SetName(LblDetalheCodigoTotp,
                    $"{Idioma.Texto("A11y.TotpPreview")}: {LblDetalheCodigoTotp.Text}. {contagem}");
                PainelDetalheTotp.IsVisible = true;
                _timerTotpDetalhe.Garantir(AtualizarTotpDetalhes);
            }
            catch
            {
                PainelDetalheTotp.IsVisible = false;
                _timerTotpDetalhe.Parar();
            }
        }

        private async void CopiarTotpDetalhes_Click(object? sender, RoutedEventArgs e)
        {
            var segredo = _senhaDetalhe != null ? ObterTotpPlain(_senhaDetalhe) : null;
            if (string.IsNullOrEmpty(segredo))
                return;

            string codigo;
            try { codigo = _totp.Gerar(segredo).Codigo; }
            catch { return; }

            await CopiarDetalheAsync(codigo, Idioma.Texto("Row.CopyTotp"), limparDepois: true, campoRegistrado: TipoCampoCopiado.Totp);
        }

        private async void EdicaoCompletaDetalhes_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhaDetalhe == null)
                return;

            if (!await ConfirmarDescarteDetalhesAsync())
                return;

            var id = _senhaDetalhe.Id;
            var dlg = new JanelaEditarSenha(_servicoSenha, _senhaDetalhe, _criptografia, _servicoAnexos);
            if (await AbrirDialogoAsync<bool>(dlg))
                await CarregarSenhasAsync();

            var atualizada = _senhasAtuais.FirstOrDefault(s => s.Id == id);
            if (atualizada != null)
                AbrirDetalhes(atualizada);
            else
                FecharDetalhes();
        }

        private async void FecharDetalhes_Click(object? sender, RoutedEventArgs e)
        {
            // Salvar/Fechar/Excluir compartilham esta trava — sem ela, dava pra
            // fechar (ou descartar) o painel enquanto um Salvar do mesmo item ainda
            // estava em voo, e quando o Salvar terminasse ele reabria o painel que o
            // usuário acabou de fechar.
            if (_detalhesOperacaoEmAndamento)
                return;

            _detalhesOperacaoEmAndamento = true;
            try
            {
                if (!await ConfirmarDescarteDetalhesAsync())
                    return;
                FecharDetalhes();
            }
            finally
            {
                _detalhesOperacaoEmAndamento = false;
            }
        }

        private void FecharDetalhes()
        {
            PainelDetalhes.IsVisible = false;
            _senhaDetalhe = null;
            _senhaDetalhePlain = "";
            _senhaDetalheOriginal = "";
            _senhaDetalheVisivel = false;
            _snapshotDetalhes = null;
            TxtDetalheSenha.Text = "";
            _timerTotpDetalhe.Parar();
            PainelDetalheTotp.IsVisible = false;
        }

        private async void ExcluirDetalhes_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhaDetalhe == null || _detalhesOperacaoEmAndamento)
                return;

            _detalhesOperacaoEmAndamento = true;
            BtnSalvarDetalhes.IsEnabled = false;
            BtnFecharDetalhes.IsEnabled = false;
            BtnExcluirDetalhes.IsEnabled = false;

            try
            {
                var id = _senhaDetalhe.Id;
                await ExcluirSenhaAsync(_senhaDetalhe);
                if (_senhasAtuais.All(s => s.Id != id))
                    FecharDetalhes();
            }
            finally
            {
                _detalhesOperacaoEmAndamento = false;
                BtnSalvarDetalhes.IsEnabled = true;
                BtnFecharDetalhes.IsEnabled = true;
                BtnExcluirDetalhes.IsEnabled = true;
            }
        }

        private async void SalvarDetalhes_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhaDetalhe == null || _detalhesOperacaoEmAndamento)
                return;

            var servico = (TxtDetalheServico.Text ?? "").Trim();
            var usuario = (TxtDetalheUsuario.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(servico) || string.IsNullOrWhiteSpace(usuario))
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Entry.EditRequired"), Idioma.Texto("Common.Validation"), TipoMensagem.Aviso);
                return;
            }

            var senhaPlain = _senhaDetalheVisivel ? TxtDetalheSenha.Text : _senhaDetalhePlain;
            if (string.IsNullOrEmpty(senhaPlain))
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Entry.RecoverCurrentPasswordError"),
                    Idioma.Texto("Entry.EditTitle"), TipoMensagem.Aviso);
                return;
            }

            _detalhesOperacaoEmAndamento = true;
            BtnSalvarDetalhes.IsEnabled = false;
            BtnFecharDetalhes.IsEnabled = false;
            BtnExcluirDetalhes.IsEnabled = false;

            try
            {
                // Sincronização automática silenciosa (timer em segundo plano) pode
                // ter alterado este mesmo item por trás do painel enquanto o usuário
                // editava — CarregarSenhasAsync atualiza _senhasAtuais a cada ciclo,
                // mas nunca toca no painel de detalhes já aberto. Sem esta checagem,
                // Salvar sobrescreveria em silêncio o que acabou de chegar de outro
                // dispositivo, sem qualquer aviso de conflito.
                if (DetalhesTemAlteracaoConcorrente())
                {
                    var continuar = await CaixaMensagem.ConfirmarAsync(this,
                        Idioma.Texto("Entry.Detail.ConcurrentChangeConfirm"),
                        Idioma.Texto("Entry.Detail.ConcurrentChangeTitle"), TipoMensagem.Aviso);
                    if (!continuar)
                        return;
                }

                var id = _senhaDetalhe.Id;
                var (categoria, etiquetas) = CategoriasUI.LerCategoriaEEtiquetas(CmbDetalheCategoria.SelectedIndex, TxtDetalheEtiquetas.Text);
                await _servicoSenha.AtualizarSenhaAsync(
                    id,
                    servico,
                    usuario,
                    senhaPlain,
                    categoria,
                    string.IsNullOrWhiteSpace(TxtDetalheUrl.Text) ? null : TxtDetalheUrl.Text,
                    string.IsNullOrWhiteSpace(TxtDetalheNotas.Text) ? null : TxtDetalheNotas.Text,
                    etiquetas);

                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();

                var atualizada = _senhasAtuais.FirstOrDefault(s => s.Id == id);
                if (atualizada != null)
                    AbrirDetalhes(atualizada);
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Entry.UpdateError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
            finally
            {
                _detalhesOperacaoEmAndamento = false;
                BtnSalvarDetalhes.IsEnabled = true;
                BtnFecharDetalhes.IsEnabled = true;
                BtnExcluirDetalhes.IsEnabled = true;
            }
        }

        private void RevelarSenhaDetalhes_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhaDetalheVisivel)
                _senhaDetalhePlain = TxtDetalheSenha.Text ?? "";

            _senhaDetalheVisivel = !_senhaDetalheVisivel;
            AtualizarSenhaDetalhe();
        }

        private async void CopiarUsuarioDetalhes_Click(object? sender, RoutedEventArgs e) =>
            // limparDepois:true — mesmo motivo do LinhaSenha.CopiarUsuarioAsync (ver
            // AreaTransferenciaFeedback): sem isto, usuário copiado pelo painel de
            // detalhes (muitas vezes o e-mail da pessoa) ficava esquecido no clipboard
            // pra sempre, enquanto senha e TOTP já eram apagados sozinhos.
            await CopiarDetalheAsync(TxtDetalheUsuario.Text, Idioma.Texto("Row.CopyUser"),
                limparDepois: true, campoRegistrado: TipoCampoCopiado.Usuario, botaoFeedback: BtnCopiarUsuarioDetalhes,
                obterTimer: () => _timerFeedbackUsuarioDetalhes, definirTimer: t => _timerFeedbackUsuarioDetalhes = t,
                chaveMensagemLimpando: "Row.UserCopiedClearing");

        private async void CopiarSenhaDetalhes_Click(object? sender, RoutedEventArgs e) =>
            await CopiarDetalheAsync(_senhaDetalheVisivel ? TxtDetalheSenha.Text : _senhaDetalhePlain,
                Idioma.Texto("Row.CopyPassword"), limparDepois: true, campoRegistrado: TipoCampoCopiado.Senha,
                botaoFeedback: BtnCopiarSenhaDetalhes, obterTimer: () => _timerFeedbackSenhaDetalhes,
                definirTimer: t => _timerFeedbackSenhaDetalhes = t, chaveMensagemLimpando: "Row.PasswordCopiedClearing");

        private async void CopiarUrlDetalhes_Click(object? sender, RoutedEventArgs e) =>
            await CopiarDetalheAsync(TxtDetalheUrl.Text, "URL");

        private async void DigitacaoAutomatica_Click(object? sender, RoutedEventArgs e)
        {
            if (_senhaDetalhe == null || !DigitacaoAutomatica.Suportado)
                return;

            var usuario = TxtDetalheUsuario.Text ?? "";
            var senha = _senhaDetalheVisivel ? (TxtDetalheSenha.Text ?? "") : _senhaDetalhePlain;
            if (string.IsNullOrEmpty(senha))
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Entry.RecoverCurrentPasswordError"),
                    Idioma.Texto("AutoType.Title"), TipoMensagem.Aviso);
                return;
            }

            if (DigitacaoAutomatica.AlvoAtual() is not { } alvo)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("AutoType.NoTarget"),
                    Idioma.Texto("AutoType.Title"), TipoMensagem.Info);
                return;
            }

            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Formatar("AutoType.Confirm", alvo.Titulo, alvo.Processo),
                Idioma.Texto("AutoType.Title"), TipoMensagem.Aviso);
            if (!confirmar)
                return;

            var resultado = await DigitacaoAutomatica.DigitarAsync(alvo, usuario, senha);
            if (resultado == ResultadoDigitacao.Ok)
            {
                Acessibilidade.Anunciar(this, Idioma.Texto("AutoType.Done"));
                return;
            }

            await CaixaMensagem.MostrarAsync(this,
                Idioma.Texto(resultado switch
                {
                    ResultadoDigitacao.AlvoIndisponivel => "AutoType.TargetGone",
                    ResultadoDigitacao.FocoNaoObtido => "AutoType.FocusFailed",
                    ResultadoDigitacao.NaoSuportado => "AutoType.Unsupported",
                    _ => "AutoType.Failed"
                }),
                Idioma.Texto("AutoType.Title"), TipoMensagem.Aviso);
        }

        private async Task CopiarDetalheAsync(string? texto, string rotulo, bool limparDepois = false,
            TipoCampoCopiado? campoRegistrado = null, Button? botaoFeedback = null,
            Func<DispatcherTimer?>? obterTimer = null, Action<DispatcherTimer?>? definirTimer = null,
            string? chaveMensagemLimpando = null)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return;

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                try { await AreaTransferenciaSegura.CopiarAsync(clipboard, texto); } catch { }
            }

            int segundos = Preferencias.SegundosLimpezaClipboard;
            if (limparDepois && segundos > 0 && clipboard != null)
            {
                Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.CopiedWillClear", rotulo, segundos));
                if (botaoFeedback != null && obterTimer != null && definirTimer != null && chaveMensagemLimpando != null)
                    AgendarFeedbackLimpezaDetalhe(botaoFeedback, obterTimer, definirTimer, chaveMensagemLimpando, segundos);
                _ = ServicoLimpezaClipboard.ProgramarLimpezaAsync(new AreaTransferenciaAvalonia(clipboard), texto, segundos);
            }
            else
            {
                Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.Copied", rotulo));
            }

            if (campoRegistrado.HasValue && Preferencias.RegistrarHistoricoUso && _senhaDetalhe != null)
            {
                await _servicoSenha.RegistrarCopiaAsync(_senhaDetalhe.Id, campoRegistrado.Value);
                await _servicoSenha.PersistirAsync();
                AtualizarHistoricoDetalhes();
            }
        }

        private void AgendarFeedbackLimpezaDetalhe(Button botao, Func<DispatcherTimer?> obterTimer,
            Action<DispatcherTimer?> definirTimer, string chaveMensagem, int segundos)
        {
            var mensagem = Idioma.Formatar(chaveMensagem, segundos);
            ToolTip.SetTip(botao, mensagem);
            AutomationProperties.SetName(botao, mensagem);

            obterTimer()?.Stop();

            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Min(segundos, 3)) };
            t.Tick += (s, e) =>
            {
                botao.ClearValue(ToolTip.TipProperty);
                botao.ClearValue(AutomationProperties.NameProperty);
                t.Stop();
            };
            definirTimer(t);
            t.Start();
        }
    }
}
