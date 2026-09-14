using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace CofreDeSenhas
{
    internal enum ResultadoDigitacao
    {
        Ok,
        AlvoIndisponivel,
        FocoNaoObtido,
        NaoSuportado,
        Falha
    }

    internal readonly record struct AlvoDigitacao(nint Janela, string Titulo, string Processo);

    // Preenche usuário e senha na janela que estava em foco antes do cofre (a que
    // a pessoa deixou pra vir buscar a credencial). Sequência fixa: usuário, Tab,
    // senha — sem Enter. Só Windows; no resto Suportado é false e os botões somem.
    //
    // A "janela anterior" é rastreada por amostragem: enquanto o cofre está aberto,
    // um timer olha de tempos em tempos qual é a janela em primeiro plano e guarda
    // a última que não é do próprio processo. Assim, quando a pessoa volta ao cofre
    // pra acionar a digitação, o alvo ainda é a janela de onde ela veio.
    internal static class DigitacaoAutomatica
    {
        private static readonly object Trava = new();
        private static Timer? _observador;
        private static AlvoDigitacao? _ultimo;
        private static nint _handleProcessoCache;
        private static string _processoCache = "";
        private static int _pidProprio;

        public static bool Suportado => OperatingSystem.IsWindows();

        public static void ComecarAObservar()
        {
            if (!Suportado)
                return;

            lock (Trava)
            {
                if (_observador != null)
                    return;

                _pidProprio = Environment.ProcessId;
                _observador = new Timer(_ => Amostrar(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(200));
            }
        }

        public static void PararDeObservar()
        {
            lock (Trava)
            {
                _observador?.Dispose();
                _observador = null;
                _ultimo = null;
                _handleProcessoCache = 0;
                _processoCache = "";
            }
        }

        public static AlvoDigitacao? AlvoAtual()
        {
            lock (Trava)
            {
                if (_ultimo is { } alvo && IsWindow(alvo.Janela))
                    return alvo;
                return null;
            }
        }

        private static void Amostrar()
        {
            try
            {
                var h = GetForegroundWindow();
                if (h == 0 || !QualificaComoAlvo(h))
                    return;

                var titulo = TituloDe(h);
                if (string.IsNullOrWhiteSpace(titulo))
                    return;

                var alvo = new AlvoDigitacao(h, titulo, ProcessoDe(h));
                lock (Trava)
                    _ultimo = alvo;
            }
            catch
            {
                // amostragem é melhor esforço; um erro isolado não derruba o timer
            }
        }

        private static bool QualificaComoAlvo(nint h)
        {
            if (!IsWindowVisible(h) || GetAncestor(h, GA_ROOT) != h)
                return false;

            _ = GetWindowThreadProcessId(h, out var pid);
            if (pid == 0 || pid == _pidProprio)
                return false;

            var estiloEstendido = GetWindowLongPtrW(h, GWL_EXSTYLE).ToInt64();
            return (estiloEstendido & WS_EX_TOOLWINDOW) == 0;
        }

        private static string TituloDe(nint h)
        {
            var tamanho = GetWindowTextLengthW(h);
            if (tamanho <= 0)
                return "";

            var buffer = new StringBuilder(tamanho + 1);
            GetWindowTextW(h, buffer, buffer.Capacity);
            return buffer.ToString();
        }

        // Process.GetProcessById abre um handle a cada chamada; a amostragem roda
        // 5x/s, então guarda o nome enquanto a janela em foco não muda.
        private static string ProcessoDe(nint h)
        {
            if (h == _handleProcessoCache)
                return _processoCache;

            try
            {
                _ = GetWindowThreadProcessId(h, out var pid);
                using var proc = System.Diagnostics.Process.GetProcessById((int)pid);
                _processoCache = proc.ProcessName;
            }
            catch
            {
                _processoCache = "";
            }

            _handleProcessoCache = h;
            return _processoCache;
        }

        public static async Task<ResultadoDigitacao> DigitarAsync(AlvoDigitacao alvo, string usuario, string senha, int atrasoMs = 140)
        {
            if (!Suportado)
                return ResultadoDigitacao.NaoSuportado;
            if (!IsWindow(alvo.Janela))
                return ResultadoDigitacao.AlvoIndisponivel;

            var sequencia = MontarSequencia(usuario, senha);
            if (sequencia.Count == 0)
                return ResultadoDigitacao.Falha;

            try
            {
                if (IsIconic(alvo.Janela))
                    ShowWindow(alvo.Janela, SW_RESTORE);

                TrazerParaFrente(alvo.Janela);
                await Task.Delay(atrasoMs).ConfigureAwait(false);

                // Verificação de segurança: só digita se a janela que a pessoa
                // confirmou está mesmo em primeiro plano agora. Se o foco não veio
                // (bloqueio de foreground do Windows, a janela sumiu), aborta sem
                // mandar tecla nenhuma pra não vazar a senha no lugar errado.
                if (GetForegroundWindow() != alvo.Janela)
                    return ResultadoDigitacao.FocoNaoObtido;

                EnviarSequencia(sequencia);
                return ResultadoDigitacao.Ok;
            }
            catch (Exception ex)
            {
                Diagnostico.Registrar(ex, "DigitacaoAutomatica");
                return ResultadoDigitacao.Falha;
            }
        }

        internal enum TipoEntrada { Caractere, Tab }

        internal readonly record struct EntradaDigitacao(TipoEntrada Tipo, char Caractere);

        // usuário -> Tab -> senha, sem Enter. Tab e usuário só entram quando há
        // usuário; credencial só-senha digita direto no campo em foco. CR/LF/Tab são
        // removidos dos valores pra não submeter o formulário nem pular de campo
        // fora de hora.
        internal static IReadOnlyList<EntradaDigitacao> MontarSequencia(string usuario, string senha)
        {
            var u = Limpar(usuario);
            var s = Limpar(senha);

            var lista = new List<EntradaDigitacao>(u.Length + 1 + s.Length);
            foreach (var c in u)
                lista.Add(new EntradaDigitacao(TipoEntrada.Caractere, c));
            if (u.Length > 0)
                lista.Add(new EntradaDigitacao(TipoEntrada.Tab, '\0'));
            foreach (var c in s)
                lista.Add(new EntradaDigitacao(TipoEntrada.Caractere, c));
            return lista;
        }

        private static string Limpar(string? valor)
        {
            if (string.IsNullOrEmpty(valor))
                return "";

            var sb = new StringBuilder(valor.Length);
            foreach (var c in valor)
                if (c != '\r' && c != '\n' && c != '\t')
                    sb.Append(c);
            return sb.ToString();
        }

        private static void TrazerParaFrente(nint alvo)
        {
            var threadAlvo = GetWindowThreadProcessId(alvo, out _);
            var threadAtual = GetCurrentThreadId();
            var anexado = threadAlvo != 0 && threadAlvo != threadAtual && AttachThreadInput(threadAtual, threadAlvo, true);
            try
            {
                BringWindowToTop(alvo);
                SetForegroundWindow(alvo);
            }
            finally
            {
                if (anexado)
                    AttachThreadInput(threadAtual, threadAlvo, false);
            }
        }

        private static void EnviarSequencia(IReadOnlyList<EntradaDigitacao> sequencia)
        {
            var entradas = new List<INPUT>(sequencia.Count * 2);
            foreach (var item in sequencia)
            {
                if (item.Tipo == TipoEntrada.Tab)
                {
                    entradas.Add(TeclaVirtual(VK_TAB, cima: false));
                    entradas.Add(TeclaVirtual(VK_TAB, cima: true));
                }
                else
                {
                    entradas.Add(TeclaUnicode(item.Caractere, cima: false));
                    entradas.Add(TeclaUnicode(item.Caractere, cima: true));
                }
            }

            var arr = entradas.ToArray();
            var enviados = SendInput((uint)arr.Length, arr, Marshal.SizeOf<INPUT>());
            if (enviados != arr.Length)
                throw new InvalidOperationException($"SendInput enviou {enviados} de {arr.Length} eventos.");
        }

        private static INPUT TeclaUnicode(char caractere, bool cima) => new()
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = caractere,
                    dwFlags = KEYEVENTF_UNICODE | (cima ? KEYEVENTF_KEYUP : 0),
                    time = 0,
                    dwExtraInfo = GetMessageExtraInfo()
                }
            }
        };

        private static INPUT TeclaVirtual(ushort vk, bool cima) => new()
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    wScan = 0,
                    dwFlags = cima ? KEYEVENTF_KEYUP : 0,
                    time = 0,
                    dwExtraInfo = GetMessageExtraInfo()
                }
            }
        };

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const ushort VK_TAB = 0x09;
        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_TOOLWINDOW = 0x00000080;
        private const uint GA_ROOT = 2;
        private const int SW_RESTORE = 9;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public nint dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public nint dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [DllImport("user32.dll")]
        private static extern nint GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(nint hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(nint hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(nint hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(nint hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(nint hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(nint hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern nint GetAncestor(nint hWnd, uint gaFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextW(nint hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLengthW(nint hWnd);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern nint GetWindowLongPtrW(nint hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern nint GetMessageExtraInfo();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
    }
}
