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
        private async Task CarregarLixeiraAsync()
        {
            try
            {
                _itensLixeira = await _servicoSenha.ListarLixeiraAsync();
                AtualizarListaLixeira();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Message.LoadError", ErrosUi.MensagemAmigavel(ex)),
                    Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private void AtualizarListaLixeira()
        {
            _linhasSenha.Clear();
            _linhaFocada = null;

            var lista = _itensLixeira
                .OrderByDescending(s => s.DataExclusao)
                .ToList();

            LblContadorHeader.Text = Idioma.Plural(lista.Count, "Vault.Counter.ItemSingular", "Vault.Counter.ItemPlural");

            LblVazio.IsVisible = lista.Count == 0;
            TxtVazioMensagem.Text = Idioma.Texto("Trash.Empty");
            BtnVazioNovaSenha.IsVisible = false;
            AutomationProperties.SetName(LblVazio, Idioma.Texto("Trash.Empty"));

            PainelLista.ModoLixeira = true;
            PainelLista.ItemsSource = lista;
        }

        private Control CriarLinhaLixeira(Senha senha)
        {
            // Mesma máscara que LinhaSenha aplica na lista principal — sem isto, entrar
            // na lixeira com o modo privacidade ativo mostrava serviço e usuário reais
            // de todo item excluído, driblando o próprio modo que o usuário acabou de
            // ligar.
            var nomeExibido = _modoPrivacidade ? LinhaSenha.MascaraPrivacidade : senha.NomeServico;
            var usuarioExibido = _modoPrivacidade ? LinhaSenha.MascaraPrivacidade : senha.Usuario;

            var avatar = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(10),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            var avatarTexto = new TextBlock
            {
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            avatar.Child = avatarTexto;
            if (_modoPrivacidade)
            {
                avatar.Background = Tema.Pincel(Tema.TrailInactive);
                avatarTexto.Text = "•";
                avatarTexto.Foreground = Tema.Pincel(Tema.TextSecondary);
            }
            else
            {
                var icone = IconesServico.Obter(senha.NomeServico, senha.Url);
                avatar.Background = Tema.Pincel(icone.Fundo);
                avatarTexto.Text = icone.Texto;
                avatarTexto.Foreground = Tema.Pincel(icone.Frente);
            }

            var lblServico = new TextBlock
            {
                Text = nomeExibido,
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Tema.Pincel(Tema.TextPrimary)
            };
            var lblUsuario = new TextBlock
            {
                Text = usuarioExibido,
                FontSize = 12,
                Foreground = Tema.Pincel(Tema.TextSecondary)
            };
            var info = new StackPanel { Spacing = 2, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            info.Children.Add(lblServico);
            info.Children.Add(lblUsuario);
            Grid.SetColumn(info, 1);

            var dataFormatada = senha.DataExclusao.HasValue
                ? senha.DataExclusao.Value.ToLocalTime().ToString("dd MMM yyyy", Idioma.CulturaAtual)
                : "";
            var lblData = new TextBlock
            {
                Text = Idioma.Formatar("Trash.DeletedOn", dataFormatada),
                FontSize = 12,
                Foreground = Tema.Pincel(Tema.TextSecondary),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(12, 0)
            };
            Grid.SetColumn(lblData, 2);

            var btnRestaurar = new Button
            {
                Content = Idioma.Texto("Trash.Restore"),
                MinHeight = 34,
                FontSize = 12,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            btnRestaurar.Classes.Add("secundario");
            AutomationProperties.SetName(btnRestaurar, Idioma.Formatar("Trash.Restore") + " " + nomeExibido);
            btnRestaurar.Click += async (s, e) => await RestaurarDaLixeiraAsync(senha);
            Grid.SetColumn(btnRestaurar, 3);

            var btnExcluir = new Button
            {
                Width = 36,
                Height = 34,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            btnExcluir.Classes.Add("icone");
            btnExcluir.Content = Recursos.ImagemIcone("IconeExcluir", 22);
            ToolTip.SetTip(btnExcluir, Idioma.Texto("Trash.DeleteForever"));
            AutomationProperties.SetName(btnExcluir, Idioma.Texto("Trash.DeleteForever") + " " + nomeExibido);
            btnExcluir.Click += async (s, e) => await ExcluirDefinitivamenteAsync(senha);
            Grid.SetColumn(btnExcluir, 4);

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto,Auto"),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(avatar, 0);
            grid.Children.Add(avatar);
            grid.Children.Add(info);
            grid.Children.Add(lblData);
            grid.Children.Add(btnRestaurar);
            grid.Children.Add(btnExcluir);

            return new Border
            {
                Padding = new Thickness(14, 10),
                Margin = new Thickness(0, 0, 0, 6),
                CornerRadius = new CornerRadius(10),
                Background = Tema.Pincel(Tema.CardBackground),
                BorderBrush = Tema.Pincel(Tema.InputBorder),
                BorderThickness = new Thickness(1),
                Child = grid
            };
        }

        private async Task RestaurarDaLixeiraAsync(Senha senha)
        {
            try
            {
                await _servicoSenha.RestaurarSenhaAsync(senha.Id);
                await _servicoSenha.PersistirAsync();
                await CarregarSenhasAsync();
                var nomeExibido = _modoPrivacidade ? LinhaSenha.MascaraPrivacidade : senha.NomeServico;
                Acessibilidade.Anunciar(this, Idioma.Formatar("A11y.Restored", nomeExibido));
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Trash.RestoreError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async Task ExcluirDefinitivamenteAsync(Senha senha)
        {
            var nomeExibido = _modoPrivacidade ? LinhaSenha.MascaraPrivacidade : senha.NomeServico;
            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Formatar("Trash.DeleteForeverConfirm", nomeExibido),
                Idioma.Texto("Trash.DeleteForever"), TipoMensagem.Aviso);

            if (!confirmar)
                return;

            try
            {
                await _servicoSenha.RemoverDefinitivamenteAsync(senha.Id);
                await _servicoSenha.PersistirAsync();
                _cachePlain.Remove(senha.Id);
                _servicoAnexos?.RemoverTodos(senha);
                await PublicarTumbasNaPastaDeSincronizacaoAsync(new[] { senha.Id });
                await CarregarSenhasAsync();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Trash.DeleteForeverError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async void EsvaziarLixeira_Click(object? sender, RoutedEventArgs e)
        {
            if (_itensLixeira.Count == 0)
                return;

            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Texto("Trash.EmptyConfirm"),
                Idioma.Texto("Trash.EmptyConfirmTitle"), TipoMensagem.Aviso);

            if (!confirmar)
                return;

            try
            {
                var itensParaLimparAnexos = _itensLixeira.ToList();

                await _servicoSenha.EsvaziarLixeiraAsync();
                await _servicoSenha.PersistirAsync();

                foreach (var item in itensParaLimparAnexos)
                    _cachePlain.Remove(item.Id);

                // Sobrecarga em lote: limpa UltimosAvisos uma vez só e acumula os
                // avisos de todos os itens, em vez de um loop de chamadas individuais
                // (que perderia o aviso de cada item anterior a cada nova chamada).
                _servicoAnexos?.RemoverTodos(itensParaLimparAnexos);

                await PublicarTumbasNaPastaDeSincronizacaoAsync(itensParaLimparAnexos.Select(s => s.Id));
                await CarregarSenhasAsync();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Trash.EmptyError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        // Diferente do caminho de banco (RepositorioSenhaBanco.EsvaziarLinhaAsync
        // grava uma linha em branco persistente), a pasta de sincronização não tem
        // nenhum armazenamento próprio — só o snapshot que cada sync escreve. Uma vez
        // que RemoverDefinitivamenteAsync roda, o item não deixa nenhum rastro local
        // (RepositorioSenha.RemoverDefinitivamenteAsync é um DELETE de verdade), então
        // se nada avisasse o sincronizacao.dat agora, o próximo ciclo de sync veria o
        // item "só no remoto" (a cópia de antes da exclusão) e o ressuscitaria aqui —
        // exatamente o oposto do que "excluir definitivamente" promete. Isto publica a
        // tumba direto no arquivo compartilhado, sem esperar o próximo ciclo.
        //
        // internal só pra teste chamar direto sem precisar navegar até a lixeira na
        // UI — ver App.Testes (InternalsVisibleTo), mesmo padrão já usado em
        // RepublicarAposTrocaDeSenhaMestraAsync.
        internal async Task PublicarTumbasNaPastaDeSincronizacaoAsync(IEnumerable<Guid> idsExcluidos)
        {
            if (_servicoSincronizacao == null || Preferencias.Sincronizacao is not { } perfil)
                return;

            try
            {
                var caminho = Path.Combine(perfil.Pasta, ServicoSincronizacao.NomeArquivo);
                var remotas = await _servicoSincronizacao.LerAsync(caminho);
                var agora = DateTime.UtcNow;

                foreach (var id in idsExcluidos)
                {
                    remotas.RemoveAll(s => s.Id == id);
                    remotas.Add(new SenhaExportada
                    {
                        Id = id,
                        NomeServico = "",
                        Usuario = "",
                        Senha = "",
                        NaLixeira = true,
                        DataExclusao = agora,
                        DataCriacao = agora,
                        DataAtualizacao = agora
                    });
                }

                var salt = Convert.FromBase64String(perfil.Salt);
                await _servicoSincronizacao.EscreverAsync(caminho, salt, perfil.Kdf, perfil.Iteracoes,
                    perfil.MemoriaKb, perfil.Paralelismo, remotas);
            }
            catch
            {
                // Melhor esforço: se a publicação da tumba falhar, o item já foi
                // excluído localmente de qualquer forma; o pior caso é o próximo sync
                // ainda ressuscitar o item aqui — o mesmo comportamento de antes desta
                // correção, não uma perda pior do que já existia.
            }
        }
    }
}
