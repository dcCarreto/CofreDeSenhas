using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Interactivity;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaConfirmarSenhaMestra : Window
    {
        private readonly AutenticacaoMestra _auth = new();

        public string SenhaConfirmada { get; private set; } = string.Empty;

        public JanelaConfirmarSenhaMestra(string? titulo = null, string? instrucao = null, string? textoBotao = null)
        {
            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);

            if (titulo != null)
            {
                Title = titulo;
                LblTitulo.Text = titulo;
            }
            if (instrucao != null)
                LblInstrucao.Text = instrucao;
            if (textoBotao != null)
                BtnConfirmar.Content = textoBotao;

            this.FecharComEscConfirmarComEnter(Confirmar);

            Opened += (s, e) => TxtSenha.Focus();
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e) => this.HabilitarArraste(e, origem => origem is TextBox);

        private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);

        private void Gerar_Click(object? sender, RoutedEventArgs e) => Confirmar();

        private void Confirmar()
        {
            var senha = TxtSenha.Text ?? "";
            if (string.IsNullOrEmpty(senha))
            {
                MostrarErro(Idioma.Texto("Qr.ErrorMasterRequired"));
                return;
            }

            if (_auth.Autenticar(senha) == null)
            {
                MostrarErro(Idioma.Texto("Qr.ErrorMasterIncorrect"));
                return;
            }

            SenhaConfirmada = senha;
            Close(true);
        }

        private void MostrarErro(string msg)
        {
            LblErro.Text = msg;
            AutomationProperties.SetName(LblErro, msg);
            TxtSenha.SelectAll();
            TxtSenha.Focus();
        }
    }
}
