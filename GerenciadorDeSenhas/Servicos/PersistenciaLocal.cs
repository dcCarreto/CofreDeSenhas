using System;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Servicos
{
    public class PersistenciaLocal : IPersistenciaLocal
    {
        public const int QuantidadeMaximaBackupsPadrao = 10;

        private const int TentativasEscrita = 3;
        private static readonly TimeSpan EsperaEntreTentativas = TimeSpan.FromMilliseconds(100);

        private static readonly JsonSerializerOptions OpcoesJson = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly IServicoCriptografia _criptografia;
        private readonly string _pastaApp;
        private readonly string _caminhoSenhas;
        private readonly string _pastaBackup;

        public PersistenciaLocal(IServicoCriptografia criptografia, string? pastaApp = null)
        {
            _criptografia = criptografia ?? throw new ArgumentNullException(nameof(criptografia));

            _pastaApp = pastaApp ?? AmbienteCofre.PastaDados;

            _caminhoSenhas = Path.Combine(_pastaApp, "senhas.json.enc");
            _pastaBackup = Path.Combine(_pastaApp, "backups");

            CriarDiretorios();
        }

        private void CriarDiretorios()
        {
            if (!Directory.Exists(_pastaApp))
                Directory.CreateDirectory(_pastaApp);

            if (!Directory.Exists(_pastaBackup))
                Directory.CreateDirectory(_pastaBackup);
        }

        public async Task SalvarSenhasAsync(List<Senha> senhas, byte[] chave)
        {
            if (senhas == null)
                throw new ArgumentNullException(nameof(senhas));

            if (chave == null)
                throw new ArgumentNullException(nameof(chave));

            try
            {
                var json = JsonSerializer.Serialize(senhas, OpcoesJson);

                var criptografado = _criptografia.Criptografar(json);

                int tentativas = TentativasEscrita;
                while (tentativas > 0)
                {
                    try
                    {
                        await EscritaAtomica.EscreverTextoAsync(_caminhoSenhas, criptografado);
                        break;
                    }
                    catch (IOException) when (tentativas > 1)
                    {
                        tentativas--;
                        await Task.Delay(EsperaEntreTentativas);
                    }
                }
            }
            catch (CryptographicException ex)
            {
                throw new ErroLocalizavel("Vault.Error.WrongKeyOrCorrupt", ex);
            }
            catch (JsonException ex)
            {
                throw new ErroLocalizavel("Vault.Error.CorruptData", ex);
            }
            catch (Exception ex)
            {
                throw new ErroLocalizavel("Vault.Error.IOFailure", ex);
            }
        }

        public async Task<List<Senha>> CarregarSenhasAsync(byte[] chave)
        {
            if (chave == null)
                throw new ArgumentNullException(nameof(chave));

            if (!File.Exists(_caminhoSenhas))
                return new List<Senha>();

            try
            {
                var criptografado = await File.ReadAllTextAsync(_caminhoSenhas);

                var json = _criptografia.Descriptografar(criptografado);

                var senhas = JsonSerializer.Deserialize<List<Senha>>(json) ?? new List<Senha>();

                return senhas;
            }
            catch (CryptographicException ex)
            {
                throw new ErroLocalizavel("Vault.Error.WrongKeyOrCorrupt", ex);
            }
            catch (JsonException ex)
            {
                throw new ErroLocalizavel("Vault.Error.CorruptData", ex);
            }
            catch (Exception ex)
            {
                throw new ErroLocalizavel("Vault.Error.IOFailure", ex);
            }
        }

        public async Task BackupAutomaticoAsync(List<Senha> senhas, byte[] chave, int quantidadeMaxima = QuantidadeMaximaBackupsPadrao)
        {
            if (senhas == null)
                throw new ArgumentNullException(nameof(senhas));

            if (chave == null)
                throw new ArgumentNullException(nameof(chave));

            try
            {
                var json = JsonSerializer.Serialize(senhas, OpcoesJson);

                var criptografado = _criptografia.Criptografar(json);

                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff");
                var nomeBackup = $"senhas_backup_{timestamp}.json.enc";
                var caminhoBackup = Path.Combine(_pastaBackup, nomeBackup);

                await EscritaAtomica.EscreverTextoAsync(caminhoBackup, criptografado);

                LimparBackupsAntigos(quantidadeMaxima);
            }
            catch (Exception ex)
            {
                throw new ErroLocalizavel("Vault.Error.BackupFailed", ex);
            }
        }

        public List<InfoBackup> ListarBackups()
        {
            if (!Directory.Exists(_pastaBackup))
                return new List<InfoBackup>();

            return Directory.GetFiles(_pastaBackup, "senhas_backup_*.json.enc")
                .Select(f => new InfoBackup(f, File.GetLastWriteTimeUtc(f)))
                .OrderByDescending(b => b.DataUtc)
                .ToList();
        }

        public async Task<List<Senha>> CarregarBackupAsync(string caminhoArquivo)
        {
            if (string.IsNullOrWhiteSpace(caminhoArquivo))
                throw new ArgumentException("Caminho do backup não pode ser vazio", nameof(caminhoArquivo));

            try
            {
                var criptografado = await File.ReadAllTextAsync(caminhoArquivo);
                var json = _criptografia.Descriptografar(criptografado);
                return JsonSerializer.Deserialize<List<Senha>>(json) ?? new List<Senha>();
            }
            catch (CryptographicException ex)
            {
                throw new ErroLocalizavel("Vault.Error.WrongKeyOrCorrupt", ex);
            }
            catch (JsonException ex)
            {
                throw new ErroLocalizavel("Vault.Error.CorruptData", ex);
            }
            catch (Exception ex)
            {
                throw new ErroLocalizavel("Vault.Error.IOFailure", ex);
            }
        }

        private void LimparBackupsAntigos(int quantidadeMaxima)
        {
            try
            {
                var arquivos = Directory.GetFiles(_pastaBackup, "senhas_backup_*.json.enc")
                    .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                    .ToList();

                if (arquivos.Count > quantidadeMaxima)
                {
                    for (int i = quantidadeMaxima; i < arquivos.Count; i++)
                    {
                        File.Delete(arquivos[i]);
                    }
                }
            }
            catch
            {
            }
        }

        public Task ApagarTudoAsync()
        {
            if (File.Exists(_caminhoSenhas))
                File.Delete(_caminhoSenhas);

            if (Directory.Exists(_pastaBackup))
                Directory.Delete(_pastaBackup, recursive: true);

            return Task.CompletedTask;
        }

        public bool ValidarIntegridade()
        {
            try
            {
                if (!File.Exists(_caminhoSenhas))
                    return false;

                if (!Directory.Exists(_pastaBackup))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
