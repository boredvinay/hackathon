using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using QuestPDF.Fluent;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace RenderModule.Engines;

public sealed class PdfEngine : IPdfEngine
{
    public byte[] RenderPdf(string dslJson, Dictionary<string, object>? payload)
    {
        var dsl = Dsl.Parse(dslJson);
        var data = payload ?? new Dictionary<string, object>();

        // 1) Rasterize whole canvas to a high-quality PNG (at DSL dpi)
        using var bmp = new Bitmap(dsl.Size.Width, dsl.Size.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            foreach (var w in dsl.Widgets.OrderBy(w => w.Z))
                DrawWidget(g, w, data);
        }

        byte[] pngBytes;
        using (var ms = new MemoryStream())
        {
            bmp.Save(ms, ImageFormat.Png);
            pngBytes = ms.ToArray();
        }

        // 2) Build a PDF sized to the physical canvas (pixels/dpi inches * 72 points)
        var widthPoints = dsl.Size.Width / (double)dsl.Dpi * 72.0;
        var heightPoints = dsl.Size.Height / (double)dsl.Dpi * 72.0;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(0);
                page.Size((float)widthPoints, (float)heightPoints);
                page.Content().Image(pngBytes);
            });
        });

        using var outStream = new MemoryStream();
        doc.GeneratePdf(outStream);
        return outStream.ToArray();
    }

    // ---------------- drawing ----------------

    private static void DrawWidget(Graphics g, Widget w, IReadOnlyDictionary<string, object> payload)
    {
        var x = w.Position.X; var y = w.Position.Y;
        var wpx = w.Size.Width; var hpx = w.Size.Height;

        switch (w.Type)
        {
            case "text":
                {
                    var text = ResolveString(payload, w.Bind, w.ValueString);
                    using var font = new Font(FontFamily.GenericSansSerif, w.FontSize ?? 16f, w.Bold ? FontStyle.Bold : FontStyle.Regular);
                    using var brush = new SolidBrush(ParseColor(w.Color ?? "#000000"));
                    using var fmt = new StringFormat { Alignment = MapAlign(w.Align), LineAlignment = StringAlignment.Near };

                    var rect = new RectangleF(x, y, wpx, hpx);
                    if (w.Rotation != 0)
                    {
                        g.TranslateTransform(x + wpx / 2f, y + hpx / 2f);
                        g.RotateTransform(w.Rotation);
                        g.TranslateTransform(-(x + wpx / 2f), -(y + hpx / 2f));
                    }
                    g.DrawString(text, font, brush, rect, fmt);
                    if (w.Rotation != 0) g.ResetTransform();
                    break;
                }

            case "shape":
                {
                    if (w.Shape == "rect")
                    {
                        using var pen = new Pen(ParseColor(w.StrokeColor ?? "#000000"), w.StrokeWidth ?? 1f);
                        if (w.FillColor is string fill)
                        {
                            using var fillBrush = new SolidBrush(ParseColor(fill));
                            g.FillRectangle(fillBrush, x, y, wpx, hpx);
                        }
                        g.DrawRectangle(pen, x, y, wpx, hpx);
                    }
                    break;
                }

            case "barcode1D":
                {
                    var value = ResolveString(payload, w.Bind, w.ValueString);
                    using var img = BarcodeRenderer.Render1D(value, w.BarcodeType ?? "Code128", wpx, hpx, w.HumanReadable ?? true, w.XDim ?? 2, w.QuietZone ?? 4);
                    DrawImageWithRotation(g, img, x, y, w.Rotation);
                    break;
                }

            case "barcode2D":
                {
                    var value = ResolveFor2D(payload, w.Bind, w.ValueRaw);
                    using var img = BarcodeRenderer.Render2D(value, w.BarcodeType ?? "QRCode", wpx, hpx);
                    DrawImageWithRotation(g, img, x, y, w.Rotation);
                    break;
                }

            case "image":
                {
                    if (w.SourceBase64 is string b64)
                    {
                        var raw = Convert.FromBase64String(b64);
                        using var ms = new MemoryStream(raw);
                        using var img = Image.FromStream(ms);
                        DrawImageWithRotation(g, (Image)img.Clone(), x, y, w.Rotation);
                    }
                    break;
                }
        }
    }

    private static void DrawImageWithRotation(Graphics g, Image img, int x, int y, int rotation)
    {
        var rect = new Rectangle(x, y, img.Width, img.Height);
        if (rotation != 0)
        {
            g.TranslateTransform(x + img.Width / 2f, y + img.Height / 2f);
            g.RotateTransform(rotation);
            g.TranslateTransform(-(x + img.Width / 2f), -(y + img.Height / 2f));
        }
        g.DrawImage(img, rect);
        if (rotation != 0) g.ResetTransform();
    }

    private static string ResolveString(IReadOnlyDictionary<string, object> payload, string? bind, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(bind) && payload.TryGetValue(bind!, out var v))
            return v?.ToString() ?? fallback ?? "";
        return fallback ?? "";
    }

    private static string ResolveFor2D(IReadOnlyDictionary<string, object> payload, string? bind, object? fallback)
    {
        if (!string.IsNullOrWhiteSpace(bind) && payload.TryGetValue(bind!, out var v))
            return v is string s ? s : JsonSerializer.Serialize(v);
        return fallback is string s2 ? s2 : JsonSerializer.Serialize(fallback);
    }

    private static Color ParseColor(string hex)
    {
        if (hex.StartsWith("#")) hex = hex[1..];
        if (hex.Length == 6)
            return Color.FromArgb(255,
                Convert.ToInt32(hex.Substring(0, 2), 16),
                Convert.ToInt32(hex.Substring(2, 2), 16),
                Convert.ToInt32(hex.Substring(4, 2), 16));
        if (hex.Length == 8)
            return Color.FromArgb(
                Convert.ToInt32(hex.Substring(0, 2), 16),
                Convert.ToInt32(hex.Substring(2, 2), 16),
                Convert.ToInt32(hex.Substring(4, 2), 16),
                Convert.ToInt32(hex.Substring(6, 2), 16));
        return Color.Black;
    }

    private static StringAlignment MapAlign(string? align) => align?.ToLowerInvariant() switch
    {
        "center" => StringAlignment.Center,
        "right" => StringAlignment.Far,
        _ => StringAlignment.Near
    };

    // ---------------- DSL model ----------------

    private sealed record Dsl(DesignSpec Design, CanvasSize Size, int Dpi, List<Widget> Widgets)
    {
        public static Dsl Parse(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var design = root.GetProperty("design");
            var dpi = design.TryGetProperty("dpi", out var d) ? d.GetInt32() : 203;
            var sizeEl = design.GetProperty("size");
            var width = sizeEl.GetProperty("width").GetInt32();
            var height = sizeEl.GetProperty("height").GetInt32();

            var widgets = new List<Widget>();
            if (root.TryGetProperty("widgets", out var arr) && arr.ValueKind == JsonValueKind.Array)
                foreach (var w in arr.EnumerateArray())
                    widgets.Add(Widget.Parse(w));

            return new Dsl(new DesignSpec(), new CanvasSize(width, height), dpi, widgets);
        }
    }

    private sealed record DesignSpec;
    private sealed record CanvasSize(int Width, int Height);

    private sealed class Widget
    {
        public string Id { get; init; } = Guid.NewGuid().ToString("N");
        public string Type { get; init; } = "text";
        public string? Placeholder { get; init; }
        public string? Bind { get; init; }
        public string? ValueString { get; set; }
        public object? ValueRaw { get; set; }
        public string? BarcodeType { get; init; }
        public bool? HumanReadable { get; init; }
        public int? XDim { get; init; }
        public int? QuietZone { get; init; }

        public Position Position { get; init; } = new(0, 0);
        public Size Size { get; init; } = new(100, 30);
        public string? Color { get; init; }
        public int Rotation { get; init; } = 0;
        public int Z { get; init; } = 0;

        // text
        public int? FontSize { get; init; }
        public string? Align { get; init; }
        public bool Bold { get; init; }

        // shape
        public string? Shape { get; init; }
        public string? StrokeColor { get; init; }
        public float? StrokeWidth { get; init; }
        public string? FillColor { get; init; }

        // image
        public string? SourceBase64 { get; init; }

        public static Widget Parse(JsonElement e)
        {
            var w = new Widget
            {
                Id = e.TryGetProperty("id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString("N") : Guid.NewGuid().ToString("N"),
                Type = e.GetProperty("type").GetString() ?? "text",
                Placeholder = e.TryGetProperty("placeholder", out var p) ? p.GetString() : null,
                Bind = e.TryGetProperty("bind", out var b) ? b.GetString() : null,
                BarcodeType = e.TryGetProperty("barcodeType", out var bt) ? bt.GetString() : null,
                HumanReadable = e.TryGetProperty("humanReadable", out var hr) && hr.ValueKind == JsonValueKind.True ? true
                               : e.TryGetProperty("humanReadable", out hr) && hr.ValueKind == JsonValueKind.False ? false : null,
                XDim = e.TryGetProperty("xDim", out var xd) ? xd.GetInt32() : (int?)null,
                QuietZone = e.TryGetProperty("quietZone", out var qz) ? qz.GetInt32() : (int?)null,
                Rotation = e.TryGetProperty("rotation", out var rot) ? rot.GetInt32() : 0,
                Z = e.TryGetProperty("z", out var z) ? z.GetInt32() : 0,
                FontSize = e.TryGetProperty("fontSize", out var fs) ? fs.GetInt32() : (int?)null,
                Align = e.TryGetProperty("align", out var al) ? al.GetString() : null,
                Bold = e.TryGetProperty("bold", out var bo) && bo.ValueKind == JsonValueKind.True,
                Color = e.TryGetProperty("color", out var col) ? col.GetString() : "#000000",
                Shape = e.TryGetProperty("shape", out var sh) ? sh.GetString() : null,
                StrokeColor = e.TryGetProperty("strokeColor", out var sc) ? sc.GetString() : null,
                StrokeWidth = e.TryGetProperty("strokeWidth", out var sw) ? (float)sw.GetDouble() : (float?)null,
                FillColor = e.TryGetProperty("fillColor", out var fc) ? fc.GetString() : null,
                SourceBase64 = e.TryGetProperty("sourceBase64", out var sb) ? sb.GetString() : null,
                Position = new Position(e.GetProperty("position").GetProperty("x").GetInt32(),
                                            e.GetProperty("position").GetProperty("y").GetInt32()),
                Size = new Size(e.GetProperty("size").GetProperty("width").GetInt32(),
                                        e.GetProperty("size").GetProperty("height").GetInt32()),
            };

            if (e.TryGetProperty("value", out var val))
            {
                w.ValueString = val.ValueKind == JsonValueKind.String ? val.GetString() : null;
                w.ValueRaw = val.ValueKind == JsonValueKind.String ? w.ValueString : JsonSerializer.Deserialize<object>(val.GetRawText());
            }

            return w;
        }
    }

    private sealed record Position(int X, int Y);
    private sealed record Size(int Width, int Height);
}
