using System.Security.Cryptography;
using System.Text;
using CofreDeSenhas;

namespace App.Testes
{
    public class ServicoAtualizacaoTests
    {
        private static readonly byte[] ChecksumsExemplo = Encoding.UTF8.GetBytes(
            "aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d  CofreDeSenhas-Setup-9.9.9.exe\n");

        [Fact]
        public void VerificarAssinaturaChecksums_AceitaAssinaturaDaChaveCorreta()
        {
            using var rsa = RSA.Create(3072);
            var assinatura = rsa.SignData(ChecksumsExemplo, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            Assert.True(ServicoAtualizacao.VerificarAssinaturaChecksums(
                rsa.ExportSubjectPublicKeyInfoPem(), ChecksumsExemplo, assinatura));
        }

        [Fact]
        public void VerificarAssinaturaChecksums_RejeitaConteudoAdulterado()
        {
            using var rsa = RSA.Create(3072);
            var assinatura = rsa.SignData(ChecksumsExemplo, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var adulterado = Encoding.UTF8.GetBytes(
                "0000000000000000000000000000000000000000  CofreDeSenhas-Setup-9.9.9.exe\n");

            Assert.False(ServicoAtualizacao.VerificarAssinaturaChecksums(
                rsa.ExportSubjectPublicKeyInfoPem(), adulterado, assinatura));
        }

        [Fact]
        public void VerificarAssinaturaChecksums_RejeitaAssinaturaDeOutraChave()
        {
            using var assinante = RSA.Create(3072);
            using var confiavel = RSA.Create(3072);
            var assinatura = assinante.SignData(ChecksumsExemplo, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            Assert.False(ServicoAtualizacao.VerificarAssinaturaChecksums(
                confiavel.ExportSubjectPublicKeyInfoPem(), ChecksumsExemplo, assinatura));
        }

        [Fact]
        public void VerificarAssinaturaChecksums_RejeitaChaveVaziaOuMalformada()
        {
            Assert.False(ServicoAtualizacao.VerificarAssinaturaChecksums("", ChecksumsExemplo, new byte[8]));
            Assert.False(ServicoAtualizacao.VerificarAssinaturaChecksums(
                "-----BEGIN PUBLIC KEY-----\nnao-e-uma-chave\n-----END PUBLIC KEY-----", ChecksumsExemplo, new byte[8]));
        }

        [Fact]
        public void ChavePublicaEmbutida_EhUmaChaveRsaValida()
        {
            Assert.True(ServicoAtualizacao.ChavePublicaAtualizacaoUtilizavel());
        }

        [Fact]
        public void ExtrairHash_EncontraHashDoArquivoNoFormatoDoSha256Sum()
        {
            var checksums = "d3b07384d113edec49eaa6238ad5ff00f6bc4033  CofreDeSenhas-Setup-2.1.0.exe\n" +
                             "aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d  CofreDeSenhas-2.1.0-win-x64-portatil.exe\n";

            var hash = ServicoAtualizacao.ExtrairHash(checksums, "CofreDeSenhas-2.1.0-win-x64-portatil.exe");

            Assert.Equal("aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d", hash);
        }

        [Fact]
        public void ExtrairHash_IgnoraAsteriscoDeModoBinario()
        {
            var checksums = "d3b07384d113edec49eaa6238ad5ff00f6bc4033 *CofreDeSenhas-Setup-2.1.0.exe\n";

            var hash = ServicoAtualizacao.ExtrairHash(checksums, "CofreDeSenhas-Setup-2.1.0.exe");

            Assert.Equal("d3b07384d113edec49eaa6238ad5ff00f6bc4033", hash);
        }

        [Fact]
        public void ExtrairHash_RetornaNuloQuandoArquivoNaoEstaNaLista()
        {
            var checksums = "d3b07384d113edec49eaa6238ad5ff00f6bc4033  CofreDeSenhas-Setup-2.1.0.exe\n";

            var hash = ServicoAtualizacao.ExtrairHash(checksums, "CofreDeSenhas-2.1.0-linux-x64.tar.gz");

            Assert.Null(hash);
        }

        [Fact]
        public void ExtrairHash_ComparacaoDeNomeIgnoraMaiusculasEMinusculas()
        {
            var checksums = "d3b07384d113edec49eaa6238ad5ff00f6bc4033  CofreDeSenhas-Setup-2.1.0.EXE\n";

            var hash = ServicoAtualizacao.ExtrairHash(checksums, "cofredesenhas-setup-2.1.0.exe");

            Assert.Equal("d3b07384d113edec49eaa6238ad5ff00f6bc4033", hash);
        }

        [Theory]
        [InlineData("v2.1.0", 2, 1, 0)]
        [InlineData("V2.1.0", 2, 1, 0)]
        [InlineData("2.1.0", 2, 1, 0)]
        public void ExtrairVersao_AceitaComOuSemPrefixoV(string tag, int major, int minor, int build)
        {
            var versao = ServicoAtualizacao.ExtrairVersao(tag);

            Assert.NotNull(versao);
            Assert.Equal(new Version(major, minor, build), versao);
        }

        [Fact]
        public void ExtrairVersao_RetornaNuloParaTagInvalida()
        {
            Assert.Null(ServicoAtualizacao.ExtrairVersao("versao-experimental"));
        }
    }
}
