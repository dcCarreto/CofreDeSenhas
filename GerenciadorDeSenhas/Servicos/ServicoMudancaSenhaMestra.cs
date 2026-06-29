using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GerenciadorDeSenhas.Servicos
{
    public class ServicoMudancaSenhaMestra
    {
        private readonly string _pastaApp;

        public ServicoMudancaSenhaMestra(string? pastaApp = null)
        {
            _pastaApp = pastaApp ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GerenciadorSenhas");
        }

        public async Task AlterarAsync(string senhaAtual, string novaSenha)
        {
            if (string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length < 8)
                throw new ArgumentException("A nova senha mestra deve ter pelo menos 8 caracteres.");

            var auth = new AutenticacaoMestra(_pastaApp);
            var chaveAntiga = auth.Autenticar(senhaAtual)
                ?? throw new InvalidOperationException("Senha mestra atual incorreta.");

            var cryptoAntigo = new ServicoCriptografia(chaveAntiga);
            var persistAntigo = new PersistenciaLocal(cryptoAntigo, _pastaApp);
            var senhas = await persistAntigo.CarregarSenhasAsync(chaveAntiga);

            var planos = senhas.Select(s => cryptoAntigo.Descriptografar(s.SenhaHash)).ToList();

            var authPath = Path.Combine(_pastaApp, "auth.dat");
            var vaultPath = Path.Combine(_pastaApp, "senhas.json.enc");
            var authBak = authPath + ".bak";
            var vaultBak = vaultPath + ".bak";

            if (File.Exists(authPath)) File.Copy(authPath, authBak, overwrite: true);
            if (File.Exists(vaultPath)) File.Copy(vaultPath, vaultBak, overwrite: true);

            try
            {
                var chaveNova = auth.CriarSenhaMestra(novaSenha);

                var cryptoNovo = new ServicoCriptografia(chaveNova);
                var persistNovo = new PersistenciaLocal(cryptoNovo, _pastaApp);
                for (int i = 0; i < senhas.Count; i++)
                    senhas[i].SenhaHash = cryptoNovo.Criptografar(planos[i]);
                await persistNovo.SalvarSenhasAsync(senhas, chaveNova);
            }
            catch
            {
                try
                {
                    if (File.Exists(authBak)) File.Copy(authBak, authPath, overwrite: true);
                    if (File.Exists(vaultBak)) File.Copy(vaultBak, vaultPath, overwrite: true);
                }
                catch { }
                throw;
            }
            finally
            {
                try { if (File.Exists(authBak)) File.Delete(authBak); } catch { }
                try { if (File.Exists(vaultBak)) File.Delete(vaultBak); } catch { }
            }
        }
    }
}
