using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace App
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Preferencias.Carregar();
            Theme.DefinirModo(Preferencias.ModoEscuro);

            var auth = new AutenticacaoMestra();
            byte[] chave;
            using (var login = new FormLogin(auth))
            {
                if (login.ShowDialog() != DialogResult.OK || login.ChaveDerivada == null)
                    return;

                chave = login.ChaveDerivada;
            }

            var criptografia = new ServicoCriptografia(chave);
            var persistencia = new PersistenciaLocal(criptografia);
            var repositorio = new RepositorioSenha(persistencia, chave);
            var servicoSenha = new ServicoSenha(repositorio, criptografia);

            Application.Run(new FormRedesenhado(servicoSenha, criptografia));
        }
    }
}
