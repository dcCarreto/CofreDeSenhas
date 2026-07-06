using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaAlterarSenhaMestra : Window
    {
        public string SenhaAtual { get; private set; } = string.Empty;
        public string NovaSenha { get; private set; } = string.Empty;

        public JanelaAlterarSenhaMestra()
        {
            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);

            TxtNova.TextChanged += (s, e) => Medidor.Avaliar(TxtNova.Text);

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter) Confirmar();
                if (e.Key == Key.Escape) Close(false);
            };

            Opened += (s, e) => TxtAtual.Focus();
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.Source is not TextBox)
                BeginMoveDrag(e);
        }

        private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);

        private void Alterar_Click(object? sender, RoutedEventArgs e) => Confirmar();

        private void Confirmar()
        {
            if (string.IsNullOrWhiteSpace(TxtAtual.Text))
            {
                MostrarErro(Idioma.Texto("Master.ErrorCurrentRequired"));
                return;
            }
            if ((TxtNova.Text ?? "").Length < 8)
            {
                MostrarErro(Idioma.Texto("Master.ErrorNewLength"));
                return;
            }
            if (TxtNova.Text != TxtConfirmar.Text)
            {
                MostrarErro(Idioma.Texto("Master.ErrorConfirmMismatch"));
                return;
            }

            SenhaAtual = TxtAtual.Text!;
            NovaSenha = TxtNova.Text!;
            Close(true);
        }

        private void MostrarErro(string mensagem)
        {
            LblErro.Text = mensagem;
            AutomationProperties.SetName(LblErro, mensagem);
        }
    }
}
