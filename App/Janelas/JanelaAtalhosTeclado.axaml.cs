using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaAtalhosTeclado : Window
    {
        public JanelaAtalhosTeclado()
        {
            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);

            MontarLinhas();

            this.FecharComEsc();

            Opened += (s, e) => BtnFechar.Focus();
        }

        private void MontarLinhas()
        {
            foreach (var atalho in AtalhosTeclado.Todos)
                PainelLinhas.Children.Add(CriarLinha(Idioma.Texto(atalho.ChaveTextoAcao), atalho.TeclasExibicao));

            PainelLinhas.Children.Add(CriarLinha(Idioma.Texto("Shortcuts.CloseDialog"), "Esc"));
        }

        private static Control CriarLinha(string rotulo, params string[] teclas)
        {
            var lblRotulo = new TextBlock
            {
                Text = rotulo,
                FontSize = 13,
                Foreground = Tema.Pincel(Tema.TextPrimary),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 12, 0)
            };

            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            grid.Children.Add(lblRotulo);
            var combinacao = CriarCombinacao(teclas);
            Grid.SetColumn(combinacao, 1);
            grid.Children.Add(combinacao);

            return new Border
            {
                Padding = new Thickness(14, 12),
                CornerRadius = new CornerRadius(10),
                Background = Tema.Pincel(Tema.CardBackground),
                BorderBrush = Tema.Pincel(Tema.InputBorder),
                BorderThickness = new Thickness(1),
                Child = grid
            };
        }

        private static StackPanel CriarCombinacao(string[] teclas)
        {
            var painel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

            for (int i = 0; i < teclas.Length; i++)
            {
                if (i > 0)
                {
                    painel.Children.Add(new TextBlock
                    {
                        Text = "+",
                        FontSize = 12,
                        Foreground = Tema.Pincel(Tema.TextSecondary),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }

                painel.Children.Add(new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 3),
                    Background = Tema.Pincel(Tema.RowHover),
                    BorderBrush = Tema.Pincel(Tema.InputBorder),
                    BorderThickness = new Thickness(1),
                    Child = new TextBlock
                    {
                        Text = teclas[i],
                        FontSize = 12,
                        FontWeight = FontWeight.SemiBold,
                        FontFamily = (FontFamily)Application.Current!.FindResource("FonteMono")!,
                        Foreground = Tema.Pincel(Tema.TextPrimary)
                    }
                });
            }

            return painel;
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e) => this.HabilitarArraste(e);

        private void Fechar_Click(object? sender, RoutedEventArgs e) => Close(false);
    }
}
