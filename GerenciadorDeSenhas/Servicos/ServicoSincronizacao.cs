using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Servicos
{
    public class ServicoSincronizacao
    {
        public const string NomeArquivo = "sincronizacao.dat";
        public const int Iteracoes = 600_000;

        private const int SaltSize = 16;
        private const int KeySize = 32;

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

        public static byte[] DerivarChave(string senhaMestraPlaintext, byte[] salt, int iteracoes = Iteracoes) =>
            Rfc2898DeriveBytes.Pbkdf2(senhaMestraPlaintext, salt, iteracoes, HashAlgorithmName.SHA256, KeySize);

        public static async Task<(byte[] Salt, int Iteracoes)?> LerCabecalhoAsync(string caminhoArquivo)
        {
            if (!File.Exists(caminhoArquivo))
                return null;

            try
            {
                var envelope = JsonSerializer.Deserialize<EnvelopeSincronizacao>(await File.ReadAllTextAsync(caminhoArquivo));
                if (envelope?.Salt == null)
                    return null;

                return (Convert.FromBase64String(envelope.Salt), envelope.Iteracoes);
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

            try
            {
                var envelope = JsonSerializer.Deserialize<EnvelopeSincronizacao>(await File.ReadAllTextAsync(caminhoArquivo));
                if (envelope?.Dados == null)
                    return new List<SenhaExportada>();

                var bytesPlain = _criptografia.DescriptografarBytes(Convert.FromBase64String(envelope.Dados));
                return JsonSerializer.Deserialize<List<SenhaExportada>>(bytesPlain) ?? new List<SenhaExportada>();
            }
            catch
            {
                return new List<SenhaExportada>();
            }
        }

        public async Task EscreverAsync(string caminhoArquivo, byte[] salt, int iteracoes, List<SenhaExportada> itens)
        {
            var json = JsonSerializer.Serialize(itens, OpcoesJson);
            var bytesCifrados = _criptografia.CriptografarBytes(Encoding.UTF8.GetBytes(json));

            var envelope = new EnvelopeSincronizacao
            {
                Versao = 1,
                Salt = Convert.ToBase64String(salt),
                Iteracoes = iteracoes,
                Dados = Convert.ToBase64String(bytesCifrados)
            };

            var dir = Path.GetDirectoryName(caminhoArquivo);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(caminhoArquivo, JsonSerializer.Serialize(envelope));
        }

        public static List<SenhaExportada> MesclarListas(IReadOnlyList<SenhaExportada> locais, IReadOnlyList<SenhaExportada> remotos) =>
            MesclaSincronizacao.Mesclar(locais, remotos, item => item.Id, item => item.DataAtualizacao);

        private sealed class EnvelopeSincronizacao
        {
            public int Versao { get; set; }
            public string? Salt { get; set; }
            public int Iteracoes { get; set; }
            public string? Dados { get; set; }
        }
    }
}
