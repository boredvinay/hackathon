using System.Threading.Channels;
using JobsModule.Dependencies.Interfaces;
using Microsoft.Extensions.Options;

namespace JobsModule;

public sealed class ChannelJobQueue : IJobQueue
{
    private readonly Channel<QueuedJob> _ch;
    public ChannelJobQueue(IOptions<JobsOptions> opt)
        => _ch = Channel.CreateBounded<QueuedJob>(opt.Value.QueueCapacity);

    public ValueTask EnqueueAsync(QueuedJob job, CancellationToken ct) => _ch.Writer.WriteAsync(job, ct);
    public ChannelReader<QueuedJob> Reader => _ch.Reader;
}