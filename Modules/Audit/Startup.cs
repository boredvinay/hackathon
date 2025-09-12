using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
public static class AuditModule
{
    public static IServiceCollection AddAuditModule(this IServiceCollection s, IConfiguration c) { /* DI */ return s; }
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder e) { /* minimal endpoints */ return e; }
}