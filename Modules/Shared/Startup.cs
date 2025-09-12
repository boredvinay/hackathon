using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class SharedModule
{
    public static IServiceCollection AddSharedModule(this IServiceCollection s, IConfiguration c) { /* DI */ return s; }
    public static IEndpointRouteBuilder MapSharedEndpoints(this IEndpointRouteBuilder e) { /* minimal endpoints */ return e; }
}