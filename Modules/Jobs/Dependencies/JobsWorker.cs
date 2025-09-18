using JobsModule.Dependencies.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobsModule;

public sealed class JobsWorker(
    IJobQueue queue,
    IServiceScopeFactory scopeFactory,
    IOptions<JobsOptions> opt,
    ILogger<JobsWorker> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var workers = Math.Max(0, opt.Value.WorkerCount);
        if (workers <= 0)
        {
            log.LogInformation("Jobs worker paused (WorkerCount=0)");
            return;
        }

        var tasks = Enumerable.Range(0, workers).Select(_ => RunAsync(ct));
        await Task.WhenAll(tasks);
    }

    private async Task RunAsync(CancellationToken ct)
    {
        while (await queue.Reader.WaitToReadAsync(ct))
        {
            while (queue.Reader.TryRead(out var q))
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<IJobProvider>();
                    var processor = scope.ServiceProvider.GetRequiredService<IJobProcessor>();
                    var sink = scope.ServiceProvider.GetRequiredService<IResultSink>();

                    var items = await db.GetPendingItemsAsync(q.JobId, ct);
                    var outputs = new List<string>();

                    int i = 0;
                    foreach (var (itemId, payloadJson) in items)
                    {
                        var file = await processor.ProcessAsync(q.JobId, ++i, payloadJson, ct);
                        outputs.Add(file);
                        await db.MarkItemCompletedAsync(itemId, ct);
                    }

                    await sink.FinalizeAsync(q.JobId, outputs, ct);
                }
                catch (Exception ex)
                {
                    using var scope = scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<IJobProvider>();
                    await db.UpdateJobStatusAsync(q.JobId, "Failed", null, ex.Message, ct);
                }
            }
        }
    }
}
