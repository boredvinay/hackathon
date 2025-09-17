namespace JobsModule;

public sealed class StubJobProcessor : IJobProcessor
{
    public async Task<string> ProcessAsync(Guid jobId, int index, string payloadJson, CancellationToken ct)
    {
        var dir = Path.Combine("data", "renders", jobId.ToString());
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"item-{index}.txt");
        await File.WriteAllTextAsync(path, payloadJson, ct);
        return path;
    }
}