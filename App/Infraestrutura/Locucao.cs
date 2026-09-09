using SherpaOnnx;

namespace CofreDeSenhas
{
    // Síntese de voz neural (Piper via sherpa-onnx) embutida no app — não usa o
    // TTS do sistema operacional. A voz de cada idioma é baixada sob demanda
    // (ver GerenciadorVozes); sem a voz do idioma atual instalada, os anúncios
    // ficam só no aviso visual.
    internal static class Locucao
    {
        private static readonly object _trava = new();
        private static OfflineTts? _tts;
        private static string _idiomaCarregado = "";
        private static bool _falhou;

        public static bool Disponivel => !_falhou && AudioPcm.Disponivel;

        public static bool VozInstaladaParaIdiomaAtual() =>
            GerenciadorVozes.Instalada(Idioma.Atual.Codigo);

        public static void Falar(string texto, bool interromper)
        {
            if (_falhou || string.IsNullOrWhiteSpace(texto))
                return;

            var codigo = GerenciadorVozes.Curto(Idioma.Atual.Codigo);
            if (!GerenciadorVozes.Instalada(codigo))
                return;

            _ = Task.Run(() =>
            {
                try
                {
                    OfflineTts tts;
                    lock (_trava)
                        tts = Obter(codigo);

                    var audio = tts.Generate(texto, 1.0f, 0);

                    if (interromper)
                        AudioPcm.Parar();
                    AudioPcm.Tocar(audio.Samples, audio.SampleRate);
                }
                catch
                {
                    _falhou = true;
                }
            });
        }

        public static void Silenciar() => AudioPcm.Parar();

        public static void RedefinirVoz()
        {
            lock (_trava)
            {
                _tts?.Dispose();
                _tts = null;
                _idiomaCarregado = "";
            }
        }

        private static OfflineTts Obter(string codigo)
        {
            if (_tts != null && _idiomaCarregado == codigo)
                return _tts;

            _tts?.Dispose();

            var config = new OfflineTtsConfig();
            config.Model.Vits.Model = GerenciadorVozes.CaminhoModelo(codigo);
            config.Model.Vits.Tokens = GerenciadorVozes.CaminhoTokens(codigo);
            config.Model.Vits.DataDir = GerenciadorVozes.PastaEspeak;
            config.Model.Vits.NoiseScale = 0.667f;
            config.Model.Vits.NoiseScaleW = 0.8f;
            config.Model.Vits.LengthScale = 1.0f;
            config.Model.NumThreads = 1;
            config.Model.Provider = "cpu";
            config.MaxNumSentences = 1;

            _tts = new OfflineTts(config);
            _idiomaCarregado = codigo;
            return _tts;
        }
    }
}
