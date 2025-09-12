using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration cfg)
    {
        var cs = cfg.GetConnectionString("LabelDb");

        services.AddDbContext<LabelDbContext>(opt =>
            opt.UseSqlServer(cs, sql => sql.EnableRetryOnFailure()));

        return services;
    }

    // call once on startup to apply migrations (dev only)
    public static void MigrateDb(this IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LabelDbContext>();
        db.Database.Migrate();
    }
}