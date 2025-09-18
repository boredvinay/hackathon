using DesignModule.Services.Interfaces;

namespace DesignModule.Services;

public sealed class PreviewService : IPreviewService
{
    public Task<string> RenderPreviewAsync(Guid versionId, string dslJson, CancellationToken ct)
    {
        // For now, just write the DSL to a .json alongside a "pretend" PNG path.
        var dir = Path.Combine("data", "designs", versionId.ToString());
        Directory.CreateDirectory(dir);
        var dslPath = Path.Combine(dir, "dsl.json");
        File.WriteAllText(dslPath, dslJson);
        var png = Path.Combine(dir, "preview.png");
        // (You can have RenderModule produce an actual PNG here)
        if (!File.Exists(png)) File.WriteAllBytes(png, Array.Empty<byte>());
        return Task.FromResult(png);
    }
}