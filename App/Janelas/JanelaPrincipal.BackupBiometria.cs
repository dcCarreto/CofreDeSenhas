using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CofreDeSenhas.Controles;
using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Repositorios;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaPrincipal
    {
        private async void Backup_Click(object? sender, RoutedEventArgs e)
        {
            if (_criptografia == null)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Db.FeatureUnavailable"), Idioma.Texto("Backup.Title"), TipoMensagem.Aviso);
                return;
            }

            var persistencia = new PersistenciaLocal(_criptografia);
            var dlg = new JanelaBackup(persistencia, () => _servicoSenhaLocal.ListarTodosAsync(), _chaveMestra,
                permiteRestaurar: !_conectadoAoBanco);

            if (!await AbrirDialogoAsync<bool>(dlg) || dlg.BackupParaRestaurar is not { } caminho)
                return;

            await RestaurarBackupAsync(persistencia, caminho);
        }

        // internal só pra teste chamar direto sem precisar dirigir a JanelaBackup
        // inteira (lista de backups, clique de restaurar, confirmação) — ver
        // App.Testes (InternalsVisibleTo).
        internal async Task RestaurarBackupAsync(IPersistenciaLocal persistencia, string caminhoBackup)
        {
            try
            {
                var senhasRestauradas = await persistencia.CarregarBackupAsync(caminhoBackup);

                // Salva o estado atual como um backup antes de sobrescrever — sem isto,
                // restaurar um backup mais antigo descarta tudo que mudou depois sem
                // deixar nenhum jeito de desfazer, mesmo a própria janela de restauração
                // já avisando que as alterações mais recentes serão perdidas. Lido o
                // backup de destino ANTES desta chamada de propósito: BackupAutomaticoAsync
                // pode acabar apagando o backup mais antigo pra respeitar o teto — se for
                // justo o que o usuário escolheu restaurar, o conteúdo dele já está a
                // salvo em memória a essa altura.
                try
                {
                    var senhasAtuais = await _servicoSenhaLocal.ListarTodosAsync();
                    if (senhasAtuais.Count > 0)
                        await persistencia.BackupAutomaticoAsync(senhasAtuais, _chaveMestra, Preferencias.MaximoBackups);
                }
                catch
                {
                    // Melhor esforço: falhar na foto de segurança não pode impedir a
                    // restauração que o usuário pediu.
                }

                await persistencia.SalvarSenhasAsync(senhasRestauradas, _chaveMestra);

                _repositorioLocal = new RepositorioSenha(persistencia, _chaveMestra);
                _servicoSenhaLocal = new ServicoSenha(_repositorioLocal, _criptografia!);
                if (!_conectadoAoBanco)
                    _servicoSenha = _servicoSenhaLocal;

                LimparAuditoria();
                await CarregarSenhasAsync();

                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Backup.RestoreSuccess"), Idioma.Texto("Backup.RestoreTitle"));
            }
            catch (Exception ex)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Formatar("Backup.Error", ErrosUi.MensagemAmigavel(ex)), Idioma.Texto("Common.Error"), TipoMensagem.Erro);
            }
        }

        private async void Biometria_Click(object? sender, RoutedEventArgs e)
        {
            if (!_biometria.SistemaSuportado)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Biometric.UnsupportedPlatform"),
                    Idioma.Texto("Biometric.Title"),
                    TipoMensagem.Aviso);
                return;
            }

            if (_biometria.EstaHabilitado)
            {
                var confirmar = await CaixaMensagem.ConfirmarAsync(this,
                    Idioma.Texto("Biometric.DisableConfirm"),
                    Idioma.Texto("Biometric.Title"));
                if (!confirmar)
                    return;

                await _biometria.DesabilitarAsync();
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Biometric.Disabled"),
                    Idioma.Texto("Biometric.Title"));
                return;
            }

            var resultado = await _biometria.HabilitarAsync(this, _chaveMestra);
            if (resultado.Sucesso)
            {
                await CaixaMensagem.MostrarAsync(this,
                    Idioma.Texto("Biometric.Enabled"),
                    Idioma.Texto("Biometric.Title"));
            }
            else if (!resultado.Cancelado)
            {
                await CaixaMensagem.MostrarAsync(this,
                    resultado.Mensagem ?? Idioma.Texto("Biometric.Unavailable"),
                    Idioma.Texto("Biometric.Title"),
                    TipoMensagem.Aviso);
            }
        }

        internal void AplicarBloqueioAutomatico(int minutos)
        {
            Preferencias.MinutosBloqueio = minutos;
            Preferencias.Salvar();
            _monitor.Ajustar(minutos);
            AtualizarEstadoConexao(_descricaoConexaoAtual, _falhaReconexaoAtual);
        }

        private async Task<bool> AvisarAntesDoBloqueioAsync(int segundos)
        {
            if (OwnedWindows.Count > 0)
                return true;

            Acessibilidade.Anunciar(this, Idioma.Formatar("Access.LockWarning", segundos), assertivo: true, forcar: true);
            return await CaixaMensagem.ConfirmarComTempoAsync(this,
                Idioma.Texto("Access.LockWarning"), Idioma.Texto("Settings.AutoLock"), segundos);
        }
    }
}
