using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using GerenciadorDeSenhas.Excecoes;
using GerenciadorDeSenhas.Modelos;
using Konscious.Security.Cryptography;

namespace GerenciadorDeSenhas.Servicos
{
    public class ServicoImportacaoKdbx
    {
        private const uint Assinatura1 = 0x9AA2D903;
        private const uint Assinatura2 = 0xB54BFB67;

        private static readonly byte[] CifraAes = Convert.FromHexString("31C1F2E6BF714350BE5805216AFC5AFF");
        private static readonly byte[] CifraChaCha20 = Convert.FromHexString("D6038A2B8B6F4CB5A524339A31DBB59A");
        private static readonly byte[] KdfArgon2d = Convert.FromHexString("EF636DDF8C29444B91F7A9A403E30A0C");
        private static readonly byte[] KdfArgon2id = Convert.FromHexString("9E298B1956DB4773B23DFC3EC6F0A1E6");
        private static readonly byte[] KdfAes = Convert.FromHexString("C9D9F39A628A4460BF740D08C18A4FEA");

        private const ulong MemoriaArgon2Maxima = 1UL << 30;
        private const ulong IteracoesArgon2Maximas = 10_000;
        private const uint ParalelismoMaximo = 64;
        private const ulong RodadasAesKdfMaximas = 500_000_000;

        private static readonly HashSet<string> CamposPadrao = new(StringComparer.Ordinal)
        {
            "Title", "UserName", "Password", "URL", "Notes", "otp"
        };

        public List<SenhaExportada> ImportarArquivo(string caminho, string senha) =>
            Importar(File.ReadAllBytes(caminho), senha);

        public List<SenhaExportada> Importar(byte[] arquivo, string senha)
        {
            var leitor = new Leitor(arquivo);
            if (arquivo.Length < 12 || leitor.U32() != Assinatura1 || leitor.U32() != Assinatura2)
                throw new ErroLocalizavel("Kdbx.Error.NotKdbx");

            leitor.U16();
            var versaoMaior = leitor.U16();
            if (versaoMaior == 3)
                throw new ErroLocalizavel("Kdbx.Error.Version3");
            if (versaoMaior != 4)
                throw new ErroLocalizavel("Kdbx.Error.NotKdbx");

            byte[]? cifra = null, semente = null, iv = null;
            Dictionary<string, byte[]>? kdf = null;
            var compactado = false;
            while (true)
            {
                var tipo = leitor.Byte();
                var dado = leitor.Bytes(leitor.I32());
                if (tipo == 0) break;
                switch (tipo)
                {
                    case 2: cifra = dado; break;
                    case 3: compactado = BinaryPrimitives.ReadUInt32LittleEndian(dado) == 1; break;
                    case 4: semente = dado; break;
                    case 7: iv = dado; break;
                    case 11: kdf = LerDicionarioVariante(dado); break;
                }
            }

            if (cifra == null || semente == null || iv == null || kdf == null)
                throw new ErroLocalizavel("Kdbx.Error.Corrupted");

            var cabecalho = arquivo.AsSpan(0, leitor.Posicao).ToArray();
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(cabecalho), leitor.Bytes(32)))
                throw new ErroLocalizavel("Kdbx.Error.Corrupted");
            var hmacCabecalho = leitor.Bytes(32);

            var chaveComposta = SHA256.HashData(SHA256.HashData(Encoding.UTF8.GetBytes(senha)));
            var transformada = DerivarChave(chaveComposta, kdf);
            var chaveMestra = SHA256.HashData(Concatenar(semente, transformada));
            var baseHmac = SHA512.HashData(Concatenar(semente, transformada, new byte[] { 1 }));

            if (!CryptographicOperations.FixedTimeEquals(HMACSHA256.HashData(ChaveDoBloco(ulong.MaxValue, baseHmac), cabecalho), hmacCabecalho))
                throw new ErroLocalizavel("Kdbx.Error.WrongPassword");

