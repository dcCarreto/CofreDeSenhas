using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Janelas
{
    internal sealed record OpcaoFrequenciaBackup(FrequenciaBackup Valor, string Rotulo)
    {
        public override string ToString() => Rotulo;
    }

    public partial class JanelaBackup : Window
    {
        private static readonly int[] OpcoesMaximo = { 5, 10, 20 };

        private readonly IPersistenciaLocal _persistencia;
        private readonly Func<Task<List<Senha>>> _obterSenhasAtuais;
        private readonly byte[] _chaveMestra;
        private bool _carregandoPreferencias;

        public string? BackupParaRestaurar { get; private set; }

        public JanelaBackup(IPersistenciaLocal persistencia, Func<Task<List<Senha>>> obterSenhasAtuais,
            byte[] chaveMestra, bool permiteRestaurar)
        {
            _persistencia = persistencia ?? throw new ArgumentNullException(nameof(persistencia));
            _obterSenhasAtuais = obterSenhasAtuais ?? throw new ArgumentNullException(nameof(obterSenhasAtuais));
            _chaveMestra = chaveMestra ?? throw new ArgumentNullException(nameof(chaveMestra));

            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);

            LblAvisoBanco.IsVisible = !permiteRestaurar;

            MontarFrequencia();
            MontarMaximo();
            AtualizarUltimoBackup();
            AtualizarListaBackups(permiteRestaurar);

            this.FecharComEsc();

            Opened += (s, e) => CmbFrequencia.Focus();
        }

        private void MontarFrequencia()
        {
            _carregandoPreferencias = true;

            var opcoes = new List<OpcaoFrequenciaBackup>
            {
                new(FrequenciaBackup.Manual, Idioma.Texto("Backup.Manual")),
                new(FrequenciaBackup.Diario, Idioma.Texto("Backup.Daily")),
                new(FrequenciaBackup.Semanal, Idioma.Texto("Backup.Weekly"))
            };
            CmbFrequencia.ItemsSource = opcoes;

            var atual = Preferencias.FrequenciaBackupAtual;
            var indice = opcoes.FindIndex(o => o.Valor == atual);
            CmbFrequencia.SelectedIndex = indice >= 0 ? indice : 2;

            _carregandoPreferencias = false;
        }

        private void MontarMaximo()
        {
            _carregandoPreferencias = true;

            CmbMaximo.ItemsSource = OpcoesMaximo;
            var indice = Array.IndexOf(OpcoesMaximo, Preferencias.MaximoBackups);
            CmbMaximo.SelectedIndex = indice >= 0 ? indice : 1;

            _carregandoPreferencias = false;
        }

        private void Frequencia_Alterada(object? sender, SelectionChangedEventArgs e)
        {
            if (_carregandoPreferencias || CmbFrequencia.SelectedItem is not OpcaoFrequenciaBackup opcao)
                return;

            Preferencias.FrequenciaBackup = opcao.Valor.ToString();
            Preferencias.Salvar();
        }

        private void Maximo_Alterado(object? sender, SelectionChangedEventArgs e)
        {
            if (_carregandoPreferencias || CmbMaximo.SelectedItem is not int valor)
                return;

            Preferencias.MaximoBackups = valor;
            Preferencias.Salvar();
        }

        private void AtualizarUltimoBackup()
        {
            var backups = _persistencia.ListarBackups();
            LblUltimoBackup.Text = backups.Count == 0
                ? Idioma.Texto("Backup.NeverDone")
                : Idioma.Formatar("Backup.LastDone", FormatarData(backups[0].DataUtc));
        }

        private void AtualizarListaBackups(bool permiteRestaurar)
        {
            var backups = _persistencia.ListarBackups();
            PainelBackups.Children.Clear();
            LblSemBackups.IsVisible = backups.Count == 0;

            foreach (var backup in backups)
                PainelBackups.Children.Add(CriarLinhaBackup(backup, permiteRestaurar));
        }

        private Control CriarLinhaBackup(InfoBackup backup, bool permiteRestaurar)
        {
            var dataFormatada = FormatarData(backup.DataUtc);

            var lblData = new TextBlock
            {
                Text = dataFormatada,
                FontSize = 13,
                Foreground = Tema.Pincel(Tema.TextPrimary),
                VerticalAlignment = VerticalAlignment.Center
            };

            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            grid.Children.Add(lblData);

            if (permiteRestaurar)
            {
                var btnRestaurar = new Button
                {
                    Content = Idioma.Texto("Backup.Restore"),
                    Height = 34,
                    FontSize = 12
                };
                btnRestaurar.Classes.Add("plano");
                AutomationProperties.SetName(btnRestaurar, Idioma.Formatar("Backup.RestoreFrom", dataFormatada));
                btnRestaurar.Click += async (s, e) => await RestaurarAsync(backup);

                Grid.SetColumn(btnRestaurar, 1);
                grid.Children.Add(btnRestaurar);
            }

            return new Border
            {
                Padding = new Thickness(12, 8),
                CornerRadius = new CornerRadius(8),
                Background = Tema.Pincel(Tema.CardBackground),
                BorderBrush = Tema.Pincel(Tema.InputBorder),
                BorderThickness = new Thickness(1),
                Child = grid
            };
        }

        private async void BackupAgora_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var senhas = await _obterSenhasAtuais();
                await _persistencia.BackupAutomaticoAsync(senhas, _chaveMestra, Preferencias.MaximoBackups);
                AtualizarUltimoBackup();
                AtualizarListaBackups(!LblAvisoBanco.IsVisible);
                await CaixaMensagem.MostrarAsync(this, Idioma.Texto("Backup.Success"), Idioma.Texto("Common.Success"));
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Backup.Error", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async Task RestaurarAsync(InfoBackup backup)
        {
            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Formatar("Backup.RestoreConfirm", FormatarData(backup.DataUtc)),
                Idioma.Texto("Backup.RestoreTitle"));
            if (!confirmar)
                return;

            BackupParaRestaurar = backup.Caminho;
            Close(true);
        }

        private static string FormatarData(DateTime dataUtc) =>
            dataUtc.ToLocalTime().ToString("g", Idioma.CulturaAtual);

        private void Arrastar(object? sender, PointerPressedEventArgs e) => this.HabilitarArraste(e);

        private void Fechar_Click(object? sender, RoutedEventArgs e) => Close(false);
    }
}
