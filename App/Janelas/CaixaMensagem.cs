using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Path = Avalonia.Controls.Shapes.Path;

namespace CofreDeSenhas.Janelas
{
    public enum TipoMensagem { Info, Aviso, Erro }

    public class CaixaMensagem : Window
    {
        private const int MaximoItensVisiveis = 50;

        private CaixaMensagem(string texto, string titulo, TipoMensagem tipo, bool simNao, IReadOnlyList<string>? itens = null)
        {
            Title = titulo;
            Icon = Recursos.IconeApp();
            SystemDecorations = SystemDecorations.None;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            SizeToContent = SizeToContent.Height;
            Width = 480;
            CanResize = false;
            Background = Brushes.Transparent;
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
            RenderOptions.SetBitmapInterpolationMode(this, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);

            var lblTitulo = new TextBlock
            {
                Text = titulo,
                FontSize = 17,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(24, 0, 60, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            AutomationProperties.SetHeadingLevel(lblTitulo, 1);
            lblTitulo.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("TextPrimary"));

            var btnFechar = new Button
            {
                Content = Recursos.ImagemIcone("IconeFechar", 19)
            };
            btnFechar.Classes.Add("fechar-dialogo");
            btnFechar.Margin = new Thickness(0, 0, 14, 0);
            btnFechar.HorizontalAlignment = HorizontalAlignment.Right;
            AutomationProperties.SetName(btnFechar, Idioma.Texto("Access.Close"));
            btnFechar.Click += (s, e) => Close(false);

            var header = new Grid { Height = 56 };
            header.Children.Add(lblTitulo);
            header.Children.Add(btnFechar);
            header.PointerPressed += (s, e) => this.HabilitarArraste(e);

            var bordaHeader = new Border { Child = header, BorderThickness = new Thickness(0, 0, 0, 1) };
            bordaHeader.Bind(Border.BorderBrushProperty, this.GetResourceObservable("CardBorder"));
            DockPanel.SetDock(bordaHeader, Dock.Top);

            var glifo = new Path
            {
                Classes = { "line-icon" },
                Data = (Geometry?)Application.Current!.FindResource(tipo switch
                {
                    TipoMensagem.Aviso => "IconeAviso",
                    TipoMensagem.Erro => "IconeErro",
                    _ => "IconeInfo"
                }),
                Width = 22,
                Height = 22,
                Stroke = tipo switch
                {
                    TipoMensagem.Aviso => Tema.Pincel(Tema.StrengthMedium),
                    TipoMensagem.Erro => Tema.Pincel(Tema.StrengthWeak),
                    _ => Tema.Pincel(Tema.AccentPrimary)
                },
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 14, 0)
            };

            var lblTexto = new TextBlock
            {
                Text = texto,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            AutomationProperties.SetLiveSetting(lblTexto, tipo == TipoMensagem.Erro
                ? AutomationLiveSetting.Assertive
                : AutomationLiveSetting.Polite);
            AutomationProperties.SetName(lblTexto, texto);
            lblTexto.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("TextPrimary"));

            var corpo = new DockPanel { Margin = new Thickness(24, 20, 24, 20) };
            DockPanel.SetDock(glifo, Dock.Left);
            corpo.Children.Add(glifo);
            corpo.Children.Add(lblTexto);

            var scrollCorpo = new ScrollViewer { MaxHeight = 320, Content = corpo };

            Control conteudoCentral = scrollCorpo;
            if (itens is { Count: > 0 })
            {
                var painelItens = new StackPanel { Margin = new Thickness(24, 0, 24, 20) };
                foreach (var item in itens.Take(MaximoItensVisiveis))
                {
                    var linha = new TextBlock
                    {
                        Text = item,
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 4)
                    };
                    linha.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("TextPrimary"));
                    painelItens.Children.Add(linha);
                }
                if (itens.Count > MaximoItensVisiveis)
                {
                    var maisLinha = new TextBlock
                    {
                        Text = Idioma.Formatar("Common.AndMore", itens.Count - MaximoItensVisiveis),
                        FontSize = 12,
                        FontStyle = FontStyle.Italic
                    };
                    maisLinha.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("TextSecondary"));
                    painelItens.Children.Add(maisLinha);
                }

