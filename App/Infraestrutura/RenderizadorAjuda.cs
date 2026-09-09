using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;

namespace CofreDeSenhas
{
    internal sealed class RenderizadorAjuda
    {
        private static readonly Regex TrechoInline = new(
            @"\*\*(?<b>[^*]+)\*\*|\*(?<i>[^*]+)\*|`(?<c>[^`]+)`|\[(?<lt>[^\]]+)\]\((?<lk>[^)]+)\)",
            RegexOptions.Compiled);

        private static readonly Regex ItemOrdenado = new(@"^(?<n>\d+)\.\s+(?<t>.*)$", RegexOptions.Compiled);

        private readonly Avalonia.Input.Cursor _cursorMao = new(Avalonia.Input.StandardCursorType.Hand);

        public event Action<string>? LinkClicado;

        public Control Render(string markdown)
        {
            var raiz = new StackPanel { Spacing = 10 };
            var paragrafo = new List<string>();
            var nota = new List<string>();
            var itens = new List<(string Marca, string Texto)>();

            void FecharParagrafo()
            {
                if (paragrafo.Count == 0) return;
                raiz.Children.Add(Paragrafo(string.Join(" ", paragrafo)));
                paragrafo.Clear();
            }

            void FecharNota()
            {
                if (nota.Count == 0) return;
                raiz.Children.Add(Nota(string.Join(" ", nota)));
                nota.Clear();
            }

            void FecharLista()
            {
                if (itens.Count == 0) return;
                raiz.Children.Add(Lista(itens));
                itens.Clear();
            }

            void FecharTudo()
            {
                FecharParagrafo();
                FecharNota();
                FecharLista();
            }

            foreach (var bruta in markdown.Replace("\r\n", "\n").Split('\n'))
            {
                var linha = bruta.Trim();

                if (linha.Length == 0) { FecharTudo(); continue; }

                if (linha.StartsWith("### ")) { FecharTudo(); raiz.Children.Add(Titulo(linha[4..], 3)); }
                else if (linha.StartsWith("## ")) { FecharTudo(); raiz.Children.Add(Titulo(linha[3..], 2)); }
                else if (linha.StartsWith("# ")) { FecharTudo(); raiz.Children.Add(Titulo(linha[2..], 1)); }
                else if (linha.StartsWith("> ")) { FecharParagrafo(); FecharLista(); nota.Add(linha[2..]); }
                else if (linha.StartsWith("- ") || linha.StartsWith("* "))
                {
                    FecharParagrafo();
                    FecharNota();
                    itens.Add(("•", linha[2..]));
                }
                else if (ItemOrdenado.Match(linha) is { Success: true } mo)
                {
                    FecharParagrafo();
                    FecharNota();
                    itens.Add(($"{mo.Groups["n"].Value}.", mo.Groups["t"].Value));
                }
                else if (itens.Count > 0)
                    itens[^1] = (itens[^1].Marca, itens[^1].Texto + " " + linha);
                else if (nota.Count > 0)
                    nota.Add(linha);
                else
                    paragrafo.Add(linha);
            }

            FecharTudo();
            return raiz;
        }

        private static TextBlock Titulo(string texto, int nivel)
        {
            var tb = new TextBlock
            {
                Text = texto,
                Foreground = Tema.Pincel(Tema.TextPrimary),
                TextWrapping = TextWrapping.Wrap
            };

            switch (nivel)
            {
                case 1:
                    tb.FontFamily = (FontFamily)Application.Current!.FindResource("FonteDisplay")!;
                    tb.FontSize = 20;
                    tb.FontWeight = FontWeight.Bold;
                    tb.Margin = new Thickness(0, 2, 0, 2);
                    break;
                case 2:
                    tb.FontSize = 15;
                    tb.FontWeight = FontWeight.SemiBold;
                    tb.Margin = new Thickness(0, 12, 0, 0);
                    break;
                default:
                    tb.FontSize = 13;
                    tb.FontWeight = FontWeight.SemiBold;
                    tb.Margin = new Thickness(0, 6, 0, 0);
                    break;
            }

            return tb;
        }

        private TextBlock Paragrafo(string texto)
        {
            var tb = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                LineHeight = 20,
                Foreground = Tema.Pincel(Tema.TextSecondary)
            };
            PreencherInlines(tb, texto);
            return tb;
        }

