using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace CofreDeSenhas.Janelas
{
    public partial class JanelaAjuda : Window
    {
        private static JanelaAjuda? _instancia;

        internal static JanelaAjuda? Instancia => _instancia;

        private readonly RenderizadorAjuda _renderizador = new();
        private readonly List<Button> _botoes = new();
        private string _topicoAtual = ManualAjuda.TopicoPadrao;

        public JanelaAjuda()
        {
            InitializeComponent();
            Icon = Recursos.IconeApp();
            Acessibilidade.Vincular(this);
            this.FecharComEsc();

            _renderizador.LinkClicado += Navegar;
            ConstruirTopicos();
            Navegar(_topicoAtual);

            Idioma.Alterado += AoTrocarIdioma;
            Acessibilidade.Alterado += AoTrocarTema;
            Closed += (s, e) =>
            {
                Idioma.Alterado -= AoTrocarIdioma;
                Acessibilidade.Alterado -= AoTrocarTema;
            };
            Opened += (s, e) => BtnFechar.Focus();
        }

        private void AoTrocarTema(object? sender, EventArgs e) => Navegar(_topicoAtual);

        public static void AbrirOuFocar(Window origem, string? topico = null)
        {
            var alvo = ManualAjuda.NormalizarId(topico);

            if (_instancia is { } janela)
            {
                janela.Navegar(alvo);
                janela.Activate();
                return;
            }

            _instancia = new JanelaAjuda();
            _instancia.Navegar(alvo);
            _instancia.Closed += (s, e) => _instancia = null;
            _instancia.Show(RaizJanela(origem));
        }

        public string TopicoAtual => _topicoAtual;

        public void Navegar(string? id)
        {
            var alvo = ManualAjuda.NormalizarId(id);
            _topicoAtual = alvo;

            TxtTitulo.Text = ManualAjuda.Titulo(alvo);
            AreaConteudo.Children.Clear();
            AreaConteudo.Children.Add(_renderizador.Render(SemTituloInicial(ManualAjuda.Conteudo(alvo))));
            Rolagem.Offset = default;

            for (int i = 0; i < _botoes.Count; i++)
                _botoes[i].Classes.Set("ativo", ManualAjuda.Topicos[i].Id == alvo);
        }

        private void ConstruirTopicos()
        {
            TrilhaTopicos.Children.Clear();
            _botoes.Clear();

            foreach (var (id, chave) in ManualAjuda.Topicos)
            {
                var botao = new Button
                {
                    Content = Idioma.Texto(chave),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                botao.Classes.Add("nav-item");
                AutomationProperties.SetName(botao, Idioma.Texto(chave));

                var alvo = id;
                botao.Click += (s, e) => Navegar(alvo);

                TrilhaTopicos.Children.Add(botao);
                _botoes.Add(botao);
            }
        }

        private void AoTrocarIdioma(object? sender, EventArgs e)
        {
            ConstruirTopicos();
            Navegar(_topicoAtual);
        }

        private static string SemTituloInicial(string markdown)
        {
            var texto = markdown.TrimStart('﻿', ' ', '\r', '\n');
            if (texto.StartsWith("# "))
            {
                var quebra = texto.IndexOf('\n');
                texto = quebra < 0 ? "" : texto[(quebra + 1)..];
            }
            return texto.TrimStart('\r', '\n');
        }

        private static Window RaizJanela(Window janela)
        {
            while (janela.Owner is Window pai)
                janela = pai;
            return janela;
        }

        private void Arrastar(object? sender, PointerPressedEventArgs e) => this.HabilitarArraste(e);

        private void Fechar_Click(object? sender, RoutedEventArgs e) => Close();
    }
}
