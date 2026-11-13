using Avalonia.Threading;

namespace CofreDeSenhas
{
    // Atualização sem intervenção do usuário. Enquanto o cofre está aberto, procura e
    // baixa a atualização em segundo plano (ao desbloquear e a cada 6 horas); a
    // instalação fica para o próximo bloqueio do cofre, manual ou por inatividade (ver
    // App.Bloquear). É o único momento em que reiniciar o app não interrompe nada: o
    // cofre acabou de ser fechado e ninguém está digitando nele.
    //
    // Instalação comum (instalador, winget, portátil, AppImage): releases do GitHub com
    // assinatura conferida (ServicoAtualizacao). Microsoft Store: APIs da própria Loja
    // (AtualizacaoLoja).
    internal static class AtualizacaoAutomatica
    {
        private static readonly TimeSpan IntervaloVerificacao = TimeSpan.FromHours(6);

        private static DispatcherTimer? _relogio;
        private static bool _verificando;
        private static AtualizacaoBaixada? _baixada;
        private static bool _lojaPronta;

        // Disparado quando uma atualização fica pronta para instalar ou deixa de estar.
        public static event Action? ProntaMudou;

        public static bool Pronta => _baixada != null || _lojaPronta;

        // Pronta e instalável sem ninguém por perto. Instalação "para todos os
        // usuários" pede permissão de administrador (UAC): no bloqueio por
        // inatividade ninguém responderia, o pedido expiraria e o app ficaria fechado.
        // Nesse caso só o botão "Instalar agora" instala — quem clica está presente.
        public static bool InstalavelNoBloqueio => _lojaPronta || _baixada is { ExigeAdministrador: false };

        public static bool ExigeAdministrador => _baixada is { ExigeAdministrador: true };

        // Notas da versão baixada (markdown cru do GitHub); na Loja não há.
        public static string? NotasVersao { get; private set; }

        // Na Loja vale a configuração de atualização da própria Loja (ver
        // AtualizacaoLoja); fora dela, a preferência do app — e só onde a atualização
        // embutida funciona (não em Debug/COFRE_BASE, nem no .tar.gz do Linux).
        public static bool Ativa =>
            PacoteLoja.Empacotado || (Preferencias.AtualizacaoAutomatica && ServicoAtualizacao.Suportada);

        public static void Iniciar()
        {
            if (!Ativa)
                return;

            if (_relogio == null)
            {
                _relogio = new DispatcherTimer { Interval = IntervaloVerificacao };
                _relogio.Tick += (s, e) => _ = VerificarAsync();
                _relogio.Start();
            }
            _ = VerificarAsync();
        }

        public static void Parar()
        {
            _relogio?.Stop();
            _relogio = null;
            if (_baixada != null)
            {
                ServicoAtualizacao.Descartar(_baixada);
                _baixada = null;
                NotasVersao = null;
                ProntaMudou?.Invoke();
            }
        }

        private static async Task VerificarAsync()
        {
            if (_verificando || Pronta || !Ativa)
                return;

            _verificando = true;
            try
            {
                if (PacoteLoja.Empacotado)
                {
                    _lojaPronta = await AtualizacaoLoja.BaixarAsync();
                }
                else
                {
                    if (await ServicoAtualizacao.VerificarNovaVersaoAsync() is not { } disponivel)
                        return;

                    var resultado = await ServicoAtualizacao.BaixarAsync(disponivel.Tag);
                    if (resultado.Falha != null)
                        Diagnostico.Registrar(new InvalidOperationException(resultado.Falha), "AtualizacaoAutomatica.Baixar");

                    if (resultado.Baixada is { } baixada)
                    {
                        // Desligada pelo usuário no meio do download: descarta em vez de
                        // instalar mesmo assim no próximo bloqueio.
                        if (Ativa)
                        {
                            _baixada = baixada;
                            NotasVersao = disponivel.NotasVersao;
                        }
                        else
                        {
                            ServicoAtualizacao.Descartar(baixada);
                        }
                    }
                }

                if (Pronta)
                    ProntaMudou?.Invoke();
            }
            catch (Exception ex)
            {
                // Sem internet, GitHub/Loja fora do ar etc.: tenta de novo na próxima
                // verificação, sem incomodar o usuário.
                Diagnostico.Registrar(ex, "AtualizacaoAutomatica");
            }
            finally
            {
                _verificando = false;
            }
        }

        // Fora da Loja: inicia a instalação da atualização baixada (instalador
        // silencioso, ou troca do executável portátil/AppImage). true = quem chamou deve
        // encerrar o app agora — a versão nova é aberta sozinha.
        public static async Task<bool> AplicarLocalAsync()
        {
            if (_baixada is not { } baixada)
                return false;

            _baixada = null;
            NotasVersao = null;
            try
            {
                await ServicoAtualizacao.AplicarAsync(baixada);
                return true;
            }
            catch (Exception ex)
            {
                Diagnostico.Registrar(ex, "AtualizacaoAutomatica.Aplicar");
                ServicoAtualizacao.Descartar(baixada);
                ProntaMudou?.Invoke();
                return false;
            }
        }

        // Na Loja: pede a instalação; o Windows fecha o app e o reabre ao terminar.
        // Retornar daqui significa que não instalou.
        public static async Task<bool> InstalarLojaAsync()
        {
            if (!_lojaPronta)
                return false;

            _lojaPronta = false;
            var instalou = false;
            try
            {
                instalou = await AtualizacaoLoja.InstalarAsync();
            }
            catch (Exception ex)
            {
                Diagnostico.Registrar(ex, "AtualizacaoAutomatica.InstalarLoja");
            }

            if (!instalou)
                ProntaMudou?.Invoke();
            return instalou;
        }
    }
}
