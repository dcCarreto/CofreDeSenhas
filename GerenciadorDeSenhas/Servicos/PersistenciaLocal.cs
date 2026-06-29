using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Servicos
{
    public class PersistenciaLocal : IPersistenciaLocal
    {
        private readonly IServicoCriptografia _criptografia;
        private readonly string _pastaApp;
        private readonly string _caminhoSenhas;
        private readonly string _pastaBackup;

        public PersistenciaLocal(IServicoCriptografia criptografia, string? pastaApp = null)
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

            int tentativas = 3;
            while (tentativas > 0)
            {
                try
                {
                    await File.WriteAllTextAsync(_caminhoSenhas, criptografado);
                    break;
                }
                catch (IOException) when (tentativas > 1)
                {
                    tentativas--;
                    await Task.Delay(100);
                }
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
    }
}
