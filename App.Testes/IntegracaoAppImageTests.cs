using CofreDeSenhas;

namespace App.Testes
{
    public class IntegracaoAppImageTests : IDisposable
    {
        private const string Modelo =
            "[Desktop Entry]\nType=Application\nName=Cofre de Senhas\nExec=CofreDeSenhas\nIcon=cofre-de-senhas\n";

        private readonly string _raiz = Path.Combine(Path.GetTempPath(), "IntegracaoAppImageTests-" + Guid.NewGuid());
        private readonly string _appDir;
        private readonly string _dados;
        private string Atalho => Path.Combine(_dados, "applications", IntegracaoAppImage.NomeArquivo);

        public IntegracaoAppImageTests()
        {
            _appDir = Directory.CreateDirectory(Path.Combine(_raiz, "AppDir")).FullName;
            _dados = Path.Combine(_raiz, "share");
            File.WriteAllText(Path.Combine(_appDir, IntegracaoAppImage.NomeArquivo), Modelo);
            File.WriteAllBytes(Path.Combine(_appDir, "cofre-de-senhas.png"), [1, 2, 3]);
        }

        public void Dispose()
        {
            try { Directory.Delete(_raiz, recursive: true); } catch { }
        }

        [Fact]
        public void MontarAtalho_ExecutaOAppImageComCaminhoEscapado()
        {
            var atalho = IntegracaoAppImage.MontarAtalho(Modelo, "/home/ana/Meus Apps/Cofre 100%.AppImage");

            Assert.Contains("Exec=\"/home/ana/Meus Apps/Cofre 100%%.AppImage\"\n", atalho);
            Assert.Contains("TryExec=/home/ana/Meus Apps/Cofre 100%.AppImage\n", atalho);
            Assert.DoesNotContain("Exec=CofreDeSenhas", atalho);
            Assert.Contains(IntegracaoAppImage.Marca, atalho);
        }

        [Fact]
        public void ArgumentoExec_EscapaCaracteresReservados()
        {
            Assert.Equal("\"/a/\\\\$b\\\\`c\\\\\\\\d\"", IntegracaoAppImage.ArgumentoExec("/a/$b`c\\d"));
        }

        [Fact]
        public void Integrar_CriaAtalhoEIconeEDepoisNaoRegrava()
        {
            Assert.True(IntegracaoAppImage.Integrar("/opt/Cofre.AppImage", _appDir, _dados));
            Assert.Contains("Exec=\"/opt/Cofre.AppImage\"", File.ReadAllText(Atalho));
            Assert.True(File.Exists(Path.Combine(_dados, "icons", "hicolor", "256x256", "apps", "cofre-de-senhas.png")));

            Assert.False(IntegracaoAppImage.Integrar("/opt/Cofre.AppImage", _appDir, _dados));
        }

        [Fact]
        public void Integrar_AtualizaCaminhoQuandoOAppImageMuda()
        {
            IntegracaoAppImage.Integrar("/opt/Cofre.AppImage", _appDir, _dados);

            Assert.True(IntegracaoAppImage.Integrar("/home/ana/Cofre-novo.AppImage", _appDir, _dados));
            Assert.Contains("Exec=\"/home/ana/Cofre-novo.AppImage\"", File.ReadAllText(Atalho));
        }

        [Fact]
        public void Integrar_NaoMexeNoAtalhoDoInstalarSh()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Atalho)!);
            File.WriteAllText(Atalho, "[Desktop Entry]\nExec=/home/ana/.local/opt/cofre-de-senhas/CofreDeSenhas\n");

            Assert.False(IntegracaoAppImage.Integrar("/opt/Cofre.AppImage", _appDir, _dados));
            Assert.DoesNotContain("AppImage", File.ReadAllText(Atalho));
        }

        [Fact]
        public void RemoverAtalho_ApagaSoOAtalhoDoAppImage()
        {
            IntegracaoAppImage.Integrar("/opt/Cofre.AppImage", _appDir, _dados);
            Assert.True(IntegracaoAppImage.RemoverAtalho(_dados));
            Assert.False(File.Exists(Atalho));

            File.WriteAllText(Atalho, "[Desktop Entry]\nExec=/home/ana/.local/opt/cofre-de-senhas/CofreDeSenhas\n");
            Assert.False(IntegracaoAppImage.RemoverAtalho(_dados));
            Assert.True(File.Exists(Atalho));
        }

        [Fact]
        public void Integrar_JaIntegradoPorOutro_NaoDuplicaERemoveONosso()
        {
            IntegracaoAppImage.Integrar("/opt/Cofre.AppImage", _appDir, _dados);
            File.WriteAllText(Path.Combine(_dados, "applications", "appimagekit_abc-Cofre.desktop"),
                "[Desktop Entry]\nExec=/opt/Cofre.AppImage %U\n");

            Assert.True(IntegracaoAppImage.Integrar("/opt/Cofre.AppImage", _appDir, _dados));
            Assert.False(File.Exists(Atalho));
            Assert.False(IntegracaoAppImage.Integrar("/opt/Cofre.AppImage", _appDir, _dados));
        }
    }
}
