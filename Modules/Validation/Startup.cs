using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ValidationModule
{
    public static IServiceCollection AddValidationModule(this IServiceCollection s, IConfiguration c) { /* DI */ return s; }
    public static IEndpointRouteBuilder MapValidationEndpoints(this IEndpointRouteBuilder e) { /* minimal endpoints */ return e; }
}