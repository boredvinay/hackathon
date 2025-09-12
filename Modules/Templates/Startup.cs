using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class TemplatesModule
{
    public static IServiceCollection AddTemplatesModule(this IServiceCollection s, IConfiguration c) { /* DI */ return s; }
    public static IEndpointRouteBuilder MapTemplatesEndpoints(this IEndpointRouteBuilder e) { /* minimal endpoints */ return e; }
}