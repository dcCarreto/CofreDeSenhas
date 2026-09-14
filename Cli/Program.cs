using GerenciadorDeSenhas.Excecoes;

namespace CofreDeSenhas.Cli
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }

            if (args.Length == 0 || args[0] is "-h" or "--help" or "ajuda" or "help")
            {
                Ajuda.Imprimir();
                return args.Length == 0 ? 1 : 0;
            }

            var comando = args[0];
            var resto = new Argumentos(args[1..]);

            try
            {
                return comando switch
                {
                    "gerar" => Comandos.Gerar(resto),
                    "listar" => await Comandos.ListarAsync(resto),
                    "buscar" => await Comandos.BuscarAsync(resto),
                    "mostrar" => await Comandos.MostrarAsync(resto),
                    "copiar" => await Comandos.CopiarAsync(resto),
                    _ => ComandoDesconhecido(comando),
                };
            }
            catch (ErroLocalizavel ex)
            {
                Console.Error.WriteLine("Erro: " + MensagemDeErro(ex));
                return 1;
            }
            catch (SemClipboardException ex)
            {
                Console.Error.WriteLine("Erro: " + ex.Message);
                return 4;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Erro inesperado: " + ex.Message);
                return 1;
            }
        }

        private static int ComandoDesconhecido(string comando)
        {
            Console.Error.WriteLine($"Comando desconhecido: {comando}");
            Console.Error.WriteLine("Use \"cofre --help\" para ver os comandos.");
            return 1;
        }

        // ErroLocalizavel carrega uma chave de i18n que só o app resolve. Aqui
        // traduzimos as poucas que o CLI pode disparar; o resto sai com a chave.
        private static string MensagemDeErro(ErroLocalizavel ex) => ex.Chave switch
        {
            "Generator.Error.LengthRange" => "o comprimento deve ficar entre 4 e 1000.",
            "Generator.Error.NoCharacterType" => "nenhum tipo de caractere habilitado (não use todos os --sem-* juntos).",
            "Generator.Error.PassphraseWordsRange" => "a frase-senha deve ter entre 3 e 12 palavras.",
            "Generator.Error.SeparatorTooLong" => "o separador da frase-senha tem no máximo 3 caracteres.",
            "Generator.Error.QuantityRange" => "a quantidade deve ficar entre 1 e 50.",
            "Vault.Error.WrongKeyOrCorrupt" => "não foi possível decifrar o cofre (senha errada ou arquivo corrompido).",
            "Vault.Error.CorruptData" => "os dados do cofre parecem corrompidos.",
            "Vault.Error.IOFailure" => "falha ao ler o arquivo do cofre.",
            _ => ex.Chave,
        };
    }
}
