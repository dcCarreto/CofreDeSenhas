using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaSenhaExportacao : Window
    {
        private readonly bool _modoExportar;

        public string SenhaInformada { get; private set; } = string.Empty;

        public JanelaSenhaExportacao(bool modoExportar)
        {
            _modoExportar = modoExportar;

            InitializeComponent();
            Icon = Recursos.IconeApp();

            Title = modoExportar ? "Exportar senhas" : "Importar senhas";
            LblTitulo.Text = Title;
            BtnPrincipal.Content = modoExportar ? "Exportar" : "Importar";
            LblSenha.Text = modoExportar ? "Senha de exportação" : "Senha";
            LblInfo.Text = modoExportar
                ? "Defina uma senha para proteger o arquivo. Ela será exigida na importação — guarde-a bem, pois sem ela o arquivo não pode ser aberto."
                : "Informe a senha definida quando o arquivo foi exportado.";
            PainelConfirmar.IsVisible = modoExportar;

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

        private void Principal_Click(object? sender, RoutedEventArgs e) => Confirmar();

        private void Confirmar()
        {
            var senha = TxtSenha.Text ?? "";

            if (string.IsNullOrWhiteSpace(senha))
            {
                MostrarErro("Informe a senha.");
                return;
            }

            if (_modoExportar)
            {
                if (senha.Length < 8)
                {
                    MostrarErro("A senha deve ter pelo menos 8 caracteres.");
                    return;
                }
                if (senha != TxtConfirmar.Text)
                {
                    MostrarErro("As senhas não coincidem.");
                    return;
                }
            }

            SenhaInformada = senha;
            Close(true);
        }

        private void MostrarErro(string mensagem)
        {
            LblErro.Text = mensagem;
            TxtSenha.Focus();
            TxtSenha.SelectAll();
        }
    }
}
