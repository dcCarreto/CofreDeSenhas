using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Servicos
{
    public class ServicoImportacaoCsv
    {
        private enum Campo { Nome, Usuario, Senha, Url, Notas, Totp, Categoria, Favorito }

        private static readonly Dictionary<string, Campo> Aliases = ConstruirAliases();

        public ResultadoImportacaoCsv ImportarArquivo(string caminhoArquivo)
        {
            if (string.IsNullOrWhiteSpace(caminhoArquivo) || !File.Exists(caminhoArquivo))
                throw new InvalidOperationException("Arquivo de importação não encontrado.");

            string conteudo;
            try
            {
                conteudo = File.ReadAllText(caminhoArquivo);
            }
            catch
            {
                throw new InvalidOperationException("Não foi possível ler o arquivo de importação.");
            }

            return Importar(conteudo);
        }

        public ResultadoImportacaoCsv Importar(string conteudo)
        {
            if (string.IsNullOrWhiteSpace(conteudo))
                throw new InvalidOperationException("O arquivo está vazio.");

            var delimitador = DetectarDelimitador(conteudo);
            var linhas = AnalisarCsv(conteudo, delimitador);
            linhas.RemoveAll(l => l.All(string.IsNullOrWhiteSpace));

            if (linhas.Count < 2)
                throw new InvalidOperationException("O arquivo não contém credenciais para importar.");

            var cabecalho = linhas[0];
            var colunas = MapearColunas(cabecalho);

            if (!colunas.ContainsKey(Campo.Senha))
                throw new InvalidOperationException(
                    "Não foi possível identificar a coluna de senha. Verifique se o arquivo tem uma linha de cabeçalho com os nomes das colunas.");

            var resultado = new ResultadoImportacaoCsv { FormatoDetectado = DetectarFormato(cabecalho) };

            for (int i = 1; i < linhas.Count; i++)
            {
                var valores = linhas[i];

                string Pegar(Campo campo)
                {
                    if (!colunas.TryGetValue(campo, out var indices))
                        return string.Empty;
                    foreach (var indice in indices)
                        if (indice < valores.Count && !string.IsNullOrWhiteSpace(valores[indice]))
                            return valores[indice].Trim();
                    return string.Empty;
                }

                var senha = Pegar(Campo.Senha);
                var nome = Pegar(Campo.Nome);
                var url = Pegar(Campo.Url);
                if (nome.Length == 0)
                    nome = NomeDeUrl(url);

                if (senha.Length == 0 || nome.Length == 0)
                {
                    resultado.LinhasIgnoradas++;
                    continue;
                }

                resultado.Itens.Add(new SenhaExportada
                {
                    NomeServico = nome,
                    Usuario = Pegar(Campo.Usuario),
                    Senha = senha,
                    Url = string.IsNullOrEmpty(url) ? null : url,
                    Notas = ValorOuNulo(Pegar(Campo.Notas)),
                    TotpSegredo = ValorOuNulo(Pegar(Campo.Totp)),
                    Categoria = MapearCategoria(Pegar(Campo.Categoria)),
                    Favorito = InterpretarFavorito(Pegar(Campo.Favorito)),
                    DataCriacao = DateTime.UtcNow,
                    DataAtualizacao = DateTime.UtcNow
                });
            }

            return resultado;
        }

        private static Dictionary<Campo, List<int>> MapearColunas(List<string> cabecalho)
        {
            var colunas = new Dictionary<Campo, List<int>>();
            for (int i = 0; i < cabecalho.Count; i++)
            {
                if (!Aliases.TryGetValue(Normalizar(cabecalho[i]), out var campo))
                    continue;
                if (!colunas.TryGetValue(campo, out var indices))
                    colunas[campo] = indices = new List<int>();
                indices.Add(i);
            }
            return colunas;
        }

        private static string DetectarFormato(List<string> cabecalho)
        {
            var h = new HashSet<string>(cabecalho.Select(Normalizar));

            if (h.Contains("login password") && h.Contains("login username")) return "Bitwarden";
            if (h.Contains("grouping") && h.Contains("fav")) return "LastPass";
            if (h.Contains("otpauth") && h.Contains("title")) return "1Password";
            if (h.Contains("httprealm") || h.Contains("formactionorigin") || h.Contains("timepasswordchanged")) return "Firefox";
            if (h.Contains("otpsecret")) return "Dashlane";
            if (h.Contains("cardholdername")) return "NordPass";
            if (h.Contains("group") && h.Contains("title") && h.Contains("password")) return "KeePass";
            if (h.Contains("name") && h.Contains("url") && h.Contains("username") && h.Contains("password")) return "Google Chrome / Edge";

            return "CSV genérico";
        }

        private static Dictionary<string, Campo> ConstruirAliases()
        {
            var mapa = new Dictionary<string, Campo>();

            void Registrar(Campo campo, params string[] nomes)
            {
                foreach (var nome in nomes)
                    mapa[nome] = campo;
            }

            Registrar(Campo.Nome, "name", "title", "account", "account name", "entry", "entry name",
                "item", "item name", "display name", "service", "servico", "serviço", "nome", "titulo", "título");
            Registrar(Campo.Usuario, "username", "user", "user name", "login", "login name", "login username",
                "email", "e mail", "usuario", "usuário", "userid", "user id");
            Registrar(Campo.Senha, "password", "pass", "pwd", "login password", "senha");
            Registrar(Campo.Url, "url", "uri", "website", "web site", "login uri", "link", "site",
                "web address", "address", "href", "urls");
            Registrar(Campo.Notas, "notes", "note", "comment", "comments", "extra", "notas",
                "observacoes", "observações", "description", "descricao", "descrição");
            Registrar(Campo.Totp, "totp", "otp", "otpauth", "otp auth", "otpsecret", "otp secret",
                "login totp", "2fa", "two factor", "authenticator key", "totpsecret", "totp secret", "otp url");
            Registrar(Campo.Categoria, "folder", "grouping", "group", "category", "categoria",
                "collection", "collections", "tags", "tag", "pasta", "grupo");
            Registrar(Campo.Favorito, "favorite", "favourite", "fav", "favorito", "star", "starred");

            return mapa;
        }

        private static Categoria MapearCategoria(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return Categoria.Other;

            var v = valor.Trim().ToLowerInvariant();
            if (v.Contains("work") || v.Contains("trabalho") || v.Contains("business") || v.Contains("job"))
                return Categoria.Work;
            if (v.Contains("financ") || v.Contains("bank") || v.Contains("banco") || v.Contains("pay") || v.Contains("cart"))
                return Categoria.Finance;
            if (v.Contains("social") || v.Contains("rede"))
                return Categoria.Social;
            if (v.Contains("person") || v.Contains("pessoal") || v.Contains("home") || v.Contains("casa"))
                return Categoria.Personal;

            return Categoria.Other;
        }

        private static bool InterpretarFavorito(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return false;

            return valor.Trim().ToLowerInvariant() switch
            {
                "1" or "true" or "yes" or "y" or "sim" or "x" or "favorite" or "favourite" or "star" or "starred" or "★" => true,
                _ => false
            };
        }

        private static string NomeDeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            var texto = url.Trim();
            int esquema = texto.IndexOf("://", StringComparison.Ordinal);
            if (esquema >= 0)
                texto = texto[(esquema + 3)..];

            int fim = texto.IndexOfAny(new[] { '/', '?', '#' });
            if (fim >= 0)
                texto = texto[..fim];

            if (texto.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                texto = texto[4..];

            int porta = texto.IndexOf(':');
            if (porta >= 0)
                texto = texto[..porta];

            return texto;
        }

        private static string? ValorOuNulo(string valor) =>
            string.IsNullOrEmpty(valor) ? null : valor;

        private static string Normalizar(string cabecalho)
        {
            if (string.IsNullOrEmpty(cabecalho))
                return string.Empty;

            var sb = new StringBuilder(cabecalho.Length);
            foreach (var c in cabecalho.Trim())
                sb.Append(c is '_' or '-' ? ' ' : char.ToLowerInvariant(c));

            var texto = sb.ToString();
            while (texto.Contains("  "))
                texto = texto.Replace("  ", " ");

            return texto.Trim();
        }

        private static int SaltarBom(string conteudo) =>
            conteudo.Length > 0 && conteudo[0] == '﻿' ? 1 : 0;

        private static char DetectarDelimitador(string conteudo)
        {
            int virgulas = 0, pontoVirgula = 0, tabulacoes = 0;
            bool emAspas = false;

            for (int i = SaltarBom(conteudo); i < conteudo.Length; i++)
            {
                char c = conteudo[i];
                if (c == '"')
                {
                    emAspas = !emAspas;
                    continue;
                }
                if (emAspas)
                    continue;
                if (c == '\n' || c == '\r')
                    break;
                if (c == ',') virgulas++;
                else if (c == ';') pontoVirgula++;
                else if (c == '\t') tabulacoes++;
            }

            if (pontoVirgula > virgulas && pontoVirgula >= tabulacoes) return ';';
            if (tabulacoes > virgulas && tabulacoes > pontoVirgula) return '\t';
            return ',';
        }

        private static List<List<string>> AnalisarCsv(string conteudo, char delimitador)
        {
            var linhas = new List<List<string>>();
            var linha = new List<string>();
            var campo = new StringBuilder();
            bool emAspas = false;

            for (int i = SaltarBom(conteudo); i < conteudo.Length; i++)
            {
                char c = conteudo[i];

                if (emAspas)
                {
                    if (c == '"')
                    {
                        if (i + 1 < conteudo.Length && conteudo[i + 1] == '"')
                        {
                            campo.Append('"');
                            i++;
                        }
                        else
                        {
                            emAspas = false;
                        }
                    }
                    else
                    {
                        campo.Append(c);
                    }
                    continue;
                }

                if (c == '"')
                {
                    emAspas = true;
                }
                else if (c == delimitador)
                {
                    linha.Add(campo.ToString());
                    campo.Clear();
                }
                else if (c == '\r' || c == '\n')
                {
                    linha.Add(campo.ToString());
                    campo.Clear();
                    linhas.Add(linha);
                    linha = new List<string>();
                    if (c == '\r' && i + 1 < conteudo.Length && conteudo[i + 1] == '\n')
                        i++;
                }
                else
                {
                    campo.Append(c);
                }
            }

            if (campo.Length > 0 || linha.Count > 0)
            {
                linha.Add(campo.ToString());
                linhas.Add(linha);
            }

            return linhas;
        }
    }
}
