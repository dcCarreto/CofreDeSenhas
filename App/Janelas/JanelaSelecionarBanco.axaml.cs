using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
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
            var distintivo = new Border
            {
                Width = 44,
                Height = 44,
                CornerRadius = new Avalonia.CornerRadius(10),
                Background = new SolidColorBrush(Color.Parse(provedor.Cor)),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = provedor.Distintivo,
                    Foreground = Brushes.White,
                    FontWeight = FontWeight.Bold,
                    FontSize = 15,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

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

        private void Arrastar(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }

        private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);
    }
}
