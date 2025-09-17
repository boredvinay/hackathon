using JobsModule.Dependencies.Interfaces;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JobsModule;

public static class Startup
{
    public static IServiceCollection AddJobsModule(this IServiceCollection s)
    {
        s.AddOptions<JobsOptions>().BindConfiguration("Workers");

        // Business/application layer
  s      .AddScoped<Services.IJobService, Services.JobService>();

        // DB provider (ADO.NET / SQL Server)
        s.AddScoped<IJobProvider, SqlJobProvider>();

        // Queue + processing + sink
        s.AddSingleton<IJobQueue, ChannelJobQueue>();     // singleton queue
        s.AddScoped<IJobProcessor, StubJobProcessor>();   // scoped, resolved inside worker scope
        s.AddScoped<IResultSink, ZipResultSink>();        // scoped, resolved inside worker scope

        // Hosted singleton worker (creates scopes when needed)
        s.AddHostedService<JobsWorker>();

        return s;
    }

    // If you're using MVC controllers, you don't need to map Minimal APIs here.
    public static IEndpointRouteBuilder MapJobsEndpoints(this IEndpointRouteBuilder e) => e;
}

public sealed class JobsOptions
{
    public int WorkerCount { get; set; } = 2;
    public int QueueCapacity { get; set; } = 5000;
}