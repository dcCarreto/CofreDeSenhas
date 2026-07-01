using System;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Servicos
{
    public readonly record struct EstadoArquivoCofre(
        bool Existe,
        long Tamanho,
        DateTime UltimaEscritaUtc,
        string Hash);

    public class ConflitoGravacaoCofreException : IOException
    {
        public ConflitoGravacaoCofreException()
            : base("O arquivo do cofre foi alterado por outro processo.")
        {
        }
    }

    public class IntegridadeCofreException : IOException
    {
        public IntegridadeCofreException(string mensagem, Exception? innerException = null)
            : base(mensagem, innerException)
        {
        }
    }

    public class PersistenciaLocal
    {
        private readonly ServicoCriptografia _criptografia;
        private readonly string _pastaApp;
        private readonly string _caminhoSenhas;
        private readonly string _pastaBackup;

        public PersistenciaLocal(ServicoCriptografia criptografia, string? pastaApp = null)
        {
            _criptografia = criptografia ?? throw new ArgumentNullException(nameof(criptografia));

            _pastaApp = pastaApp ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GerenciadorSenhas");

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
            await SalvarSenhasComSegurancaAsync(senhas, chave, null);
        }

        public async Task<EstadoArquivoCofre> SalvarSenhasComSegurancaAsync(
            List<Senha> senhas,
            byte[] chave,
            EstadoArquivoCofre? estadoEsperado)
        {
            if (senhas == null)
                throw new ArgumentNullException(nameof(senhas));

            if (chave == null)
                throw new ArgumentNullException(nameof(chave));

            var opcoes = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(senhas, opcoes);

            var criptografado = _criptografia.Criptografar(json);

            return await SalvarCriptografadoAsync(criptografado, estadoEsperado);
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
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erro ao carregar senhas: {ex.Message}", ex);
            }
        }

        public async Task BackupAutomaticoAsync(List<Senha> senhas, byte[] chave)
        {
            if (senhas == null)
                throw new ArgumentNullException(nameof(senhas));

            if (chave == null)
                throw new ArgumentNullException(nameof(chave));

            try
            {
                var opcoes = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var json = JsonSerializer.Serialize(senhas, opcoes);

                var criptografado = _criptografia.Criptografar(json);

                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff");
                var nomeBackup = $"senhas_backup_{timestamp}.json.enc";
                var caminhoBackup = Path.Combine(_pastaBackup, nomeBackup);

                await File.WriteAllTextAsync(caminhoBackup, criptografado);

                LimparBackupsAntigos();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erro ao fazer backup: {ex.Message}", ex);
            }
        }

        private void LimparBackupsAntigos()
        {
            try
            {
                var arquivos = Directory.GetFiles(_pastaBackup, "senhas_backup_*.json.enc")
                    .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                    .ToList();

                if (arquivos.Count > 10)
                {
                    for (int i = 10; i < arquivos.Count; i++)
                    {
                        File.Delete(arquivos[i]);
                    }
                }
            }
            catch
            {
            }
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

        public EstadoArquivoCofre ObterEstadoArquivo()
        {
            if (!File.Exists(_caminhoSenhas))
                return new EstadoArquivoCofre(false, 0, DateTime.MinValue, string.Empty);

            using var stream = new FileStream(_caminhoSenhas, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var hash = Convert.ToHexString(SHA256.HashData(stream));
            var info = new FileInfo(_caminhoSenhas);
            return new EstadoArquivoCofre(true, info.Length, info.LastWriteTimeUtc, hash);
        }

        private async Task<EstadoArquivoCofre> SalvarCriptografadoAsync(
            string criptografado,
            EstadoArquivoCofre? estadoEsperado)
        {
            if (estadoEsperado.HasValue)
                ValidarEstadoEsperado(estadoEsperado.Value);

            var temp = Path.Combine(_pastaApp, $"senhas.{Guid.NewGuid():N}.tmp");
            var backup = File.Exists(_caminhoSenhas) ? CaminhoBackup() : null;

            try
            {
                await File.WriteAllTextAsync(temp, criptografado);
                ValidarArquivo(temp);

                if (File.Exists(_caminhoSenhas))
                {
                    File.Copy(_caminhoSenhas, backup!, overwrite: false);
                    if (estadoEsperado.HasValue)
                        ValidarEstadoEsperado(estadoEsperado.Value);
                    File.Replace(temp, _caminhoSenhas, null);
                }
                else
                {
                    File.Move(temp, _caminhoSenhas);
                }

                try
                {
                    ValidarArquivo(_caminhoSenhas);
                }
                catch (Exception ex)
                {
                    RestaurarBackup(backup);
                    throw new IntegridadeCofreException("A gravação do cofre falhou na validação de integridade.", ex);
                }

                LimparBackupsAntigos();
                return ObterEstadoArquivo();
            }
            finally
            {
                try
                {
                    if (File.Exists(temp))
                        File.Delete(temp);
                }
                catch
                {
                }
            }
        }

        private void ValidarEstadoEsperado(EstadoArquivoCofre estadoEsperado)
        {
            var atual = ObterEstadoArquivo();
            if (!atual.Equals(estadoEsperado))
                throw new ConflitoGravacaoCofreException();
        }

        private string CaminhoBackup()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            return Path.Combine(_pastaBackup, $"senhas_backup_{timestamp}_{Guid.NewGuid():N}.json.enc");
        }

        private void ValidarArquivo(string caminho)
        {
            var texto = File.ReadAllText(caminho);
            var json = _criptografia.Descriptografar(texto);
            _ = JsonSerializer.Deserialize<List<Senha>>(json)
                ?? throw new IntegridadeCofreException("O cofre gravado não contém uma lista válida.");
        }

        private void RestaurarBackup(string? backup)
        {
            if (string.IsNullOrWhiteSpace(backup) || !File.Exists(backup))
                return;

            try
            {
                File.Copy(backup, _caminhoSenhas, overwrite: true);
            }
            catch
            {
            }
        }
    }
}
