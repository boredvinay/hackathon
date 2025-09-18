using DesignModule.Services.Interfaces;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DesignModule;

public static class Startup
{
    public static IServiceCollection AddDesignModule(this IServiceCollection s)
    {
        // Services (application layer)
        s.AddScoped<IDesignService, Services.DesignService>();

        // Providers (DB access)
        s.AddScoped<IDesignProvider, Providers.SqlDesignProvider>();

        // Optional: preview generator (stubbed)
        s.AddScoped<IPreviewService, Services.PreviewService>();

        return s;
    }

    public static IEndpointRouteBuilder MapDesignEndpoints(this IEndpointRouteBuilder e) => e;
}