using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JobsModule.Dependencies.Interfaces
{
    /// <summary>
    /// Finalizes a job's output by packaging/publishing generated artifacts
    /// (e.g., zipping files, placing them in a known location) and performing
    /// any status updates needed.
    /// </summary>
    public interface IResultSink
    {
        /// <param name="jobId">The job identifier.</param>
        /// <param name="artifactPaths">Full paths to the per-item artifacts produced by the processor.</param>
        /// <param name="ct">Cancellation token.</param>
        Task FinalizeAsync(Guid jobId, IEnumerable<string> artifactPaths, CancellationToken ct);
    }
}
