using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

public static class AssetsModule
{
    public static IServiceCollection AddAssetsModule(this IServiceCollection s, IConfiguration c) { /* DI */ return s; }
    public static IEndpointRouteBuilder MapAssetsEndpoints(this IEndpointRouteBuilder e) { /* minimal endpoints */ return e; }
}
