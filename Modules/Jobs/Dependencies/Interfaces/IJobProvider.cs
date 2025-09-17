public interface IJobProvider
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<Guid?> FindByIdempotencyKeyAsync(string key, CancellationToken ct);

    Task CreateJobAsync(
        Guid id,
        Guid templateVersionId,
        string type,
        string status,
        IEnumerable<string> itemsJson,
        string? idempotencyKey,
        string? webhookUrl,
        CancellationToken ct);

    Task<JobRow?> GetJobAsync(Guid id, CancellationToken ct);

    Task<(int total, int completed, int failed)> GetJobProgressAsync(Guid id, CancellationToken ct);

    Task UpdateJobStatusAsync(Guid id, string status, string? resultPath, string? error, CancellationToken ct);

    Task<IReadOnlyList<(Guid itemId, string payloadJson)>> GetPendingItemsAsync(Guid jobId, CancellationToken ct);

    Task MarkItemCompletedAsync(Guid itemId, CancellationToken ct);

    Task MarkItemFailedAsync(Guid itemId, string error, CancellationToken ct);
}