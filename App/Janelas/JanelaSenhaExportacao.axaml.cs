using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Interactivity;
using GerenciadorDeSenhas.Servicos;

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
            Acessibilidade.Vincular(this);

            AtualizarTextos();
            PainelConfirmar.IsVisible = modoExportar;
            Medidor.IsVisible = modoExportar;
            TxtSenha.TextChanged += (s, e) => Medidor.Avaliar(TxtSenha.Text);
            Idioma.Alterado += Idioma_Alterado;
            Closed += (s, e) => Idioma.Alterado -= Idioma_Alterado;

            this.FecharComEscConfirmarComEnter(Confirmar);

            Opened += (s, e) => TxtSenha.Focus();
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e) => this.HabilitarArraste(e, origem => origem is TextBox);

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
            AutomationProperties.SetName(TxtSenha, LblSenha.Text ?? "");
            AutomationProperties.SetName(LblInfo, LblInfo.Text ?? "");
            AutomationProperties.SetName(BtnPrincipal, BtnPrincipal.Content?.ToString() ?? "");
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
                if (senha.Length < ServicoExportacao.TamanhoMinimoSenha)
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
            AutomationProperties.SetName(LblErro, mensagem);
            TxtSenha.Focus();
            TxtSenha.SelectAll();
        }
    }
}
