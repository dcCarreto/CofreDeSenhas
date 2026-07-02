using Avalonia;
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

            MontarFormulario();

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) Close(false);
            };

            Opened += (s, e) => (_txtArquivo ?? _txtHost)?.Focus();
        }

        private void MontarFormulario()
        {
            var provedor = ProvedorBanco.De(_tipo);
            LblTitulo.Text = $"Conectar — {provedor.Rotulo}";

            var perfil = Preferencias.UltimoBanco;
            bool temPerfil = perfil != null && perfil.Tipo == _tipo;

            if (provedor.UsaArquivo)
            {
                Campos.Children.Add(Rotulo("Arquivo do banco (.db)"));

                _txtArquivo = new TextBox { Text = temPerfil ? perfil!.Banco : null };
                _txtArquivo.Classes.Add("campo");

                var btnProcurar = new Button { Content = "Procurar…", Width = 110, Height = 38 };
                btnProcurar.Classes.Add("secundario");
                btnProcurar.Margin = new Thickness(8, 0, 0, 0);
                btnProcurar.Click += Procurar_Click;

                var grade = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
                grade.Children.Add(_txtArquivo);
                Grid.SetColumn(btnProcurar, 1);
                grade.Children.Add(btnProcurar);
                Campos.Children.Add(grade);
            }
            else
            {
                _txtHost = AdicionarCampo("Host", temPerfil ? perfil!.Host : "localhost");
                _txtPorta = AdicionarCampo("Porta",
                    (temPerfil && perfil!.Porta > 0 ? perfil.Porta : provedor.PortaPadrao).ToString());
                _txtBanco = AdicionarCampo("Banco de dados", temPerfil ? perfil!.Banco : null);
                _txtUsuario = AdicionarCampo("Usuário", temPerfil ? perfil!.Usuario : null);
                _txtSenha = AdicionarCampo("Senha", null, senha: true);
            }
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
            if (senha)
            {
                caixa.PasswordChar = '●';
                caixa.Classes.Add("revealPasswordButton");
            }

            Campos.Children.Add(caixa);
            return caixa;
        }

        private async void Procurar_Click(object? sender, RoutedEventArgs e)
        {
            var arquivo = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Arquivo do banco SQLite",
                SuggestedFileName = "cofre.db",
                DefaultExtension = "db",
                ShowOverwritePrompt = false,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Banco SQLite") { Patterns = new[] { "*.db", "*.sqlite", "*.sqlite3" } },
                    new FilePickerFileType("Todos os arquivos") { Patterns = new[] { "*" } }
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
                    MostrarErro("");
                    await CaixaMensagem.MostrarAsync(this, "Conexão bem-sucedida.", "Testar conexão");
                }
                catch (Exception ex)
                {
                    MostrarErro("Falha na conexão: " + PrimeiraLinha(ex.Message));
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
                            $"A tabela \"{ServicoBancoDados.NomeTabela}\" não existe neste banco.\n\nDeseja criá-la agora?",
                            "Criar tabela");
                        if (!criar)
                        {
                            MostrarErro("Conexão cancelada: a tabela não existe.");
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
                    MostrarErro("Não foi possível conectar: " + PrimeiraLinha(ex.Message));
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
                    MostrarErro("Informe o arquivo do banco.");
                    return null;
                }
                return new ConexaoBanco { Tipo = _tipo, Banco = arquivo };
            }

            var host = _txtHost?.Text?.Trim();
            var banco = _txtBanco?.Text?.Trim();
            var usuario = _txtUsuario?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(banco) || string.IsNullOrWhiteSpace(usuario))
            {
                MostrarErro("Preencha host, banco de dados e usuário.");
                return null;
            }

            if (!int.TryParse(_txtPorta?.Text?.Trim(), out var porta) || porta <= 0)
            {
                MostrarErro("Porta inválida.");
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

        private void MostrarErro(string msg) => LblErro.Text = msg;

        private static string PrimeiraLinha(string texto)
        {
            var quebra = texto.IndexOf('\n');
            return quebra < 0 ? texto : texto[..quebra].TrimEnd('\r');
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.Source is not TextBox)
                BeginMoveDrag(e);
        }

        private void Cancelar_Click(object? sender, RoutedEventArgs e) => Close(false);
    }
}
