using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Modelos;
using Konscious.Security.Cryptography;

namespace GerenciadorDeSenhas.Servicos
{
    public class ServicoSincronizacao
    {
        public const string NomeArquivo = "sincronizacao.dat";
        public const int Iteracoes = 600_000;
        public const string KdfArgon2id = "Argon2id";

        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int TempoCustoAtual = 3;
        private const int MemoriaKbAtual = 65536;
        private const int ParalelismoAtual = 1;

        private static readonly JsonSerializerOptions OpcoesJson = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly IServicoCriptografia _criptografia;

        public ServicoSincronizacao(IServicoCriptografia criptografia)
        {
            _criptografia = criptografia ?? throw new ArgumentNullException(nameof(criptografia));
        }

        public void ZerarChave() => _criptografia.ZerarChave();

        public static byte[] GerarSalt() => RandomNumberGenerator.GetBytes(SaltSize);

        public static (string Kdf, int Iteracoes, int MemoriaKb, int Paralelismo) ParametrosPadrao() =>
            (KdfArgon2id, TempoCustoAtual, MemoriaKbAtual, ParalelismoAtual);

        public static byte[] DerivarChave(string senhaMestraPlaintext, byte[] salt, string? kdf, int iteracoes,
            int? memoriaKb = null, int? paralelismo = null)
        {
            if (string.Equals(kdf, KdfArgon2id, StringComparison.OrdinalIgnoreCase))
                return DerivarChaveArgon2id(senhaMestraPlaintext, salt, iteracoes,
                    memoriaKb ?? MemoriaKbAtual, paralelismo ?? ParalelismoAtual);

            var iteracoesPbkdf2 = iteracoes > 0 ? iteracoes : Iteracoes;
            ProtecaoKdf.GarantirPbkdf2(iteracoesPbkdf2);
            return Rfc2898DeriveBytes.Pbkdf2(senhaMestraPlaintext, salt, iteracoesPbkdf2, HashAlgorithmName.SHA256, KeySize);
        }

        private static byte[] DerivarChaveArgon2id(string senha, byte[] salt, int tempoCusto, int memoriaKb, int paralelismo)
        {
            ProtecaoKdf.GarantirArgon2id(tempoCusto, memoriaKb, paralelismo);
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(senha))
            {
                Salt = salt,
                DegreeOfParallelism = paralelismo,
                Iterations = tempoCusto,
                MemorySize = memoriaKb
            };
            return argon2.GetBytes(KeySize);
        }

