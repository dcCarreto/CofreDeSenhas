using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GerenciadorDeSenhas.Modelos;

namespace GerenciadorDeSenhas.Servicos
{
    public class ServicoExportacao
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int IvSize = 12;
        private const int TagSize = 16;
        private const int Iteracoes = 200_000;

        private static readonly JsonSerializerOptions OpcoesJson = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public const int TamanhoMinimoSenha = 8;

        public async Task ExportarAsync(string caminhoArquivo, IReadOnlyCollection<SenhaExportada> itens, string senhaExportacao)
        {
            if (string.IsNullOrWhiteSpace(caminhoArquivo))
                throw new ArgumentException("Caminho do arquivo inválido.", nameof(caminhoArquivo));
            if (itens == null)
                throw new ArgumentNullException(nameof(itens));
            if (string.IsNullOrWhiteSpace(senhaExportacao) || senhaExportacao.Length < TamanhoMinimoSenha)
                throw new ArgumentException($"A senha de exportação deve ter pelo menos {TamanhoMinimoSenha} caracteres.");

            var json = JsonSerializer.Serialize(itens, OpcoesJson);
            var textoBytes = Encoding.UTF8.GetBytes(json);

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var iv = RandomNumberGenerator.GetBytes(IvSize);
            var chave = DerivarChave(senhaExportacao, salt);

            var cifrado = new byte[textoBytes.Length];
            var tag = new byte[TagSize];
            using (var aes = new AesGcm(chave, TagSize))
                aes.Encrypt(iv, textoBytes, cifrado, tag);

            var envelope = new EnvelopeExportacao
            {
                Versao = 1,
                Kdf = "PBKDF2-SHA256",
                Iteracoes = Iteracoes,
                Salt = Convert.ToBase64String(salt),
                Iv = Convert.ToBase64String(iv),
                Tag = Convert.ToBase64String(tag),
                Dados = Convert.ToBase64String(cifrado)
            };

            var conteudo = JsonSerializer.Serialize(envelope, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(caminhoArquivo, conteudo);
        }

        public async Task<List<SenhaExportada>> ImportarAsync(string caminhoArquivo, string senhaExportacao)
        {
            if (!File.Exists(caminhoArquivo))
                throw new InvalidOperationException("Arquivo de importação não encontrado.");
            if (string.IsNullOrWhiteSpace(senhaExportacao))
                throw new InvalidOperationException("Informe a senha de exportação.");

            EnvelopeExportacao? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<EnvelopeExportacao>(await File.ReadAllTextAsync(caminhoArquivo));
            }
            catch
            {
                throw new InvalidOperationException("Arquivo de importação inválido ou corrompido.");
            }

            if (envelope == null || envelope.Salt == null || envelope.Iv == null || envelope.Tag == null || envelope.Dados == null)
                throw new InvalidOperationException("Arquivo de importação inválido ou corrompido.");

            byte[] salt, iv, tag, cifrado;
            try
            {
                salt = Convert.FromBase64String(envelope.Salt);
                iv = Convert.FromBase64String(envelope.Iv);
                tag = Convert.FromBase64String(envelope.Tag);
                cifrado = Convert.FromBase64String(envelope.Dados);
            }
            catch
            {
                throw new InvalidOperationException("Arquivo de importação inválido ou corrompido.");
            }

            var chave = DerivarChave(senhaExportacao, salt, envelope.Iteracoes);
            var textoBytes = new byte[cifrado.Length];
            try
            {
                using var aes = new AesGcm(chave, TagSize);
                aes.Decrypt(iv, cifrado, tag, textoBytes);
            }
            catch (CryptographicException)
            {
                throw new InvalidOperationException("Senha de exportação incorreta ou arquivo corrompido.");
            }

            try
            {
                var json = Encoding.UTF8.GetString(textoBytes);
                return JsonSerializer.Deserialize<List<SenhaExportada>>(json) ?? new List<SenhaExportada>();
            }
            catch
            {
                throw new InvalidOperationException("Conteúdo do arquivo de importação inválido.");
            }
        }

        private static byte[] DerivarChave(string senha, byte[] salt, int iteracoes = Iteracoes) =>
            Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, KeySize);

        private sealed class EnvelopeExportacao
        {
            public int Versao { get; set; }
            public string? Kdf { get; set; }
            public int Iteracoes { get; set; }
            public string? Salt { get; set; }
            public string? Iv { get; set; }
            public string? Tag { get; set; }
            public string? Dados { get; set; }
        }
    }
}
