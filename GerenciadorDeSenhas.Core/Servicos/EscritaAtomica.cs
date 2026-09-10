using System;
using System.IO;
using System.Threading.Tasks;

namespace GerenciadorDeSenhas.Servicos
{
    internal static class EscritaAtomica
    {
        public static void EscreverTexto(string caminho, string conteudo)
        {
            var temp = CaminhoTemp(caminho);
            try
            {
                File.WriteAllText(temp, conteudo);
                File.Move(temp, caminho, overwrite: true);
            }
            catch
            {
                LimparTemp(temp);
                throw;
            }
        }

        public static async Task EscreverTextoAsync(string caminho, string conteudo)
        {
            var temp = CaminhoTemp(caminho);
            try
            {
                await File.WriteAllTextAsync(temp, conteudo);
                File.Move(temp, caminho, overwrite: true);
            }
            catch
            {
                LimparTemp(temp);
                throw;
            }
        }

        public static async Task EscreverBytesAsync(string caminho, byte[] conteudo)
        {
            var temp = CaminhoTemp(caminho);
            try
            {
                await File.WriteAllBytesAsync(temp, conteudo);
                File.Move(temp, caminho, overwrite: true);
            }
            catch
            {
                LimparTemp(temp);
                throw;
            }
        }

        private static void LimparTemp(string temp)
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
        }

        private static string CaminhoTemp(string caminho) => caminho + "." + Guid.NewGuid().ToString("N") + ".tmp";
    }
}
