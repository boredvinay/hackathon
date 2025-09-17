using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace JobsModule.Dependencies.Interfaces
{
    public record QueuedJob(Guid JobId);

    public interface IJobQueue
    {
        ValueTask EnqueueAsync(QueuedJob job, CancellationToken ct);
        ChannelReader<QueuedJob> Reader { get; }
    }
}
