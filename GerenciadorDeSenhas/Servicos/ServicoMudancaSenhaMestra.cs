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
            var anexos = new ServicoAnexos(cryptoAntigo, _pastaApp);

            var decifrados = senhas.Select(s => new CamposDecifrados(
                cryptoAntigo.Descriptografar(s.SenhaHash),
                string.IsNullOrEmpty(s.TotpSegredo) ? null : cryptoAntigo.Descriptografar(s.TotpSegredo),
                DecifrarHistorico(s, cryptoAntigo),
                DecifrarCamposExtras(s, cryptoAntigo),
                DecifrarCodigosRecuperacao(s, cryptoAntigo))).ToList();

            var idsAnexos = senhas.SelectMany(s => s.Anexos.Select(a => a.Id)).ToList();
            var anexosCifradosAntigos = await anexos.LerTodosBrutosAsync(idsAnexos);
            var anexosPlanos = anexosCifradosAntigos.ToDictionary(kv => kv.Key, kv => cryptoAntigo.DescriptografarBytes(kv.Value));

            var authPath = Path.Combine(_pastaApp, "auth.dat");
            var vaultPath = Path.Combine(_pastaApp, "senhas.json.enc");
            var authBak = authPath + ".bak";
            var vaultBak = vaultPath + ".bak";

            if (File.Exists(authPath)) File.Copy(authPath, authBak, overwrite: true);
            if (File.Exists(vaultPath)) File.Copy(vaultPath, vaultBak, overwrite: true);

            var anexosEscritos = new List<Guid>();
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
                    alvo.CamposExtras = origem.CamposExtras
                        .ToDictionary(c => c.Chave, c => cryptoNovo.Criptografar(c.Valor));
                    alvo.CodigosRecuperacao = origem.CodigosRecuperacao
                        .Select(c => new CodigoRecuperacao { Id = c.Id, Codigo = cryptoNovo.Criptografar(c.Plano), Usado = c.Usado })
                        .ToList();
                }
                await persistNovo.SalvarSenhasAsync(senhas, chaveNova);

                foreach (var (id, plano) in anexosPlanos)
                {
                    await anexos.EscreverBrutoAsync(id, cryptoNovo.CriptografarBytes(plano));
                    anexosEscritos.Add(id);
                }

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

                foreach (var id in anexosEscritos)
                {
                    try { await anexos.EscreverBrutoAsync(id, anexosCifradosAntigos[id]); } catch { }
                }

                throw;
            }
            finally
            {
                try { if (File.Exists(authBak)) File.Delete(authBak); } catch { }
                try { if (File.Exists(vaultBak)) File.Delete(vaultBak); } catch { }
            }
        }

        public void RestaurarBackupOrfaoSeNecessario()
        {
            var authPath = Path.Combine(_pastaApp, "auth.dat");
            var vaultPath = Path.Combine(_pastaApp, "senhas.json.enc");
            var authBak = authPath + ".bak";
            var vaultBak = vaultPath + ".bak";

            if (!File.Exists(authBak) && !File.Exists(vaultBak))
                return;

            try { if (File.Exists(authBak)) File.Copy(authBak, authPath, overwrite: true); } catch { }
            try { if (File.Exists(vaultBak)) File.Copy(vaultBak, vaultPath, overwrite: true); } catch { }
            try { if (File.Exists(authBak)) File.Delete(authBak); } catch { }
            try { if (File.Exists(vaultBak)) File.Delete(vaultBak); } catch { }
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

        private static List<(string Chave, string Valor)> DecifrarCamposExtras(Senha senha, ServicoCriptografia crypto)
        {
            var campos = new List<(string, string)>();
            foreach (var (chave, valor) in senha.CamposExtras)
            {
                try { campos.Add((chave, crypto.Descriptografar(valor))); }
                catch { }
            }
            return campos;
        }

        private static List<(Guid Id, string Plano, bool Usado)> DecifrarCodigosRecuperacao(Senha senha, ServicoCriptografia crypto)
        {
            var codigos = new List<(Guid, string, bool)>();
            foreach (var codigo in senha.CodigosRecuperacao)
            {
                try { codigos.Add((codigo.Id, crypto.Descriptografar(codigo.Codigo), codigo.Usado)); }
                catch { }
            }
            return codigos;
        }

        private sealed record CamposDecifrados(
            string Senha,
            string? Totp,
            List<(string Plano, DateTime Data)> Historico,
            List<(string Chave, string Valor)> CamposExtras,
            List<(Guid Id, string Plano, bool Usado)> CodigosRecuperacao);
    }
}
