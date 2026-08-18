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

        internal void Verificar(object? sender, EventArgs e)
        {
            if (_limite <= TimeSpan.Zero || DateTime.UtcNow - _ultimaAtividade < _limite)
                return;

            _relogio.Stop();
            _aoExpirar();
        }
    }
}
