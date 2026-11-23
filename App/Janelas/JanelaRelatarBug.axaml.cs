using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaRelatarBug : Window
    {
        // O mesmo texto que o usuário vê no painel é o que vai para a issue.
        private readonly string _relatorio = Diagnostico.MontarRelatorio();

        public JanelaRelatarBug()
        {
            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);
            TxtRelatorio.Text = _relatorio;
            this.FecharComEsc();
            Opened += (s, e) => TxtDescricao.Focus();
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e) => this.HabilitarArraste(e, origem => origem is TextBox);

        private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);

        private async void Enviar_Click(object? sender, RoutedEventArgs e)
        {
            var descricao = TxtDescricao.Text ?? "";
            if (string.IsNullOrWhiteSpace(descricao))
            {
                this.MostrarErroInline(LblErro, Idioma.Texto("BugReport.Required"), TxtDescricao);
                return;
            }

            BtnEnviar.IsEnabled = false;
            BtnEnviar.Content = Idioma.Texto("BugReport.Sending");
            var resultado = await RelatoBug.EnviarAsync(descricao, _relatorio);
            BtnEnviar.Content = Idioma.Texto("BugReport.Send");
            BtnEnviar.IsEnabled = true;

            if (resultado.Falha != null)
            {
                this.MostrarErroInline(LblErro, Idioma.Formatar("BugReport.Error", resultado.Falha));
                return;
            }

            await CaixaMensagem.MostrarAsync(this, Idioma.Formatar("BugReport.Sent", resultado.UrlIssue ?? ""),
                Idioma.Texto("BugReport.Title"), TipoMensagem.Info);
            Close(true);
        }
    }
}
