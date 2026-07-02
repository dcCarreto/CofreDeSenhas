using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaConfirmarSenhaMestra : Window
    {
        private readonly AutenticacaoMestra _auth = new();

        public string SenhaConfirmada { get; private set; } = string.Empty;

        public JanelaConfirmarSenhaMestra()
        {
            InitializeComponent();
            Icon = Recursos.IconeApp();

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter) Confirmar();
                if (e.Key == Key.Escape) Close(false);
            };

            Opened += (s, e) => TxtSenha.Focus();
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.Source is not TextBox)
                BeginMoveDrag(e);
        }

        private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);

        private void Gerar_Click(object? sender, RoutedEventArgs e) => Confirmar();

        private void Confirmar()
        {
            var senha = TxtSenha.Text ?? "";
            if (string.IsNullOrEmpty(senha))
            {
                MostrarErro("Digite sua senha mestra.");
                return;
            }

            if (_auth.Autenticar(senha) == null)
            {
                MostrarErro("Senha mestra incorreta.");
                return;
            }

            SenhaConfirmada = senha;
            Close(true);
        }

        private void MostrarErro(string msg)
        {
            LblErro.Text = msg;
            TxtSenha.SelectAll();
            TxtSenha.Focus();
        }
    }
}
