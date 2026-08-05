using System.Security.Cryptography;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CofreDeSenhas.Controles;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaLogin : Window
    {
        private readonly AutenticacaoMestra _auth;
        private readonly Action<byte[], string?> _aoAutenticar;
        private readonly bool _primeiroAcesso;
        private readonly ServicoDesbloqueioBiometrico _biometria = new();

        private BiometriaModo _modoBiometria = BiometriaModo.Desbloquear;
        private int _tentativas;

        public JanelaLogin(AutenticacaoMestra auth, Action<byte[], string?> aoAutenticar)
        {
            _auth = auth;
            _aoAutenticar = aoAutenticar;

            new ServicoMudancaSenhaMestra(_auth.PastaApp).RestaurarBackupOrfaoSeNecessario();

            _primeiroAcesso = !auth.ExisteSenhaMestra();

            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);
            Acessibilidade.RegistrarAnunciador(this, LblAnuncioLeitorTela);

            Gerador.PermiteSalvar = false;
            Gerador.ShowHeader = false;

            CmbIdioma.ItemsSource = Idioma.Idiomas;
            CmbIdioma.SelectedItem = Idioma.Atual;
            CmbIdioma.SelectionChanged += Idioma_Alterado;
            Idioma.Alterado += IdiomaGlobal_Alterado;

            AtualizarTextos();
            ConfigurarAcessibilidadeLeitorTela();
            PainelConfirmar.IsVisible = _primeiroAcesso;
            Medidor.IsVisible = _primeiroAcesso;
            LblPrimeiroAcessoAviso.IsVisible = _primeiroAcesso;
            BtnRestaurarBanco.IsVisible = _primeiroAcesso;
            TxtSenha.TextChanged += (s, e) =>
            {
                if (_primeiroAcesso) Medidor.Avaliar(TxtSenha.Text);
            };

            BtnAcessibilidade.Flyout!.Opened += (s, e) =>
                Acessibilidade.MarcarMenus(MenuDaltonismoLogin, MenuEscalaLogin, MenuAltoContrasteLogin,
                    MenuReduzirAnimacoesLogin, MenuLeitorTelaLogin);
            Acessibilidade.Alterado += Acessibilidade_Alterado;
            Closed += (s, e) =>
            {
                Idioma.Alterado -= IdiomaGlobal_Alterado;
                Acessibilidade.Alterado -= Acessibilidade_Alterado;
            };

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter && BtnPrincipal.IsEnabled)
                    _ = ConfirmarAsync();
            };

            Opened += async (s, e) =>
            {
                TxtSenha.Focus();
                await ConfigurarBotaoBiometriaAsync();
            };
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                return;

            if (e.Source is Visual origem && OrigemInterativa(origem))
                return;

            BeginMoveDrag(e);
        }

        private static bool OrigemInterativa(Visual origem) =>
            origem.FindAncestorOfType<Button>(true) != null ||
            origem.FindAncestorOfType<TextBox>(true) != null ||
            origem.FindAncestorOfType<ComboBox>(true) != null ||
            origem.FindAncestorOfType<CustomSlider>(true) != null ||
            origem.FindAncestorOfType<CustomToggle>(true) != null ||
            origem.FindAncestorOfType<ScrollViewer>(true) != null;

        private void Fechar_Click(object? sender, RoutedEventArgs e) => Close();

        private void Acessibilidade_Alterado(object? sender, EventArgs e)
        {
            ConfigurarAcessibilidadeLeitorTela();
            Gerador.AtualizarTema();
        }

        private void Daltonismo_Alterado(object? sender, RoutedEventArgs e) => Acessibilidade.TratarClickDaltonismo(sender);

        private void Escala_Alterada(object? sender, RoutedEventArgs e) => Acessibilidade.TratarClickEscala(sender);

        private void AltoContraste_Alterado(object? sender, RoutedEventArgs e) => Acessibilidade.TratarClickAltoContraste(sender);

        private void ReduzirAnimacoes_Alterado(object? sender, RoutedEventArgs e) => Acessibilidade.TratarClickReducaoMovimento(sender);

        private void LeitorTela_Alterado(object? sender, RoutedEventArgs e) => Acessibilidade.TratarClickLeitorTela(this, sender);

        private async void Principal_Click(object? sender, RoutedEventArgs e) => await ConfirmarAsync();

        private async void Biometria_Click(object? sender, RoutedEventArgs e)
        {
            if (_modoBiometria == BiometriaModo.Ativar)
                await AtivarComBiometriaAsync();
            else
                await DesbloquearComBiometriaAsync();
        }

        private async void RestaurarBanco_Click(object? sender, RoutedEventArgs e) => await RestaurarDeBancoAsync();

        private async Task RestaurarDeBancoAsync()
        {
            LblErro.Text = "";

            var seletor = new JanelaSelecionarBanco();
            if (!await AbrirDialogoAsync<bool>(seletor) || seletor.Selecionado is not { } tipo)
                return;

            var dlgConexao = new JanelaConexaoBanco(tipo, modoRestauracao: true);
            if (!await AbrirDialogoAsync<bool>(dlgConexao) || dlgConexao.Conexao is not { } cfg)
                return;

            var bd = new ServicoBancoDados();
            AuthBanco? auth;
            try
            {
                auth = await bd.LerAuthAsync(cfg);
            }
            catch (Exception ex)
            {
                MostrarErro(ErrosUi.MensagemAmigavel(ex));
                return;
            }

            if (auth == null)
            {
                MostrarErro(Idioma.Texto("Db.RestoreNoAuthPublished"));
                return;
            }

            var dlgSenha = new JanelaConfirmarSenhaMestra(
                titulo: Idioma.Texto("Login.RestoreTitle"),
                instrucao: Idioma.Texto("Login.RestoreInstruction"),
                textoBotao: Idioma.Texto("Login.RestoreConfirm"),
                validador: senha => CryptographicOperations.FixedTimeEquals(
                    SHA256.HashData(AutenticacaoMestra.DerivarChaveDeParametros(
                        senha, auth.Salt, auth.Kdf, auth.Custo, auth.MemoriaKb, auth.Paralelismo)),
                    auth.Verificador));
            if (!await AbrirDialogoAsync<bool>(dlgSenha))
                return;

            var senhaConfirmada = dlgSenha.SenhaConfirmada;
            var chave = AutenticacaoMestra.DerivarChaveDeParametros(
                senhaConfirmada, auth.Salt, auth.Kdf, auth.Custo, auth.MemoriaKb, auth.Paralelismo);

            try
            {
                var cripto = new ServicoCriptografia(chave);
                var lista = await new RepositorioSenhaBanco(cfg, cripto).ListarTudoAsync();

                _auth.GravarAutenticacaoRestaurada(auth.Salt, auth.Verificador, auth.Kdf, auth.Custo, auth.MemoriaKb, auth.Paralelismo);
                await new PersistenciaLocal(cripto, _auth.PastaApp).SalvarSenhasAsync(lista, chave);

                Preferencias.UltimoBanco = new PerfilBanco
                {
                    Tipo = cfg.Tipo,
                    Host = cfg.Host,
                    Porta = cfg.Porta,
                    Banco = cfg.Banco,
                    Usuario = cfg.Usuario,
                    SenhaCifrada = cfg.Tipo == TipoBanco.SQLite || string.IsNullOrEmpty(cfg.SenhaServidor)
                        ? null
                        : cripto.Criptografar(cfg.SenhaServidor),
                    Conectado = true,
                    ReconciliacaoInicialConcluida = true,
                    ExigirCertificadoValido = cfg.ExigirCertificadoValido
                };
                Preferencias.Salvar();
            }
            catch (Exception ex)
            {
                CryptographicOperations.ZeroMemory(chave);
                MostrarErro(ErrosUi.MensagemAmigavel(ex));
                return;
            }

            _aoAutenticar(chave, senhaConfirmada);
        }

        private async Task<T> AbrirDialogoAsync<T>(Window dialogo)
        {
            Scrim.Mostrar(this);
            try
            {
                return await dialogo.ShowDialog<T>(this);
            }
            finally
            {
                Scrim.Ocultar(this);
            }
        }

        private void Idioma_Alterado(object? sender, SelectionChangedEventArgs e)
        {
            if (CmbIdioma.SelectedItem is not IdiomaDisponivel idioma ||
                string.Equals(idioma.Codigo, Idioma.Atual.Codigo, StringComparison.OrdinalIgnoreCase))
                return;

            Idioma.Definir(idioma.Codigo);
            Preferencias.Idioma = Idioma.Atual.Codigo;
            Preferencias.Salvar();
        }

        private void IdiomaGlobal_Alterado(object? sender, EventArgs e)
        {
            CmbIdioma.SelectedItem = Idioma.Atual;
            AtualizarTextos();
            ConfigurarAcessibilidadeLeitorTela();
            LblErro.Text = "";
        }

        private void AtualizarTextos()
        {
            LblSubtitulo.Text = Idioma.Texto(_primeiroAcesso
                ? "Login.SubtitleCreate"
                : "Login.SubtitleUnlock");
            BtnPrincipal.Content = Idioma.Texto(_primeiroAcesso
                ? "Login.CreateVault"
                : "Login.Unlock");
            AtualizarTextoBiometria();
            AutomationProperties.SetName(BtnPrincipal, BtnPrincipal.Content?.ToString() ?? "");
        }

        private void ConfigurarAcessibilidadeLeitorTela()
        {
            AutomationProperties.SetName(CmbIdioma, Idioma.Texto("Settings.Language"));
            AutomationProperties.SetHelpText(MenuLeitorTelaLogin, Idioma.Texto("Access.ScreenReaderHelp"));
            AutomationProperties.SetHelpText(TxtSenha, Idioma.Texto("A11y.PasswordFieldHelp"));
            AutomationProperties.SetHelpText(TxtConfirmar, Idioma.Texto("A11y.PasswordFieldHelp"));
            AutomationProperties.SetLiveSetting(LblErro, AutomationLiveSetting.Assertive);
            AutomationProperties.SetName(BtnPrincipal, BtnPrincipal.Content?.ToString() ?? "");
            AutomationProperties.SetName(BtnRestaurarBanco, BtnRestaurarBanco.Content?.ToString() ?? "");
            AtualizarTextoBiometria();
        }

        private async Task ConfirmarAsync()
        {
            LblErro.Text = "";
            var senha = TxtSenha.Text ?? "";

            if (_primeiroAcesso)
            {
                if (senha.Length < AutenticacaoMestra.TamanhoMinimoSenha)
                {
                    MostrarErro(Idioma.Texto("Login.Error.PasswordLength"));
                    return;
                }
                if (senha != (TxtConfirmar.Text ?? ""))
                {
                    MostrarErro(Idioma.Texto("Login.Error.PasswordMismatch"));
                    return;
                }

                byte[] chave;
                try
                {
                    chave = _auth.CriarSenhaMestra(senha);
                }
                catch (Exception ex)
                {
                    MostrarErro(ErrosUi.MensagemAmigavel(ex));
                    return;
                }

                await QrBackup.OferecerSalvarAsync(this, senha);
                await OferecerBiometriaAsync(chave);
                _aoAutenticar(chave, senha);
            }
            else
            {
                if (string.IsNullOrEmpty(senha))
                {
                    MostrarErro(Idioma.Texto("Login.Error.MasterPasswordRequired"));
                    return;
                }

                var chave = _auth.Autenticar(senha);
                if (chave != null)
                {
                    var chaveMigrada = await MigrarKdfSeNecessarioAsync(senha);
                    _aoAutenticar(chaveMigrada ?? chave, senha);
                    return;
                }

                _tentativas++;
                if (_tentativas >= 5)
                {
                    MostrarErro(Idioma.Texto("Login.Error.TooManyAttempts"));
                    BtnPrincipal.IsEnabled = false;
                    var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                    t.Tick += (s, ev) =>
                    {
                        BtnPrincipal.IsEnabled = true;
                        _tentativas = 0;
                        LblErro.Text = "";
                        t.Stop();
                    };
                    t.Start();
                }
                else
                {
                    MostrarErro(Idioma.Formatar("Login.Error.WrongPassword", _tentativas));
                }

                TxtSenha.SelectAll();
                TxtSenha.Focus();
            }
        }

        private async Task<byte[]?> MigrarKdfSeNecessarioAsync(string senha)
        {
            try
            {
                var novaChave = await new ServicoMudancaSenhaMestra(_auth.PastaApp)
                    .MigrarKdfSeNecessarioAsync(senha);
                if (novaChave == null)
                    return null;

                if (_biometria.EstaHabilitado)
                {
                    await _biometria.DesabilitarAsync();
                    await CaixaMensagem.MostrarAsync(this,
                        Idioma.Texto("Biometric.DisabledAfterKdfUpgrade"),
                        Idioma.Texto("Biometric.Title"),
                        TipoMensagem.Info);
                }

                return novaChave;
            }
            catch
            {
                return null;
            }
        }

        private async Task DesbloquearComBiometriaAsync()
        {
            LblErro.Text = "";
            BtnBiometria.IsEnabled = false;
            BtnPrincipal.IsEnabled = false;

            var resultado = await _biometria.DesbloquearAsync(this, _auth);
            if (resultado.Sucesso && resultado.Chave != null)
            {
                _aoAutenticar(resultado.Chave, null);
                return;
            }

            BtnBiometria.IsEnabled = true;
            BtnPrincipal.IsEnabled = true;
            await ConfigurarBotaoBiometriaAsync();

            if (!resultado.Cancelado)
                MostrarErro(resultado.Mensagem ?? Idioma.Texto("Biometric.Unavailable"));

            TxtSenha.Focus();
        }

        private async Task AtivarComBiometriaAsync()
        {
            LblErro.Text = "";
            var senha = TxtSenha.Text ?? "";
            if (string.IsNullOrEmpty(senha))
            {
                MostrarErro(Idioma.Texto("Login.Error.MasterPasswordRequired"));
                TxtSenha.Focus();
                return;
            }

            var chave = _auth.Autenticar(senha);
            if (chave == null)
            {
                MostrarErro(Idioma.Texto("Qr.ErrorMasterIncorrect"));
                TxtSenha.SelectAll();
                TxtSenha.Focus();
                return;
            }

            BtnBiometria.IsEnabled = false;
            BtnPrincipal.IsEnabled = false;

            var resultado = await _biometria.HabilitarAsync(this, chave);

            BtnBiometria.IsEnabled = true;
            BtnPrincipal.IsEnabled = true;

            if (resultado.Sucesso)
            {
                _aoAutenticar(chave, senha);
                return;
            }

            if (!resultado.Cancelado)
                MostrarErro(resultado.Mensagem ?? Idioma.Texto("Biometric.Unavailable"));

            TxtSenha.Focus();
        }

        private async Task ConfigurarBotaoBiometriaAsync()
        {
            if (_primeiroAcesso || !_biometria.SistemaSuportado)
            {
                BtnBiometria.IsVisible = false;
                LblBiometriaHint.IsVisible = false;
                return;
            }

            if (_biometria.EstaHabilitado)
                _modoBiometria = BiometriaModo.Desbloquear;
            else if (await _biometria.PodeConfigurarAsync())
                _modoBiometria = BiometriaModo.Ativar;
            else
            {
                BtnBiometria.IsVisible = false;
                LblBiometriaHint.IsVisible = false;
                return;
            }

            BtnBiometria.IsVisible = true;
            AtualizarTextoBiometria();
        }

        private void AtualizarTextoBiometria()
        {
            bool ativar = _modoBiometria == BiometriaModo.Ativar;
            LblBiometria.Text = Idioma.Texto(ativar ? "Login.EnableWindowsHello" : "Login.WindowsHello");
            LblBiometriaHint.Text = Idioma.Texto("Login.WindowsHelloHint");
            LblBiometriaHint.IsVisible = ativar && BtnBiometria.IsVisible;
            AutomationProperties.SetName(BtnBiometria, LblBiometria.Text ?? "");
            AutomationProperties.SetHelpText(BtnBiometria, LblBiometriaHint.Text ?? "");
        }

        private async Task OferecerBiometriaAsync(byte[] chave)
        {
            if (_biometria.EstaHabilitado || !await _biometria.PodeConfigurarAsync())
                return;

            var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                Idioma.Texto("Biometric.EnableOffer"),
                Idioma.Texto("Biometric.Title"),
                TipoMensagem.Info);
            if (!confirmar)
                return;

            var resultado = await _biometria.HabilitarAsync(this, chave);
            if (!resultado.Sucesso && !resultado.Cancelado)
            {
                await CaixaMensagem.MostrarAsync(this,
                    resultado.Mensagem ?? Idioma.Texto("Biometric.Unavailable"),
                    Idioma.Texto("Biometric.Title"),
                    TipoMensagem.Aviso);
            }
        }

        private void MostrarErro(string msg) => this.MostrarErroInline(LblErro, msg);

        private enum BiometriaModo
        {
            Desbloquear,
            Ativar
        }
    }
}
