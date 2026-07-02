using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using GerenciadorDeSenhas.Modelos;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaSelecionarBanco : Window
    {
        public TipoBanco? Selecionado { get; private set; }

        public JanelaSelecionarBanco()
        {
            InitializeComponent();
            Icon = Recursos.IconeApp();

            MontarGrade();

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) Close(false);
            };
        }

        private void MontarGrade()
        {
            foreach (var provedor in ProvedorBanco.Todos)
                Grade.Children.Add(CriarCartao(provedor));
        }

        private Button CriarCartao(ProvedorBanco provedor)
        {
            var textoFallback = new TextBlock
            {
                Text = provedor.Distintivo,
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                FontSize = 15,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var imagemIcone = new Image
            {
                Width = 34,
                Height = 34,
                Stretch = Stretch.Uniform,
                IsVisible = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var conteudoDistintivo = new Grid();
            conteudoDistintivo.Children.Add(textoFallback);
            conteudoDistintivo.Children.Add(imagemIcone);

            var distintivo = new Border
            {
                Width = 44,
                Height = 44,
                CornerRadius = new Avalonia.CornerRadius(10),
                Background = new SolidColorBrush(Color.Parse(provedor.Cor)),
                VerticalAlignment = VerticalAlignment.Center,
                Child = conteudoDistintivo
            };
            ToolTip.SetTip(distintivo, provedor.Rotulo);
            _ = CarregarIconeBancoAsync(provedor, distintivo, textoFallback, imagemIcone);

            var rotulo = new TextBlock
            {
                Text = provedor.Rotulo,
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                Foreground = Tema.Pincel(Tema.TextPrimary),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(14, 0, 0, 0)
            };

            var conteudo = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Avalonia.Thickness(14, 0, 0, 0)
            };
            conteudo.Children.Add(distintivo);
            conteudo.Children.Add(rotulo);

            var cartao = new Button
            {
                Width = 232,
                Height = 84,
                Margin = new Avalonia.Thickness(0, 0, 12, 12),
                Content = conteudo
            };
            cartao.Classes.Add("cartao");
            cartao.Click += (s, e) =>
            {
                Selecionado = provedor.Tipo;
                Close(true);
            };
            return cartao;
        }

        private static async Task CarregarIconeBancoAsync(ProvedorBanco provedor, Border distintivo,
            TextBlock textoFallback, Image imagemIcone)
        {
            var icone = IconesServico.Obter(provedor.Rotulo);
            var bitmap = await IconesServico.ObterBitmapAsync(icone);
            if (bitmap == null)
                return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                imagemIcone.Source = bitmap;
                imagemIcone.IsVisible = true;
                textoFallback.IsVisible = false;
                distintivo.Background = Brushes.White;
                distintivo.BorderBrush = Tema.Pincel(Tema.CardBorder);
                distintivo.BorderThickness = new Avalonia.Thickness(1);
            });
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }

        private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);
    }
}
