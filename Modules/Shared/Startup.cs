using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SharedModule;

public static class Startup
{
    public static IServiceCollection AddSharedModule(this IServiceCollection s)
    {
        s.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
        s.AddSingleton<IFileStore>(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var root = cfg["DataRoot"];
            if (string.IsNullOrWhiteSpace(root))
                root = "data";
            return new FileStore(root);
        });

        return s;
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