        public static async Task<(byte[] Salt, string? Kdf, int Iteracoes, int? MemoriaKb, int? Paralelismo)?> LerCabecalhoAsync(string caminhoArquivo)
        {
            if (!File.Exists(caminhoArquivo))
                return null;

            try
            {
                var envelope = JsonSerializer.Deserialize<EnvelopeSincronizacao>(await File.ReadAllTextAsync(caminhoArquivo));
                if (envelope?.Salt == null)
                    return null;

                return (Convert.FromBase64String(envelope.Salt), envelope.Kdf, envelope.Iteracoes,
                    envelope.MemoriaKb, envelope.Paralelismo);
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<SenhaExportada>> LerAsync(string caminhoArquivo)
        {
            if (!File.Exists(caminhoArquivo))
                return new List<SenhaExportada>();

            string conteudo;
            try
            {
                conteudo = await File.ReadAllTextAsync(caminhoArquivo);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Diferente de "chave errada" ou "arquivo corrompido" (que viram lista vazia
                // de propósito, ver testes) — uma falha de leitura de verdade (pasta de nuvem
                // ainda baixando o arquivo, outro dispositivo escrevendo nele nesse instante
                // etc.) não pode virar lista vazia aqui: SincronizarAsync mescla o retorno com
                // o cofre local e regrava o arquivo remoto por cima, então "vazio" nesse caso
                // apagaria qualquer dado que outro dispositivo ainda não tinha terminado de
                // publicar. Propagar deixa o chamador abortar a rodada de sync sem regravar
                // nada.
                throw new ErroLocalizavel("Sync.Error.ReadFailed", ex);
            }

            try
            {
                var envelope = JsonSerializer.Deserialize<EnvelopeSincronizacao>(conteudo);
                if (envelope?.Dados == null)
                    return new List<SenhaExportada>();

                var bytesPlain = _criptografia.DescriptografarBytes(Convert.FromBase64String(envelope.Dados));
                var itens = JsonSerializer.Deserialize<List<SenhaExportada>>(bytesPlain) ?? new List<SenhaExportada>();
                foreach (var item in itens)
                    Sanitizar(item);
                return itens;
            }
            catch
            {
                return new List<SenhaExportada>();
            }
        }

        // System.Text.Json sobrescreve uma propriedade com null se a chave estiver
        // presente no JSON com valor null, mesmo com um inicializador "= new()" no
        // tipo — um sincronizacao.dat escrito por outra versão do app, ou corrompido,
        // pode chegar assim. Sem isto, uma lista nula estoura mais adiante em
        // MesclaSincronizacao (new List<T>(vencedora) com vencedora nula) ou em
        // ServicoSenha.AplicarSincronizadoAsync (.Where sobre lista nula) — e como o
        // arquivo remoto continua do jeito que está, todo sync futuro falharia do
        // mesmo jeito.
        private static void Sanitizar(SenhaExportada item)
        {
            item.NomeServico ??= "";
            item.Usuario ??= "";
            item.Senha ??= "";
            item.Etiquetas ??= new();
            item.CamposExtras ??= new();
            item.Historico ??= new();
            item.CodigosRecuperacao ??= new();
            item.Anexos ??= new();
        }

        public async Task EscreverAsync(string caminhoArquivo, byte[] salt, string? kdf, int iteracoes,
            int? memoriaKb, int? paralelismo, List<SenhaExportada> itens)
        {
            var json = JsonSerializer.Serialize(itens, OpcoesJson);
            var bytesCifrados = _criptografia.CriptografarBytes(Encoding.UTF8.GetBytes(json));

            var envelope = new EnvelopeSincronizacao
            {
                Versao = 1,
                Salt = Convert.ToBase64String(salt),
                Kdf = kdf,
                Iteracoes = iteracoes,
                MemoriaKb = memoriaKb,
                Paralelismo = paralelismo,
                Dados = Convert.ToBase64String(bytesCifrados)
            };

            try
            {
                var dir = Path.GetDirectoryName(caminhoArquivo);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                await EscritaAtomica.EscreverTextoAsync(caminhoArquivo, JsonSerializer.Serialize(envelope));
            }
            catch (Exception ex)
            {
                throw new ErroLocalizavel("Sync.Error.WriteFailed", ex);
            }
        }

        public static List<SenhaExportada> MesclarListas(IReadOnlyList<SenhaExportada> locais, IReadOnlyList<SenhaExportada> remotos) =>
            MesclaSincronizacao.MesclarSenhasExportadas(locais, remotos);

        // Subpasta ao lado de NomeArquivo (sincronizacao.dat), um .enc por anexo —
        // mesmo layout de App/anexos/ localmente, mas cifrado com a chave de
        // sincronização (a mesma que já cifra sincronizacao.dat), não a chave do
        // cofre local: os dois esquemas de chave ficam desacoplados de propósito, já
        // que a chave de sync pode viajar por dispositivos que nunca teve o cofre
        // local aberto ainda (ex.: restaurar um dispositivo novo a partir do banco).
        public const string PastaAnexosNome = "anexos-sincronizados";

        public static string CaminhoAnexo(string pastaCompartilhada, Guid anexoId) =>
            Path.Combine(pastaCompartilhada, PastaAnexosNome, anexoId.ToString("N") + ".enc");

        public async Task PublicarAnexoAsync(string pastaCompartilhada, Guid anexoId, byte[] conteudoPlano)
        {
            var caminho = CaminhoAnexo(pastaCompartilhada, anexoId);
            var dir = Path.GetDirectoryName(caminho);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var cifrado = _criptografia.CriptografarBytes(conteudoPlano);
            await EscritaAtomica.EscreverBytesAsync(caminho, cifrado);
        }

        // Null quando o anexo ainda não foi publicado por nenhum dispositivo — quem
        // chama decide o que fazer (a fase B só publica; baixar sob demanda é da UI,
        // fase D).
        public async Task<byte[]?> BaixarAnexoAsync(string pastaCompartilhada, Guid anexoId)
        {
            var caminho = CaminhoAnexo(pastaCompartilhada, anexoId);
            if (!File.Exists(caminho))
                return null;

            var cifrado = await File.ReadAllBytesAsync(caminho);
            return _criptografia.DescriptografarBytes(cifrado);
        }

        public static bool AnexoPublicado(string pastaCompartilhada, Guid anexoId) =>
            File.Exists(CaminhoAnexo(pastaCompartilhada, anexoId));

        // Best-effort: um anexo que nunca chegou a ser publicado (outro dispositivo
        // excluiu antes de sincronizar de novo) não é erro, e uma falha aqui (pasta
        // momentaneamente inacessível) só deixa um blob cifrado órfão pra trás — sem
        // vazamento nenhum, já que continua cifrado — não motivo pra derrubar o
        // ciclo de sincronização inteiro.
        public static void ApagarAnexoPublicado(string pastaCompartilhada, Guid anexoId)
        {
            try { File.Delete(CaminhoAnexo(pastaCompartilhada, anexoId)); }
            catch { }
        }

        private sealed class EnvelopeSincronizacao
        {
            public int Versao { get; set; }
            public string? Salt { get; set; }
            public string? Kdf { get; set; }
            public int Iteracoes { get; set; }
            public int? MemoriaKb { get; set; }
            public int? Paralelismo { get; set; }
            public string? Dados { get; set; }
        }
    }
}
