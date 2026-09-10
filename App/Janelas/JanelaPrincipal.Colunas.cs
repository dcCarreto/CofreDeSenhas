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
        private void RedimensionarColuna_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border divisor || divisor.Tag is not string tag)
                return;

            var coluna = Enum.Parse<ColunaTabela>(tag);
            var colunaDireita = ObterColunaDireita(coluna);
            if (colunaDireita == null)
                return;

            _colunaEmRedimensionamento = coluna;
            _colunaDireitaEmRedimensionamento = colunaDireita;
            _inicioRedimensionamentoX = e.GetPosition(GridCabecalhoTabela).X;
            _larguraInicialRedimensionamento = ObterLarguraColuna(coluna);
            _larguraDireitaInicialRedimensionamento = ObterLarguraColuna(colunaDireita.Value);
            e.Pointer.Capture(divisor);
            e.Handled = true;
        }

        private void RedimensionarColuna_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (_colunaEmRedimensionamento == null || _colunaDireitaEmRedimensionamento == null)
                return;

            var colunaEsquerda = _colunaEmRedimensionamento.Value;
            var colunaDireita = _colunaDireitaEmRedimensionamento.Value;

            var delta = e.GetPosition(GridCabecalhoTabela).X - _inicioRedimensionamentoX;
            var minimoEsquerda = ObterLarguraMinimaColuna(colunaEsquerda);
            var minimoDireita = ObterLarguraMinimaColuna(colunaDireita);
            var deltaMinimo = minimoEsquerda - _larguraInicialRedimensionamento;
            var deltaMaximo = _larguraDireitaInicialRedimensionamento - minimoDireita;

            delta = Math.Clamp(delta, deltaMinimo, deltaMaximo);

            DefinirLarguraColuna(colunaEsquerda, _larguraInicialRedimensionamento + delta);
            DefinirLarguraColuna(colunaDireita, _larguraDireitaInicialRedimensionamento - delta);
            AplicarLargurasColunas();
            e.Handled = true;
        }

        private void RedimensionarColuna_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _colunaEmRedimensionamento = null;
            _colunaDireitaEmRedimensionamento = null;
            e.Pointer.Capture(null);
            e.Handled = true;
        }

        private void AjustarLargurasIniciais()
        {
            if (_largurasIniciaisAplicadas || GridCabecalhoTabela.Bounds.Width <= 0)
                return;

            _largurasIniciaisAplicadas = true;

            double larguraDisponivel = GridCabecalhoTabela.Bounds.Width;
            double fixo = 42 + 24 + 6 + 6 + 6 + 6;

            _larguraAcoes = Math.Clamp(larguraDisponivel * 0.17, LarguraMinimaAcoes, 220);
            _larguraCategoria = Math.Clamp(larguraDisponivel * 0.13, LarguraMinimaCategoria, 132);
            _larguraData = Math.Clamp(larguraDisponivel * 0.11, LarguraMinimaData, 118);

            double flexivel = Math.Max(
                LarguraMinimaServico + LarguraMinimaUsuario,
                larguraDisponivel - fixo - _larguraCategoria - _larguraData - _larguraAcoes);

            _larguraServico = Math.Clamp(flexivel * 0.44, LarguraMinimaServico, 260);
            _larguraUsuario = Math.Max(LarguraMinimaUsuario, flexivel - _larguraServico);

            _larguraTabelaAnterior = larguraDisponivel;
            AplicarLargurasColunas();
        }

        private void GridCabecalhoTabela_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (!_largurasIniciaisAplicadas)
            {
                AjustarLargurasIniciais();
                return;
            }

            if (_larguraTabelaAnterior <= 0)
                return;

            var larguraAtual = GridCabecalhoTabela.Bounds.Width;
            var delta = larguraAtual - _larguraTabelaAnterior;
            _larguraTabelaAnterior = larguraAtual;
            if (delta == 0)
                return;

            var larguraServicoAlvo = _larguraServico + delta;
            var larguraServicoAplicada = Math.Max(LarguraMinimaServico, larguraServicoAlvo);
            var sobra = larguraServicoAlvo - larguraServicoAplicada;

            DefinirLarguraColuna(ColunaTabela.Servico, larguraServicoAplicada);
            if (sobra != 0)
                DefinirLarguraColuna(ColunaTabela.Usuario, _larguraUsuario + sobra);

            AplicarLargurasColunas();
        }

        private double ObterLarguraColuna(ColunaTabela coluna) => coluna switch
        {
            ColunaTabela.Servico => _larguraServico,
            ColunaTabela.Usuario => _larguraUsuario,
            ColunaTabela.Categoria => _larguraCategoria,
            ColunaTabela.Data => _larguraData,
            ColunaTabela.Acoes => _larguraAcoes,
            _ => 0
        };

        private static ColunaTabela? ObterColunaDireita(ColunaTabela coluna) => coluna switch
        {
            ColunaTabela.Servico => ColunaTabela.Usuario,
            ColunaTabela.Usuario => ColunaTabela.Categoria,
            ColunaTabela.Categoria => ColunaTabela.Data,
            ColunaTabela.Data => ColunaTabela.Acoes,
            _ => null
        };

        private static double ObterLarguraMinimaColuna(ColunaTabela coluna) => coluna switch
        {
            ColunaTabela.Servico => LarguraMinimaServico,
            ColunaTabela.Usuario => LarguraMinimaUsuario,
            ColunaTabela.Categoria => LarguraMinimaCategoria,
            ColunaTabela.Data => LarguraMinimaData,
            ColunaTabela.Acoes => LarguraMinimaAcoes,
            _ => 0
        };

        private void DefinirLarguraColuna(ColunaTabela coluna, double largura)
        {
            switch (coluna)
            {
                case ColunaTabela.Servico:
                    _larguraServico = Math.Max(LarguraMinimaServico, largura);
                    break;
                case ColunaTabela.Usuario:
                    _larguraUsuario = Math.Max(LarguraMinimaUsuario, largura);
                    break;
                case ColunaTabela.Categoria:
                    _larguraCategoria = Math.Max(LarguraMinimaCategoria, largura);
                    break;
                case ColunaTabela.Data:
                    _larguraData = Math.Max(LarguraMinimaData, largura);
                    break;
                case ColunaTabela.Acoes:
                    _larguraAcoes = Math.Max(LarguraMinimaAcoes, largura);
                    break;
            }
        }

        private (double Servico, double Usuario, double Categoria, double Forca) LargurasEfetivas()
        {
            var colunas = Acessibilidade.ColunasLista;
            double usuario = colunas.HasFlag(ColunasLista.Usuario) ? _larguraUsuario : 0;
            double categoria = colunas.HasFlag(ColunasLista.Categoria) ? _larguraCategoria : 0;
            double forca = colunas.HasFlag(ColunasLista.Forca) ? _larguraData : 0;
            double servico = _larguraServico
                + (_larguraUsuario - usuario) + (_larguraCategoria - categoria) + (_larguraData - forca);
            return (servico, usuario, categoria, forca);
        }

        private void AplicarLargurasColunas()
        {
            if (GridCabecalhoTabela == null)
                return;

            var colunas = Acessibilidade.ColunasLista;
            bool usuarioVisivel = colunas.HasFlag(ColunasLista.Usuario);
            bool categoriaVisivel = colunas.HasFlag(ColunasLista.Categoria);
            bool forcaVisivel = colunas.HasFlag(ColunasLista.Forca);

            var (servico, usuario, categoria, forca) = LargurasEfetivas();

            GridCabecalhoTabela.ColumnDefinitions[1].Width = new GridLength(servico);
            GridCabecalhoTabela.ColumnDefinitions[3].Width = new GridLength(usuario);
            GridCabecalhoTabela.ColumnDefinitions[4].Width = new GridLength(usuarioVisivel ? 6 : 0);
            GridCabecalhoTabela.ColumnDefinitions[5].Width = new GridLength(categoria);
            GridCabecalhoTabela.ColumnDefinitions[6].Width = new GridLength(categoriaVisivel ? 6 : 0);
            GridCabecalhoTabela.ColumnDefinitions[7].Width = new GridLength(forca);
            GridCabecalhoTabela.ColumnDefinitions[8].Width = new GridLength(forcaVisivel ? 6 : 0);
            GridCabecalhoTabela.ColumnDefinitions[9].Width = new GridLength(_larguraAcoes);

            CabUsuario.IsVisible = DivUsuario.IsVisible = usuarioVisivel;
            CabCategoria.IsVisible = DivCategoria.IsVisible = categoriaVisivel;
            CabForca.IsVisible = DivForca.IsVisible = forcaVisivel;

            foreach (var linha in _linhasSenha)
                linha.DefinirLargurasColunas(servico, usuario, categoria, forca, _larguraAcoes);
        }
    }
}
