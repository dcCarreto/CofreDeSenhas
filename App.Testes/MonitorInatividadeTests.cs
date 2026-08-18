using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using CofreDeSenhas;

namespace App.Testes
{
    public class MonitorInatividadeTests
    {
        [AvaloniaFact]
        public void SemAtividade_ExpiraAoUltrapassarLimite()
        {
            var janela = new Window();
            janela.Show();

            var expirou = false;
            var monitor = new MonitorInatividade(janela, () => expirou = true);
            monitor._limite = TimeSpan.FromMilliseconds(1);
            Thread.Sleep(5);

            monitor.Verificar(null, EventArgs.Empty);

            Assert.True(expirou);
        }

        [AvaloniaFact]
        public void AtividadeNoDialogoVinculado_ImpedeExpiracao()
        {
            var janelaPrincipal = new Window();
            janelaPrincipal.Show();

            var campoDialogo = new TextBox();
            var dialogo = new Window { Content = campoDialogo };
            dialogo.Show();
            Dispatcher.UIThread.RunJobs();

            var expirou = false;
            var monitor = new MonitorInatividade(janelaPrincipal, () => expirou = true);
            monitor.Vincular(dialogo);
            monitor._limite = TimeSpan.FromMilliseconds(500);

            Thread.Sleep(50);
            campoDialogo.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.A });
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(50);

            monitor.Verificar(null, EventArgs.Empty);

            Assert.False(expirou);
        }

        [AvaloniaFact]
        public void Desvincular_AtividadeNoDialogoNaoImpedeMaisExpiracao()
        {
            var janelaPrincipal = new Window();
            janelaPrincipal.Show();

            var campoDialogo = new TextBox();
            var dialogo = new Window { Content = campoDialogo };
            dialogo.Show();

            var expirou = false;
            var monitor = new MonitorInatividade(janelaPrincipal, () => expirou = true);
            monitor.Vincular(dialogo);
            monitor.Desvincular(dialogo);
            monitor._limite = TimeSpan.FromMilliseconds(1);
            Thread.Sleep(5);

            campoDialogo.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.A });
            monitor.Verificar(null, EventArgs.Empty);

            Assert.True(expirou);
        }
    }
}