        private Control Lista(List<(string Marca, string Texto)> itens)
        {
            var painel = new StackPanel { Spacing = 6, Margin = new Thickness(2, 2, 0, 2) };

            foreach (var (marca, texto) in itens)
            {
                var grade = new Grid { ColumnDefinitions = new ColumnDefinitions("22,*") };
                grade.Children.Add(new TextBlock
                {
                    Text = marca,
                    FontSize = 13,
                    Foreground = Tema.Pincel(Tema.TextTertiary),
                    VerticalAlignment = VerticalAlignment.Top
                });

                var corpo = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13,
                    LineHeight = 20,
                    Foreground = Tema.Pincel(Tema.TextSecondary)
                };
                PreencherInlines(corpo, texto);
                Grid.SetColumn(corpo, 1);
                grade.Children.Add(corpo);

                painel.Children.Add(grade);
            }

            return painel;
        }

        private Control Nota(string texto)
        {
            var corpo = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12.5,
                LineHeight = 19,
                Foreground = Tema.Pincel(Tema.AccentText)
            };
            PreencherInlines(corpo, texto);

            return new Border
            {
                Background = Tema.Pincel(Tema.AccentLight),
                BorderBrush = Tema.Pincel(Tema.AccentPrimary),
                BorderThickness = new Thickness(3, 0, 0, 0),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(14, 10),
                Margin = new Thickness(0, 2, 0, 2),
                Child = corpo
            };
        }

        private void PreencherInlines(TextBlock alvo, string texto)
        {
            var destino = alvo.Inlines ??= new InlineCollection();
            var links = new List<(int Inicio, int Fim, string Chave)>();
            int origem = 0;
            int montado = 0;

            void Acrescentar(string parte, Inline inline)
            {
                destino.Add(inline);
                montado += parte.Length;
            }

            foreach (Match m in TrechoInline.Matches(texto))
            {
                if (m.Index > origem)
                {
                    var plano = texto[origem..m.Index];
                    Acrescentar(plano, new Run(plano));
                }

                if (m.Groups["b"].Success)
                {
                    var v = m.Groups["b"].Value;
                    Acrescentar(v, new Run(v) { FontWeight = FontWeight.SemiBold });
                }
                else if (m.Groups["i"].Success)
                {
                    var v = m.Groups["i"].Value;
                    Acrescentar(v, new Run(v) { FontStyle = FontStyle.Italic });
                }
                else if (m.Groups["c"].Success)
                {
                    var v = m.Groups["c"].Value;
                    Acrescentar(v, new Run(v)
                    {
                        FontFamily = (FontFamily)Application.Current!.FindResource("FonteMono")!,
                        Foreground = Tema.Pincel(Tema.TextPrimary)
                    });
                }
                else
                {
                    var v = m.Groups["lt"].Value;
                    links.Add((montado, montado + v.Length, m.Groups["lk"].Value));
                    Acrescentar(v, new Run(v)
                    {
                        Foreground = Tema.Pincel(Tema.AccentText),
                        TextDecorations = TextDecorations.Underline
                    });
                }

                origem = m.Index + m.Length;
            }

            if (origem < texto.Length)
            {
                var resto = texto[origem..];
                Acrescentar(resto, new Run(resto));
            }

            if (links.Count > 0)
                AtivarLinks(alvo, links);
        }

        private void AtivarLinks(TextBlock alvo, List<(int Inicio, int Fim, string Chave)> links)
        {
            string? SobrePosicao(Avalonia.Point ponto)
            {
                var hit = alvo.TextLayout.HitTestPoint(ponto);
                foreach (var (inicio, fim, chave) in links)
                    if (hit.TextPosition >= inicio && hit.TextPosition < fim)
                        return chave;
                return null;
            }

            alvo.PointerMoved += (s, e) =>
                alvo.Cursor = SobrePosicao(e.GetPosition(alvo)) != null ? _cursorMao : Avalonia.Input.Cursor.Default;

            alvo.PointerPressed += (s, e) =>
            {
                if (SobrePosicao(e.GetPosition(alvo)) is { } chave)
                {
                    e.Handled = true;
                    LinkClicado?.Invoke(chave);
                }
            };
        }
    }
}
