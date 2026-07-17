using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaConexaoBanco : Window
    {
        private readonly TipoBanco _tipo;
        private readonly ServicoBancoDados _bd = new();

        private TextBox? _txtArquivo;
        private TextBox? _txtHost;
        private TextBox? _txtPorta;
        private TextBox? _txtBanco;
        private TextBox? _txtUsuario;
        private TextBox? _txtSenha;

        public ConexaoBanco? Conexao { get; private set; }

        public JanelaConexaoBanco(TipoBanco tipo)
        {
            _tipo = tipo;

            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);

            MontarFormulario();
            Idioma.Alterado += Idioma_Alterado;
            Closed += (s, e) => Idioma.Alterado -= Idioma_Alterado;

            this.FecharComEsc();

            Opened += (s, e) => (_txtArquivo ?? _txtHost)?.Focus();
        }

        private void MontarFormulario()
        {
            var arquivoAtual = _txtArquivo?.Text;
            var hostAtual = _txtHost?.Text;
            var portaAtual = _txtPorta?.Text;
            var bancoAtual = _txtBanco?.Text;
            var usuarioAtual = _txtUsuario?.Text;
            var senhaAtual = _txtSenha?.Text;

            Campos.Children.Clear();
            var provedor = ProvedorBanco.De(_tipo);
            LblTitulo.Text = Idioma.Formatar("Db.ConnectProviderTitle", provedor.Rotulo);

            var perfil = Preferencias.UltimoBanco;
            bool temPerfil = perfil != null && perfil.Tipo == _tipo;

            if (provedor.UsaArquivo)
            {
                Campos.Children.Add(Rotulo(Idioma.Texto("Db.DatabaseFile")));

                _txtArquivo = new TextBox { Text = arquivoAtual ?? (temPerfil ? perfil!.Banco : null) };
                _txtArquivo.Classes.Add("campo");
                AutomationProperties.SetName(_txtArquivo, Idioma.Texto("Db.DatabaseFile"));
                AutomationProperties.SetIsRequiredForForm(_txtArquivo, true);

                var btnProcurar = new Button { Content = Idioma.Texto("Db.Browse"), Width = 110, Height = 38 };
                btnProcurar.Classes.Add("secundario");
                btnProcurar.Margin = new Thickness(8, 0, 0, 0);
                AutomationProperties.SetName(btnProcurar, Idioma.Texto("Db.Browse"));
                btnProcurar.Click += Procurar_Click;

                var grade = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
                grade.Children.Add(_txtArquivo);
                Grid.SetColumn(btnProcurar, 1);
                grade.Children.Add(btnProcurar);
                Campos.Children.Add(grade);
            }
            else
            {
                _txtHost = AdicionarCampo(Idioma.Texto("Db.Host"), hostAtual ?? (temPerfil ? perfil!.Host : "localhost"));
                _txtPorta = AdicionarCampo(Idioma.Texto("Db.Port"),
                    portaAtual ?? (temPerfil && perfil!.Porta > 0 ? perfil.Porta : provedor.PortaPadrao).ToString());
                _txtBanco = AdicionarCampo(Idioma.Texto("Db.Database"), bancoAtual ?? (temPerfil ? perfil!.Banco : null));
                _txtUsuario = AdicionarCampo(Idioma.Texto("Db.User"), usuarioAtual ?? (temPerfil ? perfil!.Usuario : null));
                _txtSenha = AdicionarCampo(Idioma.Texto("Db.Password"), senhaAtual, senha: true);
            }
        }

        private void Idioma_Alterado(object? sender, EventArgs e)
        {
            MontarFormulario();
            MostrarErro("");
        }

        private static TextBlock Rotulo(string texto) => new()
        {
            Text = texto,
            FontSize = 12,
            Foreground = Tema.Pincel(Tema.TextSecondary),
            Margin = new Thickness(0, 8, 0, 4)
        };

        private TextBox AdicionarCampo(string rotulo, string? valor, bool senha = false)
        {
            Campos.Children.Add(Rotulo(rotulo));

            var caixa = new TextBox { Text = valor, Margin = new Thickness(0, 0, 0, 2) };
            caixa.Classes.Add("campo");
            AutomationProperties.SetName(caixa, rotulo);
            AutomationProperties.SetIsRequiredForForm(caixa, !senha);
            if (senha)
            {
                caixa.PasswordChar = '●';
                caixa.Classes.Add("revealPasswordButton");
                AutomationProperties.SetHelpText(caixa, Idioma.Texto("A11y.PasswordFieldHelp"));
            }

            Campos.Children.Add(caixa);
            return caixa;
        }

        private async void Procurar_Click(object? sender, RoutedEventArgs e)
        {
            var arquivo = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = Idioma.Texto("Db.SQLitePickerTitle"),
                SuggestedFileName = "cofre.db",
                DefaultExtension = "db",
                ShowOverwritePrompt = false,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType(Idioma.Texto("Common.SQLiteDatabase")) { Patterns = new[] { "*.db", "*.sqlite", "*.sqlite3" } },
                    new FilePickerFileType(Idioma.Texto("Common.AllFiles")) { Patterns = new[] { "*" } }
                }
            });

            if (arquivo != null && _txtArquivo != null)
                _txtArquivo.Text = arquivo.Path.LocalPath;
        }

        private async void Testar_Click(object? sender, RoutedEventArgs e)
        {
            var cfg = MontarConexao();
            if (cfg == null) return;

            await ComOcupado(async () =>
            {
                try
                {
                    await _bd.TestarConexaoAsync(cfg);
                    MostrarSucesso(Idioma.Texto("Db.ConnectionSuccess"));
                }
                catch (Exception ex)
                {
                    MostrarErro(Idioma.Formatar("Db.ConnectionFailed", PrimeiraLinha(ex.Message)));
                }
            });
        }

        private async void Conectar_Click(object? sender, RoutedEventArgs e)
        {
            var cfg = MontarConexao();
            if (cfg == null) return;

            await ComOcupado(async () =>
            {
                try
                {
                    await _bd.TestarConexaoAsync(cfg);

                    if (!await _bd.TabelaExisteAsync(cfg))
                    {
                        var criar = await CaixaMensagem.ConfirmarAsync(this,
                            Idioma.Formatar("Db.TableMissing", ServicoBancoDados.NomeTabela),
                            Idioma.Texto("Db.CreateTableTitle"));
                        if (!criar)
                        {
                            MostrarErro(Idioma.Texto("Db.ConnectionCanceledNoTable"));
                            return;
                        }
                        await _bd.CriarTabelaAsync(cfg);
                    }

                    await _bd.GarantirColunasAsync(cfg);

                    Conexao = cfg;
                    Close(true);
                }
                catch (Exception ex)
                {
                    MostrarErro(Idioma.Formatar("Db.CannotConnect", PrimeiraLinha(ex.Message)));
                }
            });
        }

        private ConexaoBanco? MontarConexao()
        {
            if (ProvedorBanco.De(_tipo).UsaArquivo)
            {
                var arquivo = _txtArquivo?.Text?.Trim();
                if (string.IsNullOrWhiteSpace(arquivo))
                {
                    MostrarErro(Idioma.Texto("Db.ErrorFileRequired"));
                    return null;
                }
                return new ConexaoBanco { Tipo = _tipo, Banco = arquivo };
            }

            var host = _txtHost?.Text?.Trim();
            var banco = _txtBanco?.Text?.Trim();
            var usuario = _txtUsuario?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(banco) || string.IsNullOrWhiteSpace(usuario))
            {
                MostrarErro(Idioma.Texto("Db.ErrorFieldsRequired"));
                return null;
            }

            if (!int.TryParse(_txtPorta?.Text?.Trim(), out var porta) || porta <= 0)
            {
                MostrarErro(Idioma.Texto("Db.ErrorInvalidPort"));
                return null;
            }

            return new ConexaoBanco
            {
                Tipo = _tipo,
                Host = host,
                Porta = porta,
                Banco = banco,
                Usuario = usuario,
                SenhaServidor = _txtSenha?.Text ?? ""
            };
        }

        private async Task ComOcupado(Func<Task> acao)
        {
            BtnTestar.IsEnabled = false;
            BtnConectar.IsEnabled = false;
            try { await acao(); }
            finally
            {
                BtnTestar.IsEnabled = true;
                BtnConectar.IsEnabled = true;
            }
        }

        private void MostrarErro(string msg)
        {
            LblErro.Foreground = Tema.Pincel(Tema.StrengthWeak);
            LblErro.Text = msg;
            AutomationProperties.SetName(LblErro, msg);
            if (!string.IsNullOrWhiteSpace(msg))
                Acessibilidade.Anunciar(this, msg, assertivo: true);
        }

        private void MostrarSucesso(string msg)
        {
            LblErro.Foreground = Tema.Pincel(Tema.StatusLocal);
            LblErro.Text = msg;
            AutomationProperties.SetName(LblErro, msg);
            Acessibilidade.Anunciar(this, msg, assertivo: true);
        }

        private static string PrimeiraLinha(string texto)
        {
            var quebra = texto.IndexOf('\n');
            return quebra < 0 ? texto : texto[..quebra].TrimEnd('\r');
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e) => this.HabilitarArraste(e, origem => origem is TextBox);

        private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);
    }
}
