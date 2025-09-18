namespace JobsModule;

public interface IJobProcessor
{
    /// <summary>
    /// Processes a single item payload and returns the full path to the produced artifact.
    /// </summary>
    Task<string> ProcessAsync(Guid jobId, int index, string payloadJson, CancellationToken ct);
}
