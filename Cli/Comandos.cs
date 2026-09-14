using GerenciadorDeSenhas.Modelos;
using GerenciadorDeSenhas.Servicos;

namespace CofreDeSenhas.Cli
{
    internal static class Comandos
    {
        // ---------- gerar ----------

        public static int Gerar(Argumentos a)
        {
            var servico = new ServicoGeracaoSenha();
            var quantidade = a.ValorInt(1, "--quantidade", "-n");
            List<string> resultados;

            if (a.Tem("--frase", "--passphrase"))
            {
                resultados = servico.GerarFrasesSenha(
                    quantidade,
                    a.ValorInt(5, "--palavras", "-p"),
                    a.Valor("--separador", "-s") ?? "-",
                    capitalizar: a.Tem("--capitalizar"),
                    incluirNumero: !a.Tem("--sem-numero"));
            }
            else
            {
                resultados = servico.GerarSenhas(
                    quantidade,
                    a.ValorInt(20, "--comprimento", "-c"),
                    incluirMaiusculas: !a.Tem("--sem-maiusculas"),
                    incluirMinusculas: !a.Tem("--sem-minusculas"),
                    incluirNumeros: !a.Tem("--sem-numeros"),
                    incluirEspeciais: !a.Tem("--sem-simbolos"));
            }

            if (a.Tem("--copiar"))
            {
                if (resultados.Count != 1)
                {
                    Console.Error.WriteLine("--copiar só funciona ao gerar uma única senha (use sem --quantidade).");
                    return 1;
                }
                return CopiarComLimpeza(resultados[0], a, "Senha gerada");
            }

            foreach (var r in resultados)
                Console.WriteLine(r);
            return 0;
        }

        // ---------- listar / buscar ----------

        public static async Task<int> ListarAsync(Argumentos a)
        {
            using var cofre = Cofre.Abrir(out var codigo);
            if (cofre == null) return codigo;

            var itens = await cofre.ListarAsync();
            if (!AplicarFiltros(a, ref itens, out var erro))
            {
                Console.Error.WriteLine(erro);
                return 1;
            }

            Saida.Tabela(itens);
            return 0;
        }

        public static async Task<int> BuscarAsync(Argumentos a)
        {
            var termo = a.TermoUnido();
            if (string.IsNullOrWhiteSpace(termo))
            {
                Console.Error.WriteLine("Uso: cofre buscar <termo>");
                return 1;
            }

            using var cofre = Cofre.Abrir(out var codigo);
            if (cofre == null) return codigo;

            var itens = (await cofre.ListarAsync()).Where(s => Casa(s, termo)).ToList();
            if (!AplicarFiltros(a, ref itens, out var erro))
            {
                Console.Error.WriteLine(erro);
                return 1;
            }

            Saida.Tabela(itens);
            return 0;
        }

        private static bool Casa(Senha s, string termo) =>
            Contem(s.NomeServico, termo) || Contem(s.Usuario, termo) || Contem(s.Url, termo) ||
            Contem(Formatar.CategoriaLabel(s.Categoria), termo) ||
            s.Etiquetas.Any(t => Contem(t, termo));

        private static bool Contem(string? alvo, string termo) =>
            alvo != null && alvo.Contains(termo, StringComparison.OrdinalIgnoreCase);

        private static bool AplicarFiltros(Argumentos a, ref List<Senha> itens, out string erro)
        {
            erro = string.Empty;

            if (a.Tem("--favoritas", "--favoritos"))
                itens = itens.Where(s => s.Favorito).ToList();

            if (a.Valor("--categoria", "-C") is { } catBruta)
            {
                if (!Formatar.TentarCategoria(catBruta, out var cat))
                {
                    erro = $"Categoria inválida: {catBruta}. Use trabalho, pessoal, financas, social ou outro.";
                    return false;
                }
                itens = itens.Where(s => s.Categoria == cat).ToList();
            }

            if (a.Valor("--etiqueta", "-e") is { } etq)
                itens = itens.Where(s => s.Etiquetas.Any(t => Contem(t, etq))).ToList();

            return true;
        }

        // ---------- mostrar / copiar ----------

        public static async Task<int> MostrarAsync(Argumentos a)
        {
            var (cofre, codigo, resultado) = await Selecionar(a, "mostrar");
            if (cofre == null) return codigo;
            using (cofre)
            {
                if (resultado.Tipo == TipoSelecao.Encontrada)
                {
                    Saida.Detalhe(resultado.Unica!);
                    return 0;
                }
                return Reportar(resultado, a);
            }
        }

        public static async Task<int> CopiarAsync(Argumentos a)
        {
            var (cofre, codigo, resultado) = await Selecionar(a, "copiar");
            if (cofre == null) return codigo;
            using (cofre)
            {
                if (resultado.Tipo != TipoSelecao.Encontrada)
                    return Reportar(resultado, a);

                var senha = resultado.Unica!;
                var plano = cofre.Criptografia.Descriptografar(senha.SenhaHash);
                return CopiarComLimpeza(plano, a, $"Senha de {senha.NomeServico}");
            }
        }

        private static async Task<(Cofre? Cofre, int Codigo, ResultadoSelecao Resultado)> Selecionar(Argumentos a, string comando)
        {
            var termo = a.Posicionais.Count > 0 ? a.Posicionais[0] : string.Empty;
            if (string.IsNullOrWhiteSpace(termo))
            {
                Console.Error.WriteLine($"Uso: cofre {comando} <serviço> [--usuario <usuário>]");
                return (null, 1, default);
            }

            var cofre = Cofre.Abrir(out var codigo);
            if (cofre == null)
                return (null, codigo, default);

            var todas = await cofre.ListarAsync();
            var resultado = SeletorCredencial.Selecionar(todas, termo, a.Valor("--usuario", "-u"));
            return (cofre, 0, resultado);
        }

        private static int Reportar(ResultadoSelecao r, Argumentos a)
        {
            if (r.Tipo == TipoSelecao.Ambigua)
            {
                Saida.ListaCurta(r.Candidatas);
                return 2;
            }
            Console.Error.WriteLine($"Nada encontrado para \"{(a.Posicionais.Count > 0 ? a.Posicionais[0] : "")}\".");
            return 1;
        }

        // ---------- clipboard com limpeza ----------

        private static int CopiarComLimpeza(string valor, Argumentos a, string oQueFoi)
        {
            AreaTransferencia.Copiar(valor);

            if (a.Tem("--nao-limpar", "--no-clear"))
            {
                Console.WriteLine($"{oQueFoi} copiado para a área de transferência. NÃO será apagado automaticamente.");
                return 0;
            }

            var segundos = Math.Clamp(a.ValorInt(30, "--limpar-apos", "--clear-after"), 1, 3600);
            Console.WriteLine($"{oQueFoi} copiado. Vou apagar da área de transferência em {segundos}s (Ctrl+C apaga agora).");

            // Ctrl+C só sinaliza o evento e deixa a execução seguir o fluxo normal —
            // assim o `using` do chamador ainda roda e a chave mestra é zerada.
            using var interrompido = new ManualResetEventSlim(false);
            void AoCancelar(object? _, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                interrompido.Set();
            }

            Console.CancelKeyPress += AoCancelar;
            try
            {
                interrompido.Wait(TimeSpan.FromSeconds(segundos));
            }
            finally
            {
                Console.CancelKeyPress -= AoCancelar;
                try
                {
                    AreaTransferencia.Limpar();
                    Console.WriteLine("Área de transferência limpa.");
                }
                catch { }
            }
            return 0;
        }
    }
}
