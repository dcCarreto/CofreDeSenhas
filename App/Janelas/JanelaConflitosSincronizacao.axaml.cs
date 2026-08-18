using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using GerenciadorDeSenhas.Modelos;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaConflitosSincronizacao : Window
    {
        public JanelaConflitosSincronizacao(IReadOnlyList<ConflitoSincronizacao> conflitos)
        {
            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);

            MontarLinhas(conflitos);

            this.FecharComEsc();

            Opened += (s, e) => BtnFechar.Focus();
        }

        private void MontarLinhas(IReadOnlyList<ConflitoSincronizacao> conflitos)
        {
            foreach (var conflito in conflitos.OrderByDescending(c => c.DetectadoEmUtc))
                PainelLinhas.Children.Add(CriarLinha(conflito));
        }

        private static Control CriarLinha(ConflitoSincronizacao conflito)
        {
            var (chaveTipo, cor) = conflito.Tipo switch
            {
                TipoConflitoSincronizacao.IntegridadeViolada => ("Sync.ConflictType.IntegrityViolated", Tema.StatusWarning),
                TipoConflitoSincronizacao.IntegridadeAusente => ("Sync.ConflictType.IntegrityMissing", Tema.StatusWarning),
                _ => ("Sync.ConflictType.ConcurrentEdit", Tema.TextSecondary)
            };

            var lblServico = new TextBlock
            {
                Text = conflito.NomeServico,
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                Foreground = Tema.Pincel(Tema.TextPrimary),
                TextWrapping = TextWrapping.Wrap
            };

            var lblTipo = new TextBlock
            {
                Text = Idioma.Texto(chaveTipo),
                FontSize = 12,
                Foreground = Tema.Pincel(cor),
                TextWrapping = TextWrapping.Wrap
            };

            var lblData = new TextBlock
            {
                Text = conflito.DetectadoEmUtc.ToLocalTime().ToString("g", Idioma.CulturaAtual),
                FontSize = 11,
                Foreground = Tema.Pincel(Tema.TextTertiary)
            };

            var painel = new StackPanel { Spacing = 3 };
            painel.Children.Add(lblServico);
            painel.Children.Add(lblTipo);
            painel.Children.Add(lblData);

            return new Border
            {
                Padding = new Thickness(14, 12),
                CornerRadius = new CornerRadius(10),
                Background = Tema.Pincel(Tema.CardBackground),
                BorderBrush = Tema.Pincel(conflito.Tipo is TipoConflitoSincronizacao.IntegridadeViolada or TipoConflitoSincronizacao.IntegridadeAusente ? Tema.StatusWarning : Tema.InputBorder),
                BorderThickness = new Thickness(1),
                Child = painel
            };
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e) => this.HabilitarArraste(e);

        private void Fechar_Click(object? sender, RoutedEventArgs e) => Close(false);
    }
}
