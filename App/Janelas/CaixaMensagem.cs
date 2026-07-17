using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace CofreDeSenhas.Janelas
{
    public enum TipoMensagem { Info, Aviso, Erro }

    public class CaixaMensagem : Window
    {
        private CaixaMensagem(string texto, string titulo, TipoMensagem tipo, bool simNao)
        {
            Title = titulo;
            Icon = Recursos.IconeApp();
            SystemDecorations = SystemDecorations.None;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            SizeToContent = SizeToContent.Height;
            Width = 420;
            CanResize = false;
            Background = Brushes.Transparent;
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };

            var lblTitulo = new TextBlock
            {
                Text = titulo,
                FontSize = 17,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(24, 0, 0, 0)
            };
            AutomationProperties.SetHeadingLevel(lblTitulo, 1);
            lblTitulo.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("TextPrimary"));

            var btnFechar = new Button { Content = "✕" };
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

            var glifo = new TextBlock
            {
                Text = tipo switch
                {
                    TipoMensagem.Aviso => "⚠",
                    TipoMensagem.Erro => "✖",
                    _ => "ℹ"
                },
                FontSize = 22,
                Foreground = tipo switch
                {
                    TipoMensagem.Aviso => Tema.Pincel(Tema.StrengthMedium),
                    TipoMensagem.Erro => Tema.Pincel(Tema.StrengthWeak),
                    _ => Tema.Pincel(Tema.AccentPrimary)
                },
                VerticalAlignment = VerticalAlignment.Top,
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

            var rodape = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(24, 0, 24, 20)
            };
            DockPanel.SetDock(rodape, Dock.Bottom);

            if (simNao)
            {
                var btnNao = new Button { Content = Idioma.Texto("Common.No"), Width = 110, Height = 38 };
                btnNao.Classes.Add("secundario");
                AutomationProperties.SetName(btnNao, Idioma.Texto("Common.No"));
                btnNao.Click += (s, e) => Close(false);

                var btnSim = new Button { Content = Idioma.Texto("Common.Yes"), Width = 110, Height = 38 };
                btnSim.Classes.Add("primario");
                AutomationProperties.SetName(btnSim, Idioma.Texto("Common.Yes"));
                btnSim.Click += (s, e) => Close(true);

                rodape.Children.Add(btnSim);
                rodape.Children.Add(btnNao);
                Opened += (s, e) => btnNao.Focus();
            }
            else
            {
                var btnOk = new Button { Content = "OK", Width = 110, Height = 38 };
                btnOk.Classes.Add("primario");
                AutomationProperties.SetName(btnOk, "OK");
                btnOk.Click += (s, e) => Close(true);
                rodape.Children.Add(btnOk);
                Opened += (s, e) => btnOk.Focus();
            }

            var raiz = new DockPanel();
            raiz.Children.Add(bordaHeader);
            raiz.Children.Add(rodape);
            raiz.Children.Add(corpo);

            var moldura = new Border
            {
                CornerRadius = new CornerRadius(12),
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
    }
}
