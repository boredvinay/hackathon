using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using RenderModule.Services.DTO;
using SharedModule;
using RenderModule.Services.Interfaces;
using RenderModule.Providers;
using Color = System.Drawing.Color;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace RenderModule.Services;

public sealed class DiffService(
    IDesignReadProvider designReader,
    IFileStore files) : IDiffService
{
    public async Task<DiffResponse> DiffAsync(DiffRequest req, CancellationToken ct)
    {
        if (req.Mode.Equals("dsl", StringComparison.OrdinalIgnoreCase))
            return await DslDiffAsync(req, ct);

        // Default: pixel diff on preview images (requires previews generated in Design module)
        var vA = await designReader.GetVersionAsync(req.VersionA, ct) ?? throw new InvalidOperationException("Version A not found");
        var vB = await designReader.GetVersionAsync(req.VersionB, ct) ?? throw new InvalidOperationException("Version B not found");

        if (string.IsNullOrWhiteSpace(vA.PreviewPath) || string.IsNullOrWhiteSpace(vB.PreviewPath) ||
            !System.IO.File.Exists(vA.PreviewPath) || !System.IO.File.Exists(vB.PreviewPath))
        {
            // Fallback: compare DSL hashes if previews not available
            return await DslDiffAsync(req, ct);
        }

        // Try to compute a simple pixel diff (Windows-friendly)
        double score = 0.0;
        string diffOut = files.Combine("renders", DateTime.UtcNow.ToString("yyyy/MM/dd"), $"diff-{Guid.NewGuid():N}.png");
        await files.CreateDirectoryAsync(Path.GetDirectoryName(diffOut)!, ct);

        try
        {
            using var a = new Bitmap(vA.PreviewPath);
            using var b = new Bitmap(vB.PreviewPath);
            int w = Math.Min(a.Width, b.Width);
            int h = Math.Min(a.Height, b.Height);
            using var diff = new Bitmap(w, h);

            long total = 0;
            long equal = 0;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var ca = a.GetPixel(x, y);
                    var cb = b.GetPixel(x, y);
                    if (ca.ToArgb() == cb.ToArgb())
                    {
                        diff.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0)); // transparent
                        equal++;
                    }
                    else
                    {
                        // mark changed pixel in red overlay
                        diff.SetPixel(x, y, Color.FromArgb(200, 255, 0, 0));
                    }
                    total++;
                }

            diff.Save(diffOut, ImageFormat.Png);
            score = total == 0 ? 1.0 : (double)equal / total; // crude similarity (0..1)
        }
        catch
        {
            // If System.Drawing fails, fall back to hash comparison
            score = await QuickHashSimilarity(req, ct);
            diffOut = "";
        }

        var report = files.Combine("renders", DateTime.UtcNow.ToString("yyyy/MM/dd"), $"diff-{Guid.NewGuid():N}.json");
        var summary = $"Pixel diff similarity ≈ {score:0.000}";
        var json = $$"""
        {"mode":"pixel","versionA":"{{req.VersionA}}","versionB":"{{req.VersionB}}","score":{{score}},"diffImagePath":"{{diffOut.Replace("\\", "/")}}","summary":"{{summary}}"}
        """;
        await System.IO.File.WriteAllTextAsync(report, json, ct);

        return new DiffResponse
        {
            Mode = "pixel",
            Score = score,
            DiffImagePath = string.IsNullOrWhiteSpace(diffOut) ? null : diffOut,
            ReportPath = report,
            Summary = summary
        };
    }

    private async Task<DiffResponse> DslDiffAsync(DiffRequest req, CancellationToken ct)
    {
        var dslA = await designReader.GetDslAsync(req.VersionA, ct) ?? throw new InvalidOperationException("DSL A missing");
        var dslB = await designReader.GetDslAsync(req.VersionB, ct) ?? throw new InvalidOperationException("DSL B missing");

        var hashA = Sha256(dslA);
        var hashB = Sha256(dslB);
        var score = hashA == hashB ? 1.0 : 0.0;

        var report = Path.Combine("data", "renders", DateTime.UtcNow.ToString("yyyy/MM/dd"), $"diff-{Guid.NewGuid():N}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(report)!);
        var json = $$"""
        {"mode":"dsl","versionA":"{{req.VersionA}}","versionB":"{{req.VersionB}}","hashA":"{{hashA}}","hashB":"{{hashB}}","score":{{score}}}
        """;
        await System.IO.File.WriteAllTextAsync(report, json, ct);

        return new DiffResponse
        {
            Mode = "dsl",
            Score = score,
            DiffImagePath = null,
            ReportPath = report,
            Summary = score == 1.0 ? "DSLs are identical." : "DSLs differ (hash mismatch)."
        };
    }

    private static string Sha256(string s)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();

    private async Task<double> QuickHashSimilarity(DiffRequest req, CancellationToken ct)
    {
        var dslA = await designReader.GetDslAsync(req.VersionA, ct) ?? "";
        var dslB = await designReader.GetDslAsync(req.VersionB, ct) ?? "";
        return Sha256(dslA) == Sha256(dslB) ? 1.0 : 0.0;
    }
}
