using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class SecurityModule
{
    public static IServiceCollection AddSecurityModule(this IServiceCollection s, IConfiguration c) { /* DI */ return s; }
    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder e) { /* minimal endpoints */ return e; }
}