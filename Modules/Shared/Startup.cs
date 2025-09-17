using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SharedModule;

public static class Startup
{
    public static IServiceCollection AddSharedModule(this IServiceCollection s)
    {
        s.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
        s.AddSingleton<IFileStore, LocalFileStore>();
        return s;
    }
}


public interface IDbConnectionFactory
{
    DbConnection Create();
}

public sealed class SqlConnectionFactory(IConfiguration cfg) : IDbConnectionFactory
{
    private readonly string _cs = cfg.GetConnectionString("LabelDb")!;
    public DbConnection Create() => new SqlConnection(_cs);
}
public interface IFileStore
{
    string Root { get; }
    string Combine(params string[] parts);    // Root + parts
    void EnsureDir(string path);              // mkdir -p
    Task<Stream> OpenReadAsync(string path, CancellationToken ct = default);
    Task<Stream> OpenWriteAsync(string path, bool overwrite = true, CancellationToken ct = default);
}

public sealed class LocalFileStore : IFileStore
{
    public string Root { get; }
    public LocalFileStore(IConfiguration cfg)
    {
        Root = cfg["DataRoot"] ?? "data";
        Directory.CreateDirectory(Root);
    }

    public string Combine(params string[] parts) =>
        Path.Combine(new[] { Root }.Concat(parts).ToArray());

    public void EnsureDir(string path) => Directory.CreateDirectory(path);

    public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
        => Task.FromResult<Stream>(File.OpenRead(path));

    public Task<Stream> OpenWriteAsync(string path, bool overwrite = true, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var mode = overwrite ? FileMode.Create : FileMode.CreateNew;
        return Task.FromResult<Stream>(File.Open(path, mode, FileAccess.Write, FileShare.None));
    }
}
public static class Paths
{
    public static string RendersDir(IFileStore fs) => fs.Combine("renders");
    public static string RenderZip(IFileStore fs, Guid jobId) => fs.Combine("renders", $"{jobId}.zip");
    public static string AssetsDir(IFileStore fs) => fs.Combine("assets");
    public static string BundlesDir(IFileStore fs) => fs.Combine("bundles");
    public static string PreviewsDir(IFileStore fs) => fs.Combine("previews");
}