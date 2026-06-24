using SkiaSharp;

namespace voila.Services;

public sealed class CoverService
{
  private const int Size = 600;
  private readonly SKTypeface _bold;
  private readonly SKTypeface _regular;

  public CoverService(IWebHostEnvironment env)
  {
    var dir = Path.Combine(env.ContentRootPath, "Fonts");
    _bold = SKTypeface.FromFile(Path.Combine(dir, "DejaVuSans-Bold.ttf")) ?? SKTypeface.Default;
    _regular = SKTypeface.FromFile(Path.Combine(dir, "DejaVuSans.ttf")) ?? SKTypeface.Default;
  }

  public byte[] Render(string title, string artist, ulong seed, long index)
  {
    var rng = SeedService.Cover(seed, index);
    var pal = BuildPalette(rng);

    using var bmp = new SKBitmap(Size, Size);
    using (var canvas = new SKCanvas(bmp))
    {
      canvas.Clear(pal.Bg);
      switch (rng.Next(6))
      {
        case 0: Rings(canvas, rng, pal); break;
        case 1: Bauhaus(canvas, rng, pal); break;
        case 2: Stripes(canvas, rng, pal); break;
        case 3: Dots(canvas, rng, pal); break;
        case 4: Waves(canvas, rng, pal); break;
        default: Scatter(canvas, rng, pal); break;
      }
      DrawTextPanel(canvas, rng, pal, title, artist);
      canvas.Flush();
    }

    using var img = SKImage.FromBitmap(bmp);
    using var data = img.Encode(SKEncodedImageFormat.Png, 100);
    return data.ToArray();
  }

  private readonly record struct Palette(SKColor Bg, SKColor[] Colors);

  private static double U(Random r, double a, double b) => a + r.NextDouble() * (b - a);
  private static SKColor Pick(Random r, SKColor[] a) => a[r.Next(a.Length)];
  private static SKColor Hsv(double h, double s, double v) =>
      SKColor.FromHsv((float)(((h % 360) + 360) % 360), (float)(s * 100), (float)(v * 100));
  private static double Luminance(SKColor c) => (0.299 * c.Red + 0.587 * c.Green + 0.114 * c.Blue) / 255.0;

  private static Palette BuildPalette(Random r)
  {
    double baseHue = U(r, 0, 360);
    double[] hues = r.Next(4) switch
    {
      0 => new[] { baseHue, baseHue + 28, baseHue - 28, baseHue + 56 },
      1 => new[] { baseHue, baseHue + 180, baseHue + 18, baseHue + 162 },
      2 => new[] { baseHue, baseHue + 120, baseHue + 240, baseHue + 60 },
      _ => new[] { baseHue, baseHue + 150, baseHue + 210, baseHue + 30 },
    };
    bool dark = r.NextDouble() < 0.55;
    SKColor bg = dark
        ? Hsv(baseHue + U(r, -10, 10), U(r, 0.30, 0.50), U(r, 0.10, 0.17))
        : Hsv(baseHue + U(r, -10, 10), U(r, 0.06, 0.15), U(r, 0.91, 0.97));
    double s = U(r, 0.48, 0.70), v = U(r, 0.74, 0.90);
    var colors = new SKColor[hues.Length];
    for (int i = 0; i < hues.Length; i++) colors[i] = Hsv(hues[i], s, v);
    return new Palette(bg, colors);
  }

  private static void Rings(SKCanvas c, Random r, Palette p)
  {
    using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
    float cx = r.Next((int)(Size * 0.25), (int)(Size * 0.75));
    float cy = r.Next((int)(Size * 0.25), (int)(Size * 0.60));
    int step = r.Next(26, 45);
    int radius = (int)(Size * 1.1), i = 0;
    while (radius > 0)
    {
      paint.Color = p.Colors[i % p.Colors.Length];
      c.DrawCircle(cx, cy, radius, paint);
      radius -= step; i++;
    }
  }

  private static void Bauhaus(SKCanvas c, Random r, Palette p)
  {
    using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
    int cols = r.Next(3, 5), rows = r.Next(3, 5);
    float cw = (float)Size / cols, ch = (float)Size / rows;
    for (int i = 0; i < cols; i++)
      for (int j = 0; j < rows; j++)
        if (r.NextDouble() < 0.55)
        {
          paint.Color = Pick(r, p.Colors);
          c.DrawRect(i * cw, j * ch, cw, ch, paint);
        }

    int rad = r.Next((int)(Size * 0.18), (int)(Size * 0.30));
    paint.Color = Pick(r, p.Colors);
    c.DrawCircle(r.Next(rad, Size - rad), r.Next(rad, (int)(Size * 0.6)), rad, paint);

    float tx = r.Next(0, Size), ty = r.Next(0, (int)(Size * 0.5)); int t = r.Next(120, 241);
    using var tri = new SKPath();
    tri.MoveTo(tx, ty); tri.LineTo(tx + t, ty); tri.LineTo(tx + t / 2f, ty + t); tri.Close();
    paint.Color = Pick(r, p.Colors);
    c.DrawPath(tri, paint);
  }

  private static void Stripes(SKCanvas c, Random r, Palette p)
  {
    using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
    int w = r.Next(34, 61), i = 0;
    for (float x = -Size; x < Size * 2; x += w, i++)
    {
      paint.Color = p.Colors[i % p.Colors.Length];
      using var path = new SKPath();
      path.MoveTo(x, 0); path.LineTo(x + w, 0);
      path.LineTo(x + w - Size, Size); path.LineTo(x - Size, Size); path.Close();
      c.DrawPath(path, paint);
    }
  }

