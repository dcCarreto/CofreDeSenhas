#:package SkiaSharp@2.88.9
// Gera o ícone do app (colorido) e os da bandeja (monocromáticos) no estilo do
// Windows 11. Uso: dotnet run ferramentas/gerar-icones.cs -- <pasta de saída>
// Depois copie app.ico para App/, e app.png e bandeja-*.ico para App/Ativos/.
using SkiaSharp;

var saida = args.Length > 0 ? args[0] : ".";
Directory.CreateDirectory(saida);

// ---------- ícone do app: cadeado azul com alça metálica neutra ----------
SKBitmap App(int tam)
{
    var bmp = new SKBitmap(tam, tam, SKColorType.Rgba8888, SKAlphaType.Premul);
    using var c = new SKCanvas(bmp);
    c.Clear(SKColors.Transparent);
    float k = tam / 256f;
    c.Scale(k);

    // Em tamanhos pequenos o traço precisa engrossar para não sumir.
    float alca = tam <= 24 ? 34 : tam <= 48 ? 30 : 26;

    // Alça
    using (var p = new SKPath())
    {
        p.MoveTo(78, 124);
        p.LineTo(78, 92);
        p.ArcTo(new SKRect(78, 42, 178, 142), 180, 180, false);
        p.LineTo(178, 124);
        using var contorno = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = alca + 3, StrokeCap = SKStrokeCap.Butt, Color = new SKColor(0x5E, 0x5E, 0x5E, 0x90) };
        c.DrawPath(p, contorno);
        using var tinta = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = alca, StrokeCap = SKStrokeCap.Butt,
            Shader = SKShader.CreateLinearGradient(new SKPoint(0, 40), new SKPoint(0, 130),
                new[] { new SKColor(0xEE, 0xEE, 0xEE), new SKColor(0x9C, 0x9C, 0x9C) }, SKShaderTileMode.Clamp)
        };
        c.DrawPath(p, tinta);
    }

    var corpo = new SKRoundRect(new SKRect(36, 112, 220, 236), 30);

    // Sombra suave
    using (var sombra = new SKPaint { IsAntialias = true, Color = new SKColor(0, 0, 0, 0x50), MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 7) })
    {
        c.Save();
        c.Translate(0, 6);
        c.DrawRoundRect(corpo, sombra);
        c.Restore();
    }

    // Corpo
    using (var tinta = new SKPaint
    {
        IsAntialias = true,
        Shader = SKShader.CreateLinearGradient(new SKPoint(0, 112), new SKPoint(0, 236),
            new[] { new SKColor(0x4C, 0xB4, 0xF9), new SKColor(0x0A, 0x5C, 0xC0) }, SKShaderTileMode.Clamp)
    })
        c.DrawRoundRect(corpo, tinta);

    // Brilho no topo do corpo
    using (var brilho = new SKPaint
    {
        IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3,
        Shader = SKShader.CreateLinearGradient(new SKPoint(0, 112), new SKPoint(0, 150),
            new[] { new SKColor(255, 255, 255, 0x80), new SKColor(255, 255, 255, 0) }, SKShaderTileMode.Clamp)
    })
        c.DrawRoundRect(new SKRoundRect(new SKRect(37.5f, 113.5f, 218.5f, 234.5f), 28.5f), brilho);

    // Buraco da fechadura
    using (var furo = new SKPaint { IsAntialias = true, Color = new SKColor(0xF4, 0xF9, 0xFF) })
    {
        float r = tam <= 24 ? 20 : 17;
        c.DrawCircle(128, 164, r, furo);
        if (tam > 24)
        {
            using var fenda = new SKPath();
            fenda.MoveTo(120, 170);
            fenda.LineTo(136, 170);
            fenda.LineTo(140, 206);
            fenda.QuadTo(140, 210, 136, 210);
            fenda.LineTo(120, 210);
            fenda.QuadTo(116, 210, 116, 206);
            fenda.Close();
            c.DrawPath(fenda, furo);
        }
    }

    return bmp;
}