            var cifrado = new MemoryStream();
            for (ulong indice = 0; ; indice++)
            {
                var hmac = leitor.Bytes(32);
                var tamanho = leitor.I32();
                var dados = leitor.Bytes(tamanho);
                var assinado = new byte[12 + tamanho];
                BinaryPrimitives.WriteUInt64LittleEndian(assinado, indice);
                BinaryPrimitives.WriteInt32LittleEndian(assinado.AsSpan(8), tamanho);
                dados.CopyTo(assinado, 12);
                if (!CryptographicOperations.FixedTimeEquals(HMACSHA256.HashData(ChaveDoBloco(indice, baseHmac), assinado), hmac))
                    throw new ErroLocalizavel("Kdbx.Error.Corrupted");
                if (tamanho == 0) break;
                cifrado.Write(dados);
            }

            var conteudo = Decifrar(cifra, chaveMestra, iv, cifrado.ToArray());
            if (compactado)
            {
                using var gzip = new GZipStream(new MemoryStream(conteudo), CompressionMode.Decompress);
                using var saida = new MemoryStream();
                gzip.CopyTo(saida);
                conteudo = saida.ToArray();
            }

            var interno = new Leitor(conteudo);
            var anexos = new List<byte[]>();
            uint fluxoInterno = 0;
            byte[]? chaveInterna = null;
            while (true)
            {
                var tipo = interno.Byte();
                var dado = interno.Bytes(interno.I32());
                if (tipo == 0) break;
                switch (tipo)
                {
                    case 1: fluxoInterno = BinaryPrimitives.ReadUInt32LittleEndian(dado); break;
                    case 2: chaveInterna = dado; break;
                    case 3: anexos.Add(dado.AsSpan(1).ToArray()); break;
                }
            }

            if (fluxoInterno != 3 || chaveInterna == null)
                throw new ErroLocalizavel("Kdbx.Error.Unsupported");

            var hashInterno = SHA512.HashData(chaveInterna);
            var protecao = new ChaCha20(hashInterno.AsSpan(0, 32), hashInterno.AsSpan(32, 12));

            var xml = XDocument.Parse(Encoding.UTF8.GetString(conteudo, interno.Posicao, conteudo.Length - interno.Posicao));
            foreach (var valor in xml.Descendants("Value").Where(v => (string?)v.Attribute("Protected") == "True"))
            {
                var bytes = Convert.FromBase64String(valor.Value);
                protecao.Aplicar(bytes);
                valor.Value = Encoding.UTF8.GetString(bytes);
            }

            var lixeira = xml.Root?.Element("Meta")?.Element("RecycleBinUUID")?.Value;
            var raiz = xml.Root?.Element("Root")?.Element("Group") ?? throw new ErroLocalizavel("Kdbx.Error.Corrupted");

