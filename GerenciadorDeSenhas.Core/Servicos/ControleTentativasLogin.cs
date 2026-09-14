using System.Text.Json;

namespace GerenciadorDeSenhas.Servicos
{
    // Guarda o contador de tentativas erradas de senha mestra em disco, não só em
    // memória — sem isto, o limite de 5 tentativas (JanelaLogin) só durava até o
    // usuário (ou um atacante testando senhas na tela de login) fechar e reabrir o
    // app: cada JanelaLogin nova começa com o contador zerado, então reiniciar o
    // processo a cada 4 tentativas driblava o bloqueio por completo.
    public class ControleTentativasLogin
    {
        public const int LimiteTentativas = 5;

        // Bloqueio por rodada de LimiteTentativas erros seguidos; um bloqueio fixo deixava
        // testar ~5 senhas a cada 5s pra sempre. RegistrarSucesso zera a escala.
        public static readonly TimeSpan[] Escalada =
        {
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(1),
        };

        private readonly string _caminho;

        public ControleTentativasLogin(string pastaApp) =>
            _caminho = Path.Combine(pastaApp, "tentativas.dat");

        private sealed class Estado
        {
            public int Tentativas { get; set; }
            public int Rodadas { get; set; }
            public DateTime? BloqueadoAteUtc { get; set; }
        }

        // Instante (UTC) até quando o login deve continuar bloqueado, ou null se
        // não há bloqueio em vigor agora — usado ao abrir a janela, pra um bloqueio
        // de uma sessão anterior continuar valendo em vez de ser esquecido.
        public DateTime? ObterBloqueioAtivo()
        {
            var estado = Ler();
            return estado.BloqueadoAteUtc is { } ate && ate > DateTime.UtcNow ? ate : null;
        }

        // Registra uma tentativa com senha errada. BloqueioAteUtc vem preenchido só
        // se o limite acabou de ser atingido agora.
        public (int Tentativas, DateTime? BloqueioAteUtc) RegistrarFalha()
        {
            var estado = Ler();

            // Bloqueio expirado zera Tentativas mas NÃO Rodadas — senão bastava esperar
            // cada bloqueio expirar pra ter tentativas de novo sem escalar a punição.
            if (estado.BloqueadoAteUtc is { } ateAnterior && ateAnterior <= DateTime.UtcNow)
            {
                estado.Tentativas = 0;
                estado.BloqueadoAteUtc = null;
            }

            estado.Tentativas++;

            DateTime? bloqueioNovo = null;
            if (estado.Tentativas >= LimiteTentativas)
            {
                var indice = Math.Min(estado.Rodadas, Escalada.Length - 1);
                bloqueioNovo = DateTime.UtcNow + Escalada[indice];
                estado.BloqueadoAteUtc = bloqueioNovo;
                estado.Rodadas++;
            }

            Gravar(estado);
            return (estado.Tentativas, bloqueioNovo);
        }

        public void RegistrarSucesso() => Limpar();

        // Também usado por "Excluir cofre" — apaga todo rastro de tentativas
        // anteriores junto com auth.dat, senhas.json.enc etc.
        public void Limpar()
        {
            try { if (File.Exists(_caminho)) File.Delete(_caminho); }
            catch { }
        }

        private Estado Ler()
        {
            try
            {
                if (!File.Exists(_caminho))
                    return new Estado();

                return JsonSerializer.Deserialize<Estado>(File.ReadAllText(_caminho)) ?? new Estado();
            }
            catch
            {
                return new Estado();
            }
        }

        private void Gravar(Estado estado)
        {
            try { EscritaAtomica.EscreverTexto(_caminho, JsonSerializer.Serialize(estado)); }
            catch { }
        }
    }
}
