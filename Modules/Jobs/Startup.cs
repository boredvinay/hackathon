using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class JobsModule
{
    public static IServiceCollection AddJobsModule(this IServiceCollection s, IConfiguration c) { /* DI */ return s; }
    public static IEndpointRouteBuilder MapJobsEndpoints(this IEndpointRouteBuilder e) { /* minimal endpoints */ return e; }
}