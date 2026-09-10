using System.Security.Cryptography;
using GerenciadorDeSenhas;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Cli
{
    // Abre o cofre local para leitura: pede a senha mestra no terminal, respeita o
    // mesmo bloqueio escalonado por tentativas erradas do aplicativo e zera a chave
    // ao ser descartado. Não escreve nada no cofre.
    internal sealed class Cofre : IDisposable
    {
        private readonly byte[] _chave;
        private readonly ServicoCriptografia _criptografia;
        private readonly RepositorioSenha _repositorio;

        private Cofre(byte[] chave, ServicoCriptografia criptografia, RepositorioSenha repositorio)
        {
            _chave = chave;
            _criptografia = criptografia;
            _repositorio = repositorio;
        }

        public IServicoCriptografia Criptografia => _criptografia;

        public Task<List<Senha>> ListarAsync() => _repositorio.ListarTodosAsync();

        // Retorna null e escreve o motivo em stderr quando não dá para abrir.
        public static Cofre? Abrir(out int codigoSaida)
        {
            codigoSaida = 0;
            var auth = new AutenticacaoMestra();

            if (!auth.ExisteSenhaMestra())
            {
                Console.Error.WriteLine($"Nenhum cofre encontrado em {auth.PastaApp}.");
                Console.Error.WriteLine("Crie um abrindo o aplicativo pelo menos uma vez.");
                codigoSaida = 4;
                return null;
            }

            var tentativas = new ControleTentativasLogin(auth.PastaApp);
            if (tentativas.ObterBloqueioAtivo() is { } bloqueadoAte)
            {
                var faltam = bloqueadoAte - DateTime.UtcNow;
                Console.Error.WriteLine(
                    $"Cofre bloqueado por tentativas erradas. Tente de novo em {Formatar.Duracao(faltam)} " +
                    $"(até {bloqueadoAte.ToLocalTime():HH:mm:ss}).");
                codigoSaida = 3;
                return null;
            }

            var senha = EntradaSenha.LerOculto("Senha mestra: ");
            if (string.IsNullOrEmpty(senha))
            {
                Console.Error.WriteLine("Senha mestra não informada.");
                codigoSaida = 1;
                return null;
            }

            var chave = auth.Autenticar(senha);
            if (chave == null)
            {
                var (feitas, novoBloqueio) = tentativas.RegistrarFalha();
                if (novoBloqueio is { } ate)
                    Console.Error.WriteLine(
                        $"Senha incorreta. Limite atingido — cofre bloqueado até {ate.ToLocalTime():HH:mm:ss}.");
                else
                    Console.Error.WriteLine(
                        $"Senha incorreta. ({feitas}/{ControleTentativasLogin.LimiteTentativas} antes do bloqueio)");
                codigoSaida = 1;
                return null;
            }

            tentativas.RegistrarSucesso();

            var criptografia = new ServicoCriptografia(chave, travarNaMemoria: true);
            var persistencia = new PersistenciaLocal(criptografia);
            var repositorio = new RepositorioSenha(persistencia, chave);
            return new Cofre(chave, criptografia, repositorio);
        }

        public void Dispose()
        {
            _criptografia.ZerarChave();
            CryptographicOperations.ZeroMemory(_chave);
        }
    }
}
