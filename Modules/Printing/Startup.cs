using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class PrintingModule
{
    public static IServiceCollection AddPrintingModule(this IServiceCollection s, IConfiguration c) { /* DI */ return s; }
    public static IEndpointRouteBuilder MapPrintingEndpoints(this IEndpointRouteBuilder e) { /* minimal endpoints */ return e; }
}