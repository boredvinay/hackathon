using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RenderModule.Engines;
using RenderModule.Providers;
using RenderModule.Services;
using RenderModule.Services.Interfaces;

namespace RenderModule;

public static class Startup
{
    public static IServiceCollection AddRenderModule(this IServiceCollection s)
    {
        // Services
        s.AddScoped<IRenderService, RenderService>();
        s.AddScoped<IMergeService, MergeService>();
        s.AddScoped<IDiffService, DiffService>();

        // Providers / engines
        s.AddScoped<IDesignReadProvider, DesignReadProvider>();
        s.AddScoped<IPdfEngine, PdfEngine>(); 

        return s;
    }

    public static IEndpointRouteBuilder MapRenderEndpoints(this IEndpointRouteBuilder e) => e;
}