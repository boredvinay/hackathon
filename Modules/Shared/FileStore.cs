using System.Text;

namespace SharedModule;

public sealed class FileStore : IFileStore
{
    public string Root { get; }

    public void EnsureDir(string path) => Directory.CreateDirectory(path);
    public FileStore(string root)
    {
        // Normalize to absolute path
        Root = Path.GetFullPath(string.IsNullOrWhiteSpace(root) ? "data" : root);
        Directory.CreateDirectory(Root);
    }

    public string Combine(params string[] parts)
    {
        // Join under Root and normalize separators
        var all = new List<string> { Root };
        all.AddRange(parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        return Path.GetFullPath(Path.Combine(all.ToArray()));
    }

    public Task CreateDirectoryAsync(string path, CancellationToken ct = default)
    {
        // Accept both absolute and relative (relative will be rooted)
        var abs = Path.IsPathFullyQualified(path) ? path : Combine(path);
        Directory.CreateDirectory(abs);
        return Task.CompletedTask;
    }

    public async Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken ct = default)
    {
        var abs = Path.IsPathFullyQualified(path) ? path : Combine(path);
        Directory.CreateDirectory(Path.GetDirectoryName(abs)!);
        await File.WriteAllBytesAsync(abs, bytes, ct);
    }

    public async Task WriteAllTextAsync(string path, string contents, CancellationToken ct = default)
    {
        var abs = Path.IsPathFullyQualified(path) ? path : Combine(path);
        Directory.CreateDirectory(Path.GetDirectoryName(abs)!);
        await File.WriteAllTextAsync(abs, contents, Encoding.UTF8, ct);
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
    {
        var abs = Path.IsPathFullyQualified(path) ? path : Combine(path);
        Stream s = File.Open(abs, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Task.FromResult(s);
    }

    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        var abs = Path.IsPathFullyQualified(path) ? path : Combine(path);
        return Task.FromResult(File.Exists(abs));
    }
}