// ---------- ícone da bandeja: glifo monocromático, como os do Windows 11 ----------
SKBitmap Bandeja(int tam, SKColor cor)
{
    var bmp = new SKBitmap(tam, tam, SKColorType.Rgba8888, SKAlphaType.Premul);
    using var c = new SKCanvas(bmp);
    c.Clear(SKColors.Transparent);
    float k = tam / 16f;
    c.Scale(k);

    using var tinta = new SKPaint { IsAntialias = true, Color = cor };

    using (var alca = new SKPath())
    {
        alca.MoveTo(4.75f, 7.5f);
        alca.LineTo(4.75f, 5.25f);
        alca.ArcTo(new SKRect(4.75f, 2f, 11.25f, 8.5f), 180, 180, false);
        alca.LineTo(11.25f, 7.5f);
        using var traco = new SKPaint { IsAntialias = true, Color = cor, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, StrokeCap = SKStrokeCap.Butt };
        c.DrawPath(alca, traco);
    }

    c.DrawRoundRect(new SKRoundRect(new SKRect(2.5f, 7f, 13.5f, 14.5f), 1.75f), tinta);

    using var recorte = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Clear };
    c.DrawCircle(8f, 10.2f, 1.2f, recorte);
    c.DrawRoundRect(new SKRoundRect(new SKRect(7.45f, 10.2f, 8.55f, 12.8f), 0.4f), recorte);

    return bmp;
}

byte[] Png(SKBitmap b)
{
    using var img = SKImage.FromBitmap(b);
    using var dados = img.Encode(SKEncodedImageFormat.Png, 100);
    return dados.ToArray();
}

void Ico(string caminho, IEnumerable<(int Tam, byte[] Png)> imagens)
{
    var lista = imagens.ToList();
    using var fs = File.Create(caminho);
    using var w = new BinaryWriter(fs);
    w.Write((ushort)0); w.Write((ushort)1); w.Write((ushort)lista.Count);
    int offset = 6 + 16 * lista.Count;
    foreach (var (tam, png) in lista)
    {
        w.Write((byte)(tam >= 256 ? 0 : tam));
        w.Write((byte)(tam >= 256 ? 0 : tam));
        w.Write((byte)0); w.Write((byte)0);
        w.Write((ushort)1); w.Write((ushort)32);
        w.Write(png.Length); w.Write(offset);
        offset += png.Length;
    }
    foreach (var (_, png) in lista)
        w.Write(png);
}

int[] tamanhosApp = { 16, 20, 24, 32, 40, 48, 64, 128, 256 };
Ico(Path.Combine(saida, "app.ico"), tamanhosApp.Select(t => (t, Png(App(t)))));
File.WriteAllBytes(Path.Combine(saida, "app.png"), Png(App(256)));

int[] tamanhosBandeja = { 16, 20, 24, 32, 40, 48, 64 };
Ico(Path.Combine(saida, "bandeja-clara.ico"), tamanhosBandeja.Select(t => (t, Png(Bandeja(t, SKColors.White)))));
Ico(Path.Combine(saida, "bandeja-escura.ico"), tamanhosBandeja.Select(t => (t, Png(Bandeja(t, new SKColor(0x1B, 0x1B, 0x1B))))));

// Prévia: tudo lado a lado sobre fundos escuro e claro.
using (var previa = new SKBitmap(760, 340))
using (var c = new SKCanvas(previa))
{
    c.Clear(new SKColor(0x20, 0x20, 0x20));
    c.DrawRect(0, 170, 760, 170, new SKPaint { Color = new SKColor(0xF3, 0xF3, 0xF3) });
    for (int faixa = 0; faixa < 2; faixa++)
    {
        float y = faixa * 170 + 20;
        float x = 20;
        foreach (var t in new[] { 128, 48, 32, 24, 16 })
        {
            c.DrawBitmap(App(t), x, y);
            x += t + 16;
        }
        var cor = faixa == 0 ? SKColors.White : new SKColor(0x1B, 0x1B, 0x1B);
        foreach (var t in new[] { 64, 32, 24, 20, 16 })
        {
            c.DrawBitmap(Bandeja(t, cor), x, y);
            x += t + 16;
        }
    }
    File.WriteAllBytes(Path.Combine(saida, "previa.png"), Png(previa));
}

Console.WriteLine("ok");
