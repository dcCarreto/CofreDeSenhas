using Avalonia.Controls;
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
                LblErro.Text = "Informe a senha mestra atual.";
                return;
            }
            if ((TxtNova.Text ?? "").Length < 8)
            {
                LblErro.Text = "A nova senha deve ter pelo menos 8 caracteres.";
                return;
            }
            if (TxtNova.Text != TxtConfirmar.Text)
            {
                LblErro.Text = "A confirmação não coincide com a nova senha.";
                return;
            }

            SenhaAtual = TxtAtual.Text!;
            NovaSenha = TxtNova.Text!;
            Close(true);
        }
    }
}