                var scrollItens = new ScrollViewer { MaxHeight = 240, Content = painelItens };

                var container = new DockPanel();
                DockPanel.SetDock(scrollCorpo, Dock.Top);
                container.Children.Add(scrollCorpo);
                container.Children.Add(scrollItens);
                conteudoCentral = container;
            }

            var rodape = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(24, 20, 24, 20)
            };
            DockPanel.SetDock(rodape, Dock.Bottom);

            if (simNao)
            {
                var btnNao = new Button { Content = Idioma.Texto("Common.No"), MinWidth = 120, Height = 40 };
                btnNao.Classes.Add("secundario");
                AutomationProperties.SetName(btnNao, Idioma.Texto("Common.No"));
                btnNao.Click += (s, e) => Close(false);

                var btnSim = new Button { Content = Idioma.Texto("Common.Yes"), MinWidth = 140, Height = 40 };
                btnSim.Classes.Add("primario");
                AutomationProperties.SetName(btnSim, Idioma.Texto("Common.Yes"));
                btnSim.Click += (s, e) => Close(true);

                rodape.Children.Add(btnSim);
                rodape.Children.Add(btnNao);
                Opened += (s, e) => btnNao.Focus();
            }
            else
            {
                var btnOk = new Button { Content = "OK", MinWidth = 140, Height = 40 };
                btnOk.Classes.Add("primario");
                AutomationProperties.SetName(btnOk, "OK");
                btnOk.Click += (s, e) => Close(true);
                rodape.Children.Add(btnOk);
                Opened += (s, e) => btnOk.Focus();
            }

            var raiz = new DockPanel();
            raiz.Children.Add(bordaHeader);
            raiz.Children.Add(rodape);
            raiz.Children.Add(conteudoCentral);

            var moldura = new Border
            {
                CornerRadius = new CornerRadius(20),
                BorderThickness = new Thickness(1),
                ClipToBounds = true,
                Child = raiz
            };
            moldura.Bind(Border.BackgroundProperty, this.GetResourceObservable("CardBackground"));
            moldura.Bind(Border.BorderBrushProperty, this.GetResourceObservable("CardBorder"));

            Content = moldura;
            AutomationProperties.SetName(this, titulo);
            Acessibilidade.Vincular(this);

            this.FecharComEsc();
        }

        public static async Task MostrarAsync(Window dono, string texto, string titulo, TipoMensagem tipo = TipoMensagem.Info)
        {
            Scrim.Mostrar(dono);
            try
            {
                await new CaixaMensagem(texto, titulo, tipo, simNao: false).ShowDialog(dono);
            }
            finally
            {
                Scrim.Ocultar(dono);
            }
        }

        public static async Task<bool> ConfirmarAsync(Window dono, string texto, string titulo, TipoMensagem tipo = TipoMensagem.Aviso)
        {
            Scrim.Mostrar(dono);
            try
            {
                return await new CaixaMensagem(texto, titulo, tipo, simNao: true).ShowDialog<bool>(dono);
            }
            finally
            {
                Scrim.Ocultar(dono);
            }
        }

        public static async Task<bool> ConfirmarComListaAsync(Window dono, string texto, string titulo, IReadOnlyList<string> itens, TipoMensagem tipo = TipoMensagem.Aviso)
        {
            Scrim.Mostrar(dono);
            try
            {
                return await new CaixaMensagem(texto, titulo, tipo, simNao: true, itens).ShowDialog<bool>(dono);
            }
            finally
            {
                Scrim.Ocultar(dono);
            }
        }
    }
}
