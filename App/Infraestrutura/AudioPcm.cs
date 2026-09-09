using System.Runtime.InteropServices;
using PortAudioSharp;

namespace CofreDeSenhas
{
    // Reprodução de PCM float32 mono via PortAudio (bundle nativo próprio, não usa
    // o TTS nem APIs de fala do SO). Serve só aos avisos de acessibilidade.
    internal static class AudioPcm
    {
        private static readonly object _trava = new();
        private static bool _iniciado;
        private static bool _falhou;
        private static PortAudioSharp.Stream? _stream;
        private static PortAudioSharp.Stream.Callback? _callbackAtual;

        public static bool Disponivel => !_falhou;

        public static void Tocar(float[] amostras, int taxaAmostragem)
        {
            if (_falhou || amostras.Length == 0)
                return;

            try
            {
                lock (_trava)
                {
                    if (!Garantir())
                        return;

                    PararInterno();

                    var pos = 0;
                    PortAudioSharp.Stream.Callback cb = (IntPtr entrada, IntPtr saida, uint quadros,
                        ref StreamCallbackTimeInfo tempo, StreamCallbackFlags status, IntPtr usuario) =>
                    {
                        var pedidos = (int)quadros;
                        var restantes = amostras.Length - pos;

                        if (restantes <= 0)
                        {
                            Zerar(saida, 0, pedidos);
                            return StreamCallbackResult.Complete;
                        }

                        var copiar = Math.Min(pedidos, restantes);
                        Marshal.Copy(amostras, pos, saida, copiar);
                        pos += copiar;

                        if (copiar < pedidos)
                            Zerar(saida, copiar, pedidos - copiar);

                        return pos >= amostras.Length ? StreamCallbackResult.Complete : StreamCallbackResult.Continue;
                    };

                    var info = PortAudio.GetDeviceInfo(PortAudio.DefaultOutputDevice);
                    var saida = new StreamParameters
                    {
                        device = PortAudio.DefaultOutputDevice,
                        channelCount = 1,
                        sampleFormat = SampleFormat.Float32,
                        suggestedLatency = info.defaultLowOutputLatency,
                        hostApiSpecificStreamInfo = IntPtr.Zero
                    };

                    _callbackAtual = cb;
                    _stream = new PortAudioSharp.Stream(
                        inParams: null, outParams: saida, sampleRate: taxaAmostragem,
                        framesPerBuffer: 0, streamFlags: StreamFlags.ClipOff,
                        callback: cb, userData: IntPtr.Zero);
                    _stream.Start();
                }
            }
            catch
            {
                _falhou = true;
            }
        }

        public static void Parar()
        {
            lock (_trava)
            {
                try { PararInterno(); } catch { }
            }
        }

        private static void PararInterno()
        {
            if (_stream == null)
                return;

            try { _stream.Abort(); } catch { }
            try { _stream.Dispose(); } catch { }
            _stream = null;
        }

        private static bool Garantir()
        {
            if (_iniciado)
                return true;
            if (_falhou)
                return false;

            try
            {
                PortAudio.Initialize();
                if (PortAudio.DefaultOutputDevice == PortAudio.NoDevice)
                {
                    _falhou = true;
                    return false;
                }
                _iniciado = true;
                return true;
            }
            catch
            {
                _falhou = true;
                return false;
            }
        }

        private static void Zerar(IntPtr saida, int deslocamentoFloats, int quantidadeFloats)
        {
            var bytes = quantidadeFloats * sizeof(float);
            Marshal.Copy(new byte[bytes], 0, IntPtr.Add(saida, deslocamentoFloats * sizeof(float)), bytes);
        }
    }
}
