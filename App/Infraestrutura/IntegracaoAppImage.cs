namespace CofreDeSenhas
{
    // Rodando como AppImage, registra o app no menu de aplicativos do usuário (atalho
    // .desktop + ícone em ~/.local/share, sem sudo) — o que antes só acontecia com um
    // integrador de AppImage ou o instalar.sh. Sem isso, quem só executava o arquivo
    // tinha de abri-lo pelo terminal toda vez (#86).
    //
    // Roda a cada abertura e só regrava o que mudou: se o .AppImage for movido ou
    // trocado por outro com nome diferente, o atalho passa a apontar para o novo.
    internal static class IntegracaoAppImage
    {
        internal const string NomeArquivo = "cofre-de-senhas.desktop";
        internal const string Marca = "X-CofreDeSenhas-AppImage=true";

        public static void Integrar()
        {
            var appImage = Environment.GetEnvironmentVariable("APPIMAGE");
            var appDir = Environment.GetEnvironmentVariable("APPDIR");
            var debInstalado = PacoteDeb.PresenteNoSistema;
            if (!OperatingSystem.IsLinux() || GerenciadorDeSenhas.AmbienteCofre.Isolado ||
                (!debInstalado && (string.IsNullOrEmpty(appImage) || string.IsNullOrEmpty(appDir))))
                return;

            Task.Run(() =>
            {
                try
                {
                    // No Linux é $XDG_DATA_HOME ou ~/.local/share. DoNotVerify: sem ele, uma
                    // pasta que ainda não existe vira "" e o atalho iria para a pasta atual.
                    var pastaDados = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData,
                        Environment.SpecialFolderOption.DoNotVerify);
                    // Com o pacote .deb instalado, o atalho do menu é o dele: um atalho do
                    // AppImage com o mesmo nome em ~/.local/share teria prioridade e abriria o AppImage.
                    if (debInstalado)
                        RemoverAtalho(pastaDados);
                    else
                        Integrar(appImage!, appDir!, pastaDados);
                }
                catch (Exception ex)
                {
                    Diagnostico.Registrar(ex, "Integração do AppImage ao menu");
                }
            });
        }

        // Apaga o atalho criado pelo AppImage, se houver. true = apagou.
        internal static bool RemoverAtalho(string pastaDados)
        {
            var atalho = Path.Combine(pastaDados, "applications", NomeArquivo);
            if (!File.Exists(atalho) || !File.ReadAllText(atalho).Contains(Marca))
                return false;
            File.Delete(atalho);
            return true;
        }

        // Devolve true se gravou ou apagou o atalho.
        internal static bool Integrar(string appImage, string appDir, string pastaDados)
        {
            var pastaAtalhos = Path.Combine(pastaDados, "applications");
            var atalho = Path.Combine(pastaAtalhos, NomeArquivo);
            var existente = File.Exists(atalho) ? File.ReadAllText(atalho) : null;
            var ehNosso = existente == null || existente.Contains(Marca);

            // Atalho do instalar.sh (mesmo nome, sem a marca): não é nosso, fica como está.
            if (!ehNosso)
                return false;

            // Um integrador (AppImageLauncher, Gear Lever...) já criou o atalho dele para
            // este arquivo: o nosso seria uma entrada duplicada no menu.
            if (IntegradoPorOutro(pastaAtalhos, appImage))
            {
                if (existente == null)
                    return false;
                File.Delete(atalho);
                return true;
            }

            var novo = MontarAtalho(File.ReadAllText(Path.Combine(appDir, NomeArquivo)), appImage);
            var icone = Path.Combine(pastaDados, "icons", "hicolor", "256x256", "apps", "cofre-de-senhas.png");
            if (!File.Exists(icone))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(icone)!);
                File.Copy(Path.Combine(appDir, "cofre-de-senhas.png"), icone);
            }

            if (novo == existente)
                return false;

            Directory.CreateDirectory(pastaAtalhos);
            File.WriteAllText(atalho, novo);
            return true;
        }

        // O .desktop que vai dentro do AppImage tem Exec=CofreDeSenhas (relativo ao AppDir);
        // fora dele, o atalho executa o próprio .AppImage. TryExec esconde a entrada do
        // menu se o arquivo for apagado. O modelo só tem a seção [Desktop Entry].
        internal static string MontarAtalho(string modelo, string appImage)
        {
            var linhas = modelo.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n')
                .Where(l => !l.StartsWith("Exec=") && !l.StartsWith("TryExec="))
                .Append("Exec=" + ArgumentoExec(appImage))
                .Append("TryExec=" + EscaparString(appImage))
                .Append(Marca);
            return string.Join('\n', linhas) + "\n";
        }

        // Regras da Desktop Entry Specification: o argumento vai entre aspas com " ` $ \
        // escapados; depois vale o escape de string (\ vira \\), e % vira %%.
        internal static string ArgumentoExec(string caminho)
        {
            var citado = "\"" + caminho.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("`", "\\`").Replace("$", "\\$") + "\"";
            return EscaparString(citado).Replace("%", "%%");
        }

        private static string EscaparString(string valor) => valor.Replace("\\", "\\\\");

        private static bool IntegradoPorOutro(string pastaAtalhos, string appImage)
        {
            if (!Directory.Exists(pastaAtalhos))
                return false;

            foreach (var arquivo in Directory.EnumerateFiles(pastaAtalhos, "*.desktop"))
            {
                if (Path.GetFileName(arquivo) == NomeArquivo)
                    continue;
                try
                {
                    if (File.ReadLines(arquivo).Any(l => l.StartsWith("Exec=") && l.Contains(appImage)))
                        return true;
                }
                catch { }
            }
            return false;
        }
    }
}