            var itens = new List<SenhaExportada>();
            LerGrupo(raiz, null, lixeira, anexos, itens);
            return itens;
        }

        private static void LerGrupo(XElement grupo, string? nomeGrupo, string? lixeira, List<byte[]> anexos, List<SenhaExportada> itens)
        {
            if (!string.IsNullOrEmpty(lixeira) && grupo.Element("UUID")?.Value == lixeira)
                return;

            foreach (var entrada in grupo.Elements("Entry"))
                if (LerEntrada(entrada, nomeGrupo, anexos) is { } item)
                    itens.Add(item);

            foreach (var filho in grupo.Elements("Group"))
                LerGrupo(filho, filho.Element("Name")?.Value, lixeira, anexos, itens);
        }

        private static SenhaExportada? LerEntrada(XElement entrada, string? nomeGrupo, List<byte[]> anexos)
        {
            var campos = Campos(entrada);
            var titulo = campos.GetValueOrDefault("Title");
            var url = campos.GetValueOrDefault("URL");
            var nome = !string.IsNullOrWhiteSpace(titulo) ? titulo : url;
            if (string.IsNullOrWhiteSpace(nome))
                return null;

            var notas = campos.GetValueOrDefault("Notes") ?? "";
            var extras = campos.Where(c => !CamposPadrao.Contains(c.Key) && !c.Key.StartsWith("TimeOtp-", StringComparison.Ordinal)
                    && !string.IsNullOrEmpty(c.Value))
                .Select(c => $"{c.Key}: {c.Value}")
                .ToList();
            if (extras.Count > 0)
                notas = string.Join("\n", new[] { notas }.Where(n => n.Length > 0).Concat(extras));

            var etiquetas = (entrada.Element("Tags")?.Value ?? "")
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            if (!string.IsNullOrWhiteSpace(nomeGrupo) && !etiquetas.Contains(nomeGrupo))
                etiquetas.Add(nomeGrupo);

            var senhaAtual = campos.GetValueOrDefault("Password") ?? "";
            var historico = (entrada.Element("History")?.Elements("Entry") ?? Enumerable.Empty<XElement>())
                .Select(h => new HistoricoSenhaExportada
                {
                    Senha = Campos(h).GetValueOrDefault("Password") ?? "",
                    DataAlteracao = Data(h, "LastModificationTime")
                })
                .Where(h => h.Senha.Length > 0 && h.Senha != senhaAtual)
                .ToList();

            var anexosDaEntrada = new List<AnexoExportado>();
            foreach (var binario in entrada.Elements("Binary"))
            {
                var referencia = binario.Element("Value")?.Attribute("Ref")?.Value;
                if (int.TryParse(referencia, out var indice) && indice >= 0 && indice < anexos.Count)
                    anexosDaEntrada.Add(new AnexoExportado
                    {
                        NomeArquivo = binario.Element("Key")?.Value ?? $"anexo-{indice}",
                        ConteudoBase64 = Convert.ToBase64String(anexos[indice]),
                        TamanhoBytes = anexos[indice].Length
                    });
            }

            return new SenhaExportada
            {
                NomeServico = nome,
                Usuario = campos.GetValueOrDefault("UserName") ?? "",
                Senha = senhaAtual,
                Url = string.IsNullOrWhiteSpace(url) ? null : url,
                Notas = notas.Length > 0 ? notas : null,
                Categoria = ServicoImportacaoCsv.MapearCategoria(nomeGrupo ?? ""),
                Etiquetas = etiquetas,
                TotpSegredo = Totp(campos),
                Historico = historico,
                Anexos = anexosDaEntrada,
                DataCriacao = Data(entrada, "CreationTime"),
                DataAtualizacao = Data(entrada, "LastModificationTime")
            };
        }

        private static Dictionary<string, string> Campos(XElement entrada)
        {
            var campos = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var s in entrada.Elements("String"))
                if (s.Element("Key")?.Value is { } chave)
                    campos[chave] = s.Element("Value")?.Value ?? "";
            return campos;
        }

        private static string? Totp(Dictionary<string, string> campos)
        {
            if (campos.TryGetValue("otp", out var otp) && !string.IsNullOrWhiteSpace(otp))
                return otp.Trim();
            if (campos.TryGetValue("TimeOtp-Secret-Base32", out var base32) && !string.IsNullOrWhiteSpace(base32))
                return base32.Trim();
            return null;
        }

        private static DateTime Data(XElement entrada, string campo)
        {
            var texto = entrada.Element("Times")?.Element(campo)?.Value;
            if (string.IsNullOrEmpty(texto))
                return DateTime.UtcNow;
            if (DateTime.TryParse(texto, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var iso))
                return iso;
            try
            {
                var segundos = BinaryPrimitives.ReadInt64LittleEndian(Convert.FromBase64String(texto));
                return DateTime.SpecifyKind(DateTime.MinValue.AddSeconds(segundos), DateTimeKind.Utc);
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                return DateTime.UtcNow;
            }
        }

        private static byte[] DerivarChave(byte[] chaveComposta, Dictionary<string, byte[]> kdf)
        {
            var uuid = kdf.GetValueOrDefault("$UUID") ?? throw new ErroLocalizavel("Kdbx.Error.Corrupted");

            if (uuid.AsSpan().SequenceEqual(KdfAes))
            {
                var rodadas = BinaryPrimitives.ReadUInt64LittleEndian(kdf["R"]);
                if (rodadas > RodadasAesKdfMaximas)
                    throw new ErroLocalizavel("Kdbx.Error.Unsupported");
                using var aes = Aes.Create();
                aes.Key = kdf["S"];
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                using var cifrador = aes.CreateEncryptor();
                var atual = (byte[])chaveComposta.Clone();
                var proximo = new byte[atual.Length];
                for (ulong i = 0; i < rodadas; i++)
                {
                    cifrador.TransformBlock(atual, 0, atual.Length, proximo, 0);
                    (atual, proximo) = (proximo, atual);
                }
                return SHA256.HashData(atual);
            }

            var argon2d = uuid.AsSpan().SequenceEqual(KdfArgon2d);
            if (!argon2d && !uuid.AsSpan().SequenceEqual(KdfArgon2id))
                throw new ErroLocalizavel("Kdbx.Error.Unsupported");

            var memoria = BinaryPrimitives.ReadUInt64LittleEndian(kdf["M"]);
            var iteracoes = BinaryPrimitives.ReadUInt64LittleEndian(kdf["I"]);
            var paralelismo = BinaryPrimitives.ReadUInt32LittleEndian(kdf["P"]);
            var versao = BinaryPrimitives.ReadUInt32LittleEndian(kdf["V"]);
            if (versao != 0x13 || memoria > MemoriaArgon2Maxima || iteracoes > IteracoesArgon2Maximas
                || paralelismo == 0 || paralelismo > ParalelismoMaximo)
                throw new ErroLocalizavel("Kdbx.Error.Unsupported");

            Argon2 argon2 = argon2d ? new Argon2d(chaveComposta) : new Argon2id(chaveComposta);
            using (argon2)
            {
                argon2.Salt = kdf["S"];
                argon2.MemorySize = (int)(memoria / 1024);
                argon2.Iterations = (int)iteracoes;
                argon2.DegreeOfParallelism = (int)paralelismo;
                return argon2.GetBytes(32);
            }
        }

        private static byte[] Decifrar(byte[] cifra, byte[] chave, byte[] iv, byte[] dados)
        {
            if (cifra.AsSpan().SequenceEqual(CifraAes))
            {
                using var aes = Aes.Create();
                aes.Key = chave;
                try { return aes.DecryptCbc(dados, iv, PaddingMode.PKCS7); }
                catch (CryptographicException ex) { throw new ErroLocalizavel("Kdbx.Error.Corrupted", ex); }
            }

            if (cifra.AsSpan().SequenceEqual(CifraChaCha20))
            {
                var claro = (byte[])dados.Clone();
                new ChaCha20(chave, iv).Aplicar(claro);
                return claro;
            }

            throw new ErroLocalizavel("Kdbx.Error.Unsupported");
        }

        private static Dictionary<string, byte[]> LerDicionarioVariante(byte[] dado)
        {
            var leitor = new Leitor(dado);
            leitor.U16();
            var resultado = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            while (true)
            {
                var tipo = leitor.Byte();
                if (tipo == 0) break;
                var chave = Encoding.UTF8.GetString(leitor.Bytes(leitor.I32()));
                resultado[chave] = leitor.Bytes(leitor.I32());
            }
            return resultado;
        }

        private static byte[] ChaveDoBloco(ulong indice, byte[] baseHmac)
        {
            var entrada = new byte[8 + baseHmac.Length];
            BinaryPrimitives.WriteUInt64LittleEndian(entrada, indice);
            baseHmac.CopyTo(entrada, 8);
            return SHA512.HashData(entrada);
        }

        private static byte[] Concatenar(params byte[][] partes)
        {
            var resultado = new byte[partes.Sum(p => p.Length)];
            var posicao = 0;
            foreach (var parte in partes)
            {
                parte.CopyTo(resultado, posicao);
                posicao += parte.Length;
            }
            return resultado;
        }

        private sealed class Leitor
        {
            private readonly byte[] _dados;
            public int Posicao { get; private set; }

            public Leitor(byte[] dados) => _dados = dados;

            public byte Byte() => Bytes(1)[0];
            public ushort U16() => BinaryPrimitives.ReadUInt16LittleEndian(Bytes(2));
            public uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Bytes(4));
            public int I32() => BinaryPrimitives.ReadInt32LittleEndian(Bytes(4));

            public byte[] Bytes(int quantidade)
            {
                if (quantidade < 0 || Posicao + quantidade > _dados.Length)
                    throw new ErroLocalizavel("Kdbx.Error.Corrupted");
                var resultado = _dados.AsSpan(Posicao, quantidade).ToArray();
                Posicao += quantidade;
                return resultado;
            }
        }
    }
}