  private static void Dots(SKCanvas c, Random r, Palette p)
  {
    using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
    int n = r.Next(7, 10); float cell = (float)Size / n;
    for (int i = 0; i < n; i++)
      for (int j = 0; j < n; j++)
      {
        float cx = (i + 0.5f) * cell, cy = (j + 0.5f) * cell;
        float rad = cell * 0.5f * (float)U(r, 0.30, 0.95);
        paint.Color = Pick(r, p.Colors);
        c.DrawCircle(cx, cy, rad, paint);
      }
  }

  private static void Waves(SKCanvas c, Random r, Palette p)
  {
    using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
    int bands = r.Next(5, 9); double amp = U(r, 20, 55), ph = U(r, 0, 6); float bh = (float)Size / bands;
    for (int b = 0; b < bands; b++)
    {
      paint.Color = p.Colors[b % p.Colors.Length];
      double freq = U(r, 1.0, 2.0);
      float y0 = b * bh;
      using var path = new SKPath();
      path.MoveTo(0, Size);
      for (int x = 0; x <= Size; x += 12)
      {
        double y = y0 + Math.Sin((double)x / Size * Math.PI * 2 * freq + ph + b) * amp;
        path.LineTo(x, (float)y);
      }
      path.LineTo(Size, Size); path.Close();
      c.DrawPath(path, paint);
    }
  }

  private static void Scatter(SKCanvas c, Random r, Palette p)
  {
    int count = r.Next(14, 23);
    for (int k = 0; k < count; k++)
    {
      var col = Pick(r, p.Colors);
      byte a = (byte)r.Next(140, 236);
      using var paint = new SKPaint
      {
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
        Color = new SKColor(col.Red, col.Green, col.Blue, a)
      };
      float cx = r.Next(0, Size), cy = r.Next(0, (int)(Size * 0.7)); int rad = r.Next(40, 151);
      double kind = r.NextDouble();
      if (kind < 0.5)
      {
        c.DrawCircle(cx, cy, rad, paint);
      }
      else if (kind < 0.8)
      {
        using var tri = new SKPath();
        tri.MoveTo(cx, cy - rad); tri.LineTo(cx - rad, cy + rad); tri.LineTo(cx + rad, cy + rad); tri.Close();
        c.DrawPath(tri, paint);
      }
      else
      {
        c.DrawRect(cx - rad, cy - rad, rad * 2, rad * 2, paint);
      }
    }
  }

  private void DrawTextPanel(SKCanvas c, Random r, Palette p, string title, string artist)
  {
    float pad = Size * 0.06f, panelH = Size * 0.30f, y0 = Size - panelH;

    SKColor panel = r.NextDouble() < 0.7
        ? new SKColor(18, 18, 24, 235)
        : new SKColor(p.Colors[0].Red, p.Colors[0].Green, p.Colors[0].Blue, 235);
    using (var pp = new SKPaint { Style = SKPaintStyle.Fill, Color = panel })
      c.DrawRect(0, y0, Size, panelH, pp);

    using (var ap = new SKPaint { Style = SKPaintStyle.Fill, Color = p.Colors[1] })
      c.DrawRect(pad, y0 + pad * 0.6f, 70, 6, ap);

    SKColor txt = Luminance(panel) < 0.5 ? new SKColor(245, 245, 245) : new SKColor(20, 20, 20);
    float maxW = Size - pad * 2;
    using var paintTxt = new SKPaint { IsAntialias = true, Color = txt };

    string up = title.ToUpperInvariant();
    float ts = FitSize(_bold, up, maxW, Size * 0.085f);
    using var titleFont = new SKFont(_bold, ts);
    var lines = Wrap(titleFont, up, maxW, 2);
    float ty = y0 + pad * 1.4f + ts;
    foreach (var ln in lines)
    {
      c.DrawText(ln, pad, ty, SKTextAlign.Left, titleFont, paintTxt);
      ty += ts * 1.05f;
    }

    float asz = FitSize(_regular, artist, maxW, Size * 0.05f);
    using var artistFont = new SKFont(_regular, asz);
    c.DrawText(artist, pad, ty + 4, SKTextAlign.Left, artistFont, paintTxt);
  }

  private static float FitSize(SKTypeface tf, string text, float maxW, float start, float min = 18f)
  {
    for (float size = start; size > min; size -= 2f)
    {
      using var f = new SKFont(tf, size);
      if (f.MeasureText(text) <= maxW) return size;
    }
    return min;
  }

  private static List<string> Wrap(SKFont font, string text, float maxW, int maxLines)
  {
    var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var lines = new List<string>();
    var cur = "";
    foreach (var w in words)
    {
      var t = cur.Length == 0 ? w : cur + " " + w;
      if (font.MeasureText(t) <= maxW || cur.Length == 0) cur = t;
      else
      {
        lines.Add(cur);
        cur = w;
        if (lines.Count == maxLines - 1) break;
      }
    }
    if (cur.Length > 0 && lines.Count < maxLines) lines.Add(cur);

    var joined = string.Join(" ", lines);
    if (joined.Length < text.Length && lines.Count > 0)
    {
      var last = lines[^1];
      while (last.Length > 1 && font.MeasureText(last + "…") > maxW) last = last[..^1];
      lines[^1] = last + "…";
    }
    return lines;
  }
}