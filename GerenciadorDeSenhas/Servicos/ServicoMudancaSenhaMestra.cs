using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Modelos;

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

        public async Task<byte[]?> MigrarKdfSeNecessarioAsync(string senhaAtual)
        {
            if (!new AutenticacaoMestra(_pastaApp).KdfDesatualizado())
                return null;

            return await AlterarAsync(senhaAtual, senhaAtual);
        }

        public async Task<byte[]> AlterarAsync(string senhaAtual, string novaSenha)
        {
            if (string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length < AutenticacaoMestra.TamanhoMinimoSenha)
                throw new ErroLocalizavel("Master.Error.NewPasswordTooShort", AutenticacaoMestra.TamanhoMinimoSenha);

            var auth = new AutenticacaoMestra(_pastaApp);
            var chaveAntiga = auth.Autenticar(senhaAtual)
                ?? throw new ErroLocalizavel("Master.Error.CurrentPasswordWrong");

            var cryptoAntigo = new ServicoCriptografia(chaveAntiga);
            var persistAntigo = new PersistenciaLocal(cryptoAntigo, _pastaApp);
            var senhas = await persistAntigo.CarregarSenhasAsync(chaveAntiga);

            var decifrados = senhas.Select(s => new CamposDecifrados(
                cryptoAntigo.Descriptografar(s.SenhaHash),
                string.IsNullOrEmpty(s.TotpSegredo) ? null : cryptoAntigo.Descriptografar(s.TotpSegredo),
                DecifrarHistorico(s, cryptoAntigo))).ToList();

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
                {
                    var alvo = senhas[i];
                    var origem = decifrados[i];
                    alvo.SenhaHash = cryptoNovo.Criptografar(origem.Senha);
                    alvo.TotpSegredo = origem.Totp == null ? null : cryptoNovo.Criptografar(origem.Totp);
                    alvo.Historico = origem.Historico
                        .Select(h => new HistoricoSenha
                        {
                            SenhaHash = cryptoNovo.Criptografar(h.Plano),
                            DataAlteracao = h.Data
                        })
                        .ToList();
                }
                await persistNovo.SalvarSenhasAsync(senhas, chaveNova);
                return chaveNova;
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

        private static List<(string Plano, DateTime Data)> DecifrarHistorico(Senha senha, ServicoCriptografia crypto)
        {
            var historico = new List<(string, DateTime)>();
            foreach (var item in senha.Historico)
            {
                try { historico.Add((crypto.Descriptografar(item.SenhaHash), item.DataAlteracao)); }
                catch { }
            }
            return historico;
        }

        private sealed record CamposDecifrados(string Senha, string? Totp, List<(string Plano, DateTime Data)> Historico);
    }
}
