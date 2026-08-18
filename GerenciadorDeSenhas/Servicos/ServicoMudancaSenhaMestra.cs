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
        private readonly List<string> _avisos = new();

        public ServicoMudancaSenhaMestra(string? pastaApp = null)
        {
            _pastaApp = pastaApp ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GerenciadorSenhas");
        }

        // Itens que não puderam ser decifrados (dado corrompido) e por isso foram
        // descartados durante a última troca de senha mestra — vazio quando nada
        // precisou ser descartado. Quem chama decide se/como avisar o usuário.
        public IReadOnlyList<string> UltimosAvisos => _avisos;

        public async Task<byte[]?> MigrarKdfSeNecessarioAsync(string senhaAtual)
        {
            if (!new AutenticacaoMestra(_pastaApp).KdfDesatualizado())
                return null;

            return await AlterarAsync(senhaAtual, senhaAtual);
        }

        public async Task<byte[]> AlterarAsync(string senhaAtual, string novaSenha)
        {
            _avisos.Clear();

            if (string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length < AutenticacaoMestra.TamanhoMinimoSenha)
                throw new ErroLocalizavel("Master.Error.NewPasswordTooShort", AutenticacaoMestra.TamanhoMinimoSenha);

            var auth = new AutenticacaoMestra(_pastaApp);
            var chaveAntiga = auth.Autenticar(senhaAtual)
                ?? throw new ErroLocalizavel("Master.Error.CurrentPasswordWrong");

            var cryptoAntigo = new ServicoCriptografia(chaveAntiga);
            var persistAntigo = new PersistenciaLocal(cryptoAntigo, _pastaApp);
            var senhas = await persistAntigo.CarregarSenhasAsync(chaveAntiga);
            var anexos = new ServicoAnexos(cryptoAntigo, _pastaApp);

            var decifrados = new List<CamposDecifrados>(senhas.Count);
            foreach (var s in senhas)
            {
                string senhaPlano;
                string? totpPlano;
                try
                {
                    senhaPlano = cryptoAntigo.Descriptografar(s.SenhaHash);
                    totpPlano = string.IsNullOrEmpty(s.TotpSegredo) ? null : cryptoAntigo.Descriptografar(s.TotpSegredo);
                }
                catch (Exception ex)
                {
                    throw new ErroLocalizavel("Master.Error.CorruptedEntry", ex, s.NomeServico);
                }

                decifrados.Add(new CamposDecifrados(
                    senhaPlano,
                    totpPlano,
                    DecifrarHistorico(s, cryptoAntigo),
                    DecifrarCamposExtras(s, cryptoAntigo),
                    DecifrarCodigosRecuperacao(s, cryptoAntigo)));
            }

            var infoAnexo = senhas
                .SelectMany(s => s.Anexos.Select(a => (a.Id, a.NomeArquivo, Senha: s)))
                .ToDictionary(x => x.Id, x => x);
            var anexosCifradosAntigos = await anexos.LerTodosBrutosAsync(infoAnexo.Keys);
            var anexosPlanos = new Dictionary<Guid, byte[]>();
            foreach (var (id, cifrado) in anexosCifradosAntigos)
            {
                try
                {
                    anexosPlanos[id] = cryptoAntigo.DescriptografarBytes(cifrado);
                }
                catch
                {
                    var info = infoAnexo[id];
                    _avisos.Add($"Anexo \"{info.NomeArquivo}\" de \"{info.Senha.NomeServico}\" corrompido — descartado.");
                    info.Senha.Anexos.RemoveAll(a => a.Id == id);
                }
            }

            // Chave/subchave HMAC antigas não são mais necessárias a partir daqui —
            // ZerarChave zera o mesmo array que chaveAntiga referencia.
            cryptoAntigo.ZerarChave();

            var authPath = Path.Combine(_pastaApp, "auth.dat");
            var vaultPath = Path.Combine(_pastaApp, "senhas.json.enc");
            var authBak = authPath + ".bak";
            var vaultBak = vaultPath + ".bak";
            var marcadorConcluido = Path.Combine(_pastaApp, "troca_senha.ok");

            if (File.Exists(authPath)) File.Copy(authPath, authBak, overwrite: true);
            if (File.Exists(vaultPath)) File.Copy(vaultPath, vaultBak, overwrite: true);

            // Backup físico de cada anexo antes de regravar, igual ao padrão de
            // auth.dat/vault acima — sem isto, se o processo morrer no meio do laço
            // (não um catch, o processo de verdade sendo encerrado), os anexos já
            // regravados com a chave nova ficam ilegíveis quando auth.dat/vault
            // voltarem pra chave antiga na próxima abertura (RestaurarBackupOrfaoSeNecessario).
            var pastaAnexos = Path.Combine(_pastaApp, "anexos");
            foreach (var id in anexosPlanos.Keys)
            {
                var caminho = Path.Combine(pastaAnexos, id.ToString("N") + ".enc");
                if (File.Exists(caminho)) File.Copy(caminho, caminho + ".bak", overwrite: true);
            }

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

                // Sinaliza que a troca já terminou com sucesso ANTES de começar a
                // limpeza dos .bak abaixo (no finally) — o "return" desta função e a
                // exclusão dos .bak não são atômicos entre si (são operações de disco
                // separadas), então sem este marcador, um processo morto bem nessa
                // janela (depois do sucesso, antes da limpeza) fazia
                // RestaurarBackupOrfaoSeNecessario encontrar os dois .bak presentes e
                // concluir erroneamente "interrompido no meio", revertendo uma troca
                // que já tinha terminado. Com o marcador presente, a próxima
                // inicialização sabe que só falta limpar, não restaurar.
                await File.WriteAllTextAsync(marcadorConcluido, "");

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
                try { if (File.Exists(marcadorConcluido)) File.Delete(marcadorConcluido); } catch { }
                try { if (File.Exists(authBak)) File.Delete(authBak); } catch { }
                try { if (File.Exists(vaultBak)) File.Delete(vaultBak); } catch { }

                foreach (var id in anexosPlanos.Keys)
                {
                    var bak = Path.Combine(pastaAnexos, id.ToString("N") + ".enc.bak");
                    try { if (File.Exists(bak)) File.Delete(bak); } catch { }
                }
            }
        }

        public void RestaurarBackupOrfaoSeNecessario()
        {
            var authPath = Path.Combine(_pastaApp, "auth.dat");
            var vaultPath = Path.Combine(_pastaApp, "senhas.json.enc");
            var authBak = authPath + ".bak";
            var vaultBak = vaultPath + ".bak";
            var marcadorConcluido = Path.Combine(_pastaApp, "troca_senha.ok");

            var authBakExiste = File.Exists(authBak);
            var vaultBakExiste = File.Exists(vaultBak);
            var marcadorExiste = File.Exists(marcadorConcluido);

            if (!authBakExiste && !vaultBakExiste && !marcadorExiste)
                return;

            // O marcador é escrito só depois que a troca inteira já terminou com
            // sucesso, antes da limpeza dos .bak (ver AlterarAsync) — se ele existe, a
            // troca já funcionou e só falta terminar a limpeza; nunca restaurar nesse
            // caso, senão reverteria uma troca que já tinha dado certo. Sem o
            // marcador, os dois .bak presentes juntos são o sinal real de interrupção
            // no meio da troca (auth.dat e o cofre são copiados pra .bak juntos, antes
            // de qualquer escrita nova). Um .bak solitário sem marcador é só lixo de
            // uma exclusão que falhou (ex.: antivírus segurando o arquivo por um
            // instante) numa troca que também já tinha terminado — restaurá-lo sozinho
            // reverteria só uma metade pra chave antiga, deixando auth.dat e o cofre
            // em chaves diferentes.
            var interrompidoNoMeioDaTroca = !marcadorExiste && authBakExiste && vaultBakExiste;

            if (interrompidoNoMeioDaTroca)
            {
                try { File.Copy(authBak, authPath, overwrite: true); } catch { }
                try { File.Copy(vaultBak, vaultPath, overwrite: true); } catch { }
            }
            try { if (File.Exists(marcadorConcluido)) File.Delete(marcadorConcluido); } catch { }
            try { if (File.Exists(authBak)) File.Delete(authBak); } catch { }
            try { if (File.Exists(vaultBak)) File.Delete(vaultBak); } catch { }

            var pastaAnexos = Path.Combine(_pastaApp, "anexos");
            if (!Directory.Exists(pastaAnexos)) return;

            foreach (var bak in Directory.GetFiles(pastaAnexos, "*.enc.bak"))
            {
                var original = bak.Substring(0, bak.Length - ".bak".Length);
                if (interrompidoNoMeioDaTroca)
                {
                    try { File.Copy(bak, original, overwrite: true); } catch { }
                }
                try { File.Delete(bak); } catch { }
            }
        }

        private List<(string Plano, DateTime Data)> DecifrarHistorico(Senha senha, ServicoCriptografia crypto)
        {
            var historico = new List<(string, DateTime)>();
            foreach (var item in senha.Historico)
            {
                try { historico.Add((crypto.Descriptografar(item.SenhaHash), item.DataAlteracao)); }
                catch { _avisos.Add($"Histórico de \"{senha.NomeServico}\" corrompido — descartado."); }
            }
            return historico;
        }

        private List<(string Chave, string Valor)> DecifrarCamposExtras(Senha senha, ServicoCriptografia crypto)
        {
            var campos = new List<(string, string)>();
            foreach (var (chave, valor) in senha.CamposExtras)
            {
                try { campos.Add((chave, crypto.Descriptografar(valor))); }
                catch { _avisos.Add($"Campo \"{chave}\" de \"{senha.NomeServico}\" corrompido — descartado."); }
            }
            return campos;
        }

        private List<(Guid Id, string Plano, bool Usado)> DecifrarCodigosRecuperacao(Senha senha, ServicoCriptografia crypto)
        {
            var codigos = new List<(Guid, string, bool)>();
            foreach (var codigo in senha.CodigosRecuperacao)
            {
                try { codigos.Add((codigo.Id, crypto.Descriptografar(codigo.Codigo), codigo.Usado)); }
                catch { _avisos.Add($"Código de recuperação de \"{senha.NomeServico}\" corrompido — descartado."); }
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
