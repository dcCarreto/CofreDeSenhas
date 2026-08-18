using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Janelas
{
    internal sealed record OpcaoFrequenciaSincronizacao(int Minutos, string Rotulo)
    {
        public override string ToString() => Rotulo;
    }

    public partial class JanelaSincronizacao : Window
    {
        private static readonly int[] OpcoesFrequencia = { 5, 15, 30, 60 };

        private ServicoSincronizacao? _servicoAtual;
        private readonly Action<ServicoSincronizacao?> _definirServico;
        private readonly Func<Task<bool>> _sincronizarAgora;
        private readonly Func<bool> _estaSincronizando;
        // internal só pra teste conferir que o delegate certo foi injetado, sem
        // precisar simular o seletor de pasta nativo que vem antes dele em
        // Ativar_Click — ver App.Testes (InternalsVisibleTo).
        internal readonly Func<Window, Task<bool>> _abrirDialogoAninhado;
        private bool _carregandoPreferencias;

        public JanelaSincronizacao(ServicoSincronizacao? servicoAtual, Action<ServicoSincronizacao?> definirServico,
            Func<Task<bool>> sincronizarAgora, Func<bool> estaSincronizando, Func<Window, Task<bool>> abrirDialogoAninhado)
        {
            _servicoAtual = servicoAtual;
            _definirServico = definirServico ?? throw new ArgumentNullException(nameof(definirServico));
            _sincronizarAgora = sincronizarAgora ?? throw new ArgumentNullException(nameof(sincronizarAgora));
            _estaSincronizando = estaSincronizando ?? throw new ArgumentNullException(nameof(estaSincronizando));
            _abrirDialogoAninhado = abrirDialogoAninhado ?? throw new ArgumentNullException(nameof(abrirDialogoAninhado));

            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);

            AtualizarEstado();

            this.FecharComEsc();
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e) => this.HabilitarArraste(e);

        private void Fechar_Click(object? sender, RoutedEventArgs e) => Close(false);

        private void AtualizarEstado()
        {
            var perfil = Preferencias.Sincronizacao;
            var ativa = perfil != null && _servicoAtual != null;

            PainelInativo.IsVisible = !ativa;
            PainelAtivo.IsVisible = ativa;

            if (!ativa || perfil == null)
                return;

            LblPasta.Text = perfil.Pasta;
            MontarFrequencia(perfil.FrequenciaMinutos);
            AtualizarUltimaSincronizacao(perfil);
        }

        private void MontarFrequencia(int atual)
        {
            _carregandoPreferencias = true;

            var opcoes = OpcoesFrequencia
                .Select(m => new OpcaoFrequenciaSincronizacao(m, Idioma.Formatar("Sync.EveryMinutes", m)))
                .ToList();
            CmbFrequencia.ItemsSource = opcoes;

            var indice = opcoes.FindIndex(o => o.Minutos == atual);
            CmbFrequencia.SelectedIndex = indice >= 0 ? indice : 1;

            _carregandoPreferencias = false;
        }

        private void AtualizarUltimaSincronizacao(PerfilSincronizacao perfil)
        {
            LblUltimaSincronizacao.Text = perfil.UltimaSincronizacao.HasValue
                ? Idioma.Formatar("Sync.LastDone", FormatarData(perfil.UltimaSincronizacao.Value))
                : Idioma.Texto("Sync.NeverDone");
        }

        private static string FormatarData(DateTime dataUtc) =>
            dataUtc.ToLocalTime().ToString("g", Idioma.CulturaAtual);

        private void Frequencia_Alterada(object? sender, SelectionChangedEventArgs e)
        {
            if (_carregandoPreferencias || CmbFrequencia.SelectedItem is not OpcaoFrequenciaSincronizacao opcao)
                return;
            if (Preferencias.Sincronizacao is not { } perfil)
                return;

            perfil.FrequenciaMinutos = opcao.Minutos;
            Preferencias.Salvar();
        }

        private async void Ativar_Click(object? sender, RoutedEventArgs e)
        {
            var pastas = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = Idioma.Texto("Sync.ChooseFolder"),
                AllowMultiple = false
            });
            if (pastas.Count == 0 || pastas[0].TryGetLocalPath() is not { } pasta)
                return;

            var dlg = new JanelaConfirmarSenhaMestra(
                Idioma.Texto("Sync.ConfirmMasterTitle"),
                Idioma.Texto("Sync.ConfirmMasterInstruction"),
                Idioma.Texto("Sync.Confirm"));

            // Via _abrirDialogoAninhado (que delega pro AbrirDialogoAsync de
            // JanelaPrincipal) em vez de ShowDialog direto — sem isto, o
            // MonitorInatividade nunca soube de teclas digitadas neste diálogo
            // aninhado (ele só é vinculado a diálogos abertos diretamente pela janela
            // principal), e o bloqueio automático podia fechá-lo à força enquanto o
            // usuário ainda estava digitando a senha mestra de confirmação.
            if (!await _abrirDialogoAninhado(dlg))
                return;

            try
            {
                var caminho = Path.Combine(pasta, ServicoSincronizacao.NomeArquivo);
                var cabecalho = await ServicoSincronizacao.LerCabecalhoAsync(caminho);

                byte[] salt;
                string? kdf;
                int iteracoes;
                int? memoriaKb;
                int? paralelismo;
                if (cabecalho.HasValue)
                {
                    (salt, kdf, iteracoes, memoriaKb, paralelismo) = cabecalho.Value;
                }
                else
                {
                    var padrao = ServicoSincronizacao.ParametrosPadrao();
                    salt = ServicoSincronizacao.GerarSalt();
                    kdf = padrao.Kdf;
                    iteracoes = padrao.Iteracoes;
                    memoriaKb = padrao.MemoriaKb;
                    paralelismo = padrao.Paralelismo;
                }

                var chave = ServicoSincronizacao.DerivarChave(dlg.SenhaConfirmada, salt, kdf, iteracoes, memoriaKb, paralelismo);
                var servico = new ServicoSincronizacao(new ServicoCriptografia(chave));

                Preferencias.Sincronizacao = new PerfilSincronizacao
                {
                    Pasta = pasta,
                    Salt = Convert.ToBase64String(salt),
                    Kdf = kdf,
                    Iteracoes = iteracoes,
                    MemoriaKb = memoriaKb,
                    Paralelismo = paralelismo,
                    FrequenciaMinutos = 15
                };
                Preferencias.Salvar();

                _servicoAtual = servico;
                _definirServico(servico);

                await _sincronizarAgora();
                AtualizarEstado();
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Sync.EnableError", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async void SincronizarAgora_Click(object? sender, RoutedEventArgs e)
        {
            if (_estaSincronizando())
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Sync.AlreadyInProgress"), Idioma.Texto("Sync.Title"), TipoMensagem.Aviso);
                return;
            }

            var sucesso = await _sincronizarAgora();
            if (Preferencias.Sincronizacao is { } perfil)
                AtualizarUltimaSincronizacao(perfil);

            if (sucesso)
                await CaixaMensagem.MostrarAsync(this, Idioma.Texto("Sync.Success"), Idioma.Texto("Common.Success"));
        }

        private async void Desativar_Click(object? sender, RoutedEventArgs e)
        {
            // Mesma checagem que SincronizarAgora_Click já faz antes de agir — sem
            // ela, zerar a chave enquanto uma sincronização está em andamento
            // derrubava esse ciclo com ObjectDisposedException.
            if (_estaSincronizando())
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Sync.AlreadyInProgress"), Idioma.Texto("Sync.Title"), TipoMensagem.Aviso);
                return;
            }

            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Texto("Sync.DisableConfirm"), Idioma.Texto("Sync.Disable"));
            if (!confirmar)
                return;

            // Uma sincronização periódica silenciosa pode ter começado enquanto o
            // diálogo de confirmação estava aberto (o timer de sync não é bloqueado
            // por ele) — reconfere antes de agir.
            if (_estaSincronizando())
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Sync.AlreadyInProgress"), Idioma.Texto("Sync.Title"), TipoMensagem.Aviso);
                return;
            }

            Preferencias.Sincronizacao = null;
            Preferencias.Salvar();

            _servicoAtual?.ZerarChave();
            _servicoAtual = null;
            _definirServico(null);

            AtualizarEstado();
        }
    }
}
