using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class RenderModule
{
    public static IServiceCollection AddRenderModule(this IServiceCollection s, IConfiguration c) { /* DI */ return s; }
    public static IEndpointRouteBuilder MapRenderEndpoints(this IEndpointRouteBuilder e) { /* minimal endpoints */ return e; }
}