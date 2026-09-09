using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace CofreDeSenhas
{
    public sealed class MonitorInatividade
    {
        private readonly Action _aoExpirar;
        private readonly DispatcherTimer _relogio;
        private DateTime _ultimaAtividade = DateTime.UtcNow;
        internal TimeSpan _limite;
        private bool _avisando;

        // Aviso de antecedência (WCAG 2.2 "Tempo Suficiente"): recebe os segundos
        // restantes e devolve true se o usuário quer continuar conectado. Só entra
        // em ação enquanto AvisoHabilitado devolver true.
        public Func<int, Task<bool>>? AoAvisar { get; set; }
        public Func<bool>? AvisoHabilitado { get; set; }
        public TimeSpan Antecedencia { get; set; } = TimeSpan.FromSeconds(20);

        public MonitorInatividade(InputElement alvo, Action aoExpirar)
        {
            _aoExpirar = aoExpirar;

            _relogio = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _relogio.Tick += Verificar;

            Vincular(alvo);
        }

        // Diálogos modais (Nova Senha, Editar Senha, Conexão de Banco etc.) são
        // janelas próprias, fora da árvore visual da janela principal — sem isto,
        // digitar neles não conta como atividade e o bloqueio automático pode
        // fechá-los à força no meio da edição, descartando o que não foi salvo.
        public void Vincular(InputElement alvo)
        {
            alvo.AddHandler(InputElement.PointerMovedEvent, Registrar, RoutingStrategies.Tunnel, true);
            alvo.AddHandler(InputElement.PointerPressedEvent, Registrar, RoutingStrategies.Tunnel, true);
            alvo.AddHandler(InputElement.PointerWheelChangedEvent, Registrar, RoutingStrategies.Tunnel, true);
            alvo.AddHandler(InputElement.KeyDownEvent, Registrar, RoutingStrategies.Tunnel, true);
            _ultimaAtividade = DateTime.UtcNow;
        }

        public void Desvincular(InputElement alvo)
        {
            alvo.RemoveHandler(InputElement.PointerMovedEvent, Registrar);
            alvo.RemoveHandler(InputElement.PointerPressedEvent, Registrar);
            alvo.RemoveHandler(InputElement.PointerWheelChangedEvent, Registrar);
            alvo.RemoveHandler(InputElement.KeyDownEvent, Registrar);
            _ultimaAtividade = DateTime.UtcNow;
        }

        public void Ajustar(int minutos)
        {
            _limite = TimeSpan.FromMinutes(minutos);
            _ultimaAtividade = DateTime.UtcNow;
            _relogio.IsEnabled = minutos > 0;
        }

        public void Encerrar() => _relogio.Stop();

        private void Registrar(object? sender, RoutedEventArgs e) => _ultimaAtividade = DateTime.UtcNow;

        internal async void Verificar(object? sender, EventArgs e)
        {
            if (_limite <= TimeSpan.Zero || _avisando)
                return;

            var ocioso = DateTime.UtcNow - _ultimaAtividade;
            var restante = _limite - ocioso;

            if (restante <= TimeSpan.Zero)
            {
                _relogio.Stop();
                _aoExpirar();
                return;
            }

            if (AoAvisar == null || restante > Antecedencia || AvisoHabilitado?.Invoke() != true)
                return;

            _avisando = true;
            try
            {
                var ficar = await AoAvisar((int)Math.Ceiling(restante.TotalSeconds));
                if (ficar)
                {
                    _ultimaAtividade = DateTime.UtcNow;
                }
                else
                {
                    _relogio.Stop();
                    _aoExpirar();
                }
            }
            finally
            {
                _avisando = false;
            }
        }
    }
}
