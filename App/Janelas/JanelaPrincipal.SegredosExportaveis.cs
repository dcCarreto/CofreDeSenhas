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
        private string? ObterSenhaPlain(Senha s)
        {
            if (_criptografia == null)
                return null;

            if (_cachePlain.TryGetValue(s.Id, out var entrada) && entrada.Cifra == s.SenhaHash)
                return entrada.Plain;

            try
            {
                var plain = _criptografia.Descriptografar(s.SenhaHash);
                _cachePlain[s.Id] = (s.SenhaHash, plain, ForcaSenha.Calcular(plain));
                return plain;
            }
            catch
            {
                _cachePlain.Remove(s.Id);
                return null;
            }
        }

        private string? ObterTotpPlain(Senha s)
        {
            if (string.IsNullOrEmpty(s.TotpSegredo))
                return null;
            try { return _criptografia?.Descriptografar(s.TotpSegredo); }
            catch { return null; }
        }

        private Dictionary<string, string> ObterCamposExtrasPlain(Senha s)
        {
            var resultado = new Dictionary<string, string>();
            if (_criptografia == null)
                return resultado;

            foreach (var (chave, valorCifrado) in s.CamposExtras)
            {
                try { resultado[chave] = _criptografia.Descriptografar(valorCifrado); }
                catch (Exception ex)
                {
                    Diagnostico.Registrar(ex, "CamposExtras");
                    resultado[chave] = Idioma.Texto("Entry.FieldDecryptError");
                }
            }

            return resultado;
        }

        private List<HistoricoSenhaExportada> ObterHistoricoPlain(Senha s)
        {
            var historico = new List<HistoricoSenhaExportada>();
            if (_criptografia == null)
                return historico;

            foreach (var item in s.Historico)
            {
                try
                {
                    historico.Add(new HistoricoSenhaExportada
                    {
                        Senha = _criptografia.Descriptografar(item.SenhaHash),
                        DataAlteracao = item.DataAlteracao
                    });
                }
                catch
                {
                }
            }

            return historico;
        }

        // Decifra cada campo cifrado de s com a chave atual (_criptografia) e recifra
        // com criptografiaNova, preservando estrutura e Ids (diferente dos Obter*Plain
        // acima, que convertem para a forma exportada/plana usada na pasta de
        // sincronização e por isso descartam o Id de CodigoRecuperacao). Retorna null
        // se a própria senha estiver corrompida — melhor pular o item do que abortar a
        // republicação inteira por causa de um registro só.
        private Senha? RecifrarComNovaChave(Senha origem, ServicoCriptografia criptografiaNova)
        {
            if (_criptografia == null)
                return null;

            string senhaPlana;
            try { senhaPlana = _criptografia.Descriptografar(origem.SenhaHash); }
            catch { return null; }

            string? totpPlano = null;
            if (!string.IsNullOrEmpty(origem.TotpSegredo))
            {
                try { totpPlano = _criptografia.Descriptografar(origem.TotpSegredo); }
                catch { }
            }

            var camposExtras = new Dictionary<string, string>();
            foreach (var (chave, valorCifrado) in origem.CamposExtras)
            {
                try { camposExtras[chave] = criptografiaNova.Criptografar(_criptografia.Descriptografar(valorCifrado)); }
                catch { }
            }

            var historico = new List<HistoricoSenha>();
            foreach (var item in origem.Historico)
            {
                try
                {
                    historico.Add(new HistoricoSenha
                    {
                        SenhaHash = criptografiaNova.Criptografar(_criptografia.Descriptografar(item.SenhaHash)),
                        DataAlteracao = item.DataAlteracao
                    });
                }
                catch { }
            }

            var codigosRecuperacao = new List<CodigoRecuperacao>();
            foreach (var item in origem.CodigosRecuperacao)
            {
                try
                {
                    codigosRecuperacao.Add(new CodigoRecuperacao
                    {
                        Id = item.Id,
                        Codigo = criptografiaNova.Criptografar(_criptografia.Descriptografar(item.Codigo)),
                        Usado = item.Usado
                    });
                }
                catch { }
            }

            return new Senha
            {
                Id = origem.Id,
                NomeServico = origem.NomeServico,
                Usuario = origem.Usuario,
                SenhaHash = criptografiaNova.Criptografar(senhaPlana),
                Url = origem.Url,
                Categoria = origem.Categoria,
                Etiquetas = origem.Etiquetas.ToList(),
                Notas = origem.Notas,
                Tipo = origem.Tipo,
                CamposExtras = camposExtras,
                TotpSegredo = totpPlano == null ? null : criptografiaNova.Criptografar(totpPlano),
                Historico = historico,
                CodigosRecuperacao = codigosRecuperacao,
                Favorito = origem.Favorito,
                Fixado = origem.Fixado,
                NaLixeira = origem.NaLixeira,
                DataExclusao = origem.DataExclusao,
                DataCriacao = origem.DataCriacao,
                DataAtualizacao = origem.DataAtualizacao,
                DataUltimaCopiaSenha = origem.DataUltimaCopiaSenha,
                DataUltimaCopiaUsuario = origem.DataUltimaCopiaUsuario,
                DataUltimaCopiaTotp = origem.DataUltimaCopiaTotp
            };
        }

        private List<CodigoRecuperacaoExportado> ObterCodigosRecuperacaoPlain(Senha s)
        {
            var codigos = new List<CodigoRecuperacaoExportado>();
            if (_criptografia == null)
                return codigos;

            foreach (var item in s.CodigosRecuperacao)
            {
                try
                {
                    codigos.Add(new CodigoRecuperacaoExportado
                    {
                        Codigo = _criptografia.Descriptografar(item.Codigo),
                        Usado = item.Usado
                    });
                }
                catch
                {
                }
            }

            return codigos;
        }

        private async Task<List<AnexoExportado>> ObterAnexosExportadosAsync(Senha s)
        {
            var anexos = new List<AnexoExportado>();
            if (_servicoAnexos == null)
                return anexos;

            foreach (var item in s.Anexos)
            {
                try
                {
                    var bytes = await _servicoAnexos.LerAsync(item);
                    anexos.Add(new AnexoExportado
                    {
                        NomeArquivo = item.NomeArquivo,
                        ConteudoBase64 = Convert.ToBase64String(bytes)
                    });
                }
                catch
                {
                }
            }

            return anexos;
        }
    }
}
