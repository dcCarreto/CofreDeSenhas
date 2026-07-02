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

            AtualizarTextos();
            PainelConfirmar.IsVisible = modoExportar;
            Idioma.Alterado += Idioma_Alterado;
            Closed += (s, e) => Idioma.Alterado -= Idioma_Alterado;

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

        private void Idioma_Alterado(object? sender, EventArgs e) => AtualizarTextos();

        private void AtualizarTextos()
        {
            Title = Idioma.Texto(_modoExportar
                ? "ExportDialog.ExportTitle"
                : "ExportDialog.ImportTitle");
            LblTitulo.Text = Title;
            BtnPrincipal.Content = Idioma.Texto(_modoExportar ? "Common.Export" : "Common.Import");
            LblSenha.Text = Idioma.Texto(_modoExportar
                ? "ExportDialog.ExportPassword"
                : "ExportDialog.Password");
            LblInfo.Text = Idioma.Texto(_modoExportar
                ? "ExportDialog.InfoExport"
                : "ExportDialog.InfoImport");
        }

        private void Confirmar()
        {
            var senha = TxtSenha.Text ?? "";

            if (string.IsNullOrWhiteSpace(senha))
            {
                MostrarErro(Idioma.Texto("ExportDialog.PasswordRequired"));
                return;
            }

            if (_modoExportar)
            {
                if (senha.Length < 8)
                {
                    MostrarErro(Idioma.Texto("ExportDialog.PasswordLength"));
                    return;
                }
                if (senha != TxtConfirmar.Text)
                {
                    MostrarErro(Idioma.Texto("ExportDialog.PasswordMismatch"));
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
