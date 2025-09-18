public interface IFileStore
{
    void EnsureDir(string path);

    /// <summary>
    /// Absolute path by joining the store root with parts.
    /// </summary>
    string Combine(params string[] parts);

    /// <summary>Create a directory (and parents) if it doesn't exist.</summary>
    Task CreateDirectoryAsync(string path, CancellationToken ct = default);

    /// <summary>Write bytes to a file. Creates parent directory if needed.</summary>
    Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken ct = default);

    /// <summary>Write text to a file. Creates parent directory if needed.</summary>
    Task WriteAllTextAsync(string path, string contents, CancellationToken ct = default);

    /// <summary>Open a read stream to an existing file.</summary>
    Task<Stream> OpenReadAsync(string path, CancellationToken ct = default);

    /// <summary>Check if a file exists.</summary>
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);

    /// <summary>Root folder for the store (absolute).</summary>
    string Root { get; }
}