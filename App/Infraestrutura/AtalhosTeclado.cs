using Avalonia.Input;

namespace CofreDeSenhas
{
    internal static class AtalhosTeclado
    {
        public enum Acao { Buscar, NovaSenha, AbrirGerador, BloquearAgora, CopiarUsuario, CopiarSenha }

        public sealed record Atalho(Acao Acao, string ChaveTextoAcao, Key Tecla, bool RequerShift, string[] TeclasExibicao);

        public static readonly IReadOnlyList<Atalho> Todos = new[]
        {
            new Atalho(Acao.Buscar, "Shortcuts.Search", Key.F, false, new[] { "Ctrl", "F" }),
            new Atalho(Acao.NovaSenha, "Shortcuts.NewPassword", Key.N, false, new[] { "Ctrl", "N" }),
            new Atalho(Acao.AbrirGerador, "Shortcuts.OpenGenerator", Key.G, false, new[] { "Ctrl", "G" }),
            new Atalho(Acao.BloquearAgora, "Shortcuts.LockNow", Key.L, false, new[] { "Ctrl", "L" }),
            new Atalho(Acao.CopiarUsuario, "Shortcuts.CopyUser", Key.U, true, new[] { "Ctrl", "Shift", "U" }),
            new Atalho(Acao.CopiarSenha, "Shortcuts.CopyPassword", Key.P, true, new[] { "Ctrl", "Shift", "P" }),
        };

        public static Atalho? Encontrar(Key tecla, KeyModifiers modificadores)
        {
            if (!modificadores.HasFlag(KeyModifiers.Control))
                return null;

            bool shift = modificadores.HasFlag(KeyModifiers.Shift);
            return Todos.FirstOrDefault(a => a.Tecla == tecla && a.RequerShift == shift);
        }
    }
}
