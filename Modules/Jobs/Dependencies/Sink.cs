using System.IO.Compression;
using JobsModule.Dependencies.Interfaces;
using SharedModule;

namespace JobsModule;

public sealed class ZipResultSink(IFileStore fs, IJobProvider db) : IResultSink
{
    public async Task FinalizeAsync(Guid jobId, IEnumerable<string> files, CancellationToken ct)
    {
        var outDir = fs.Combine("renders");
        fs.EnsureDir(outDir);
        var zipPath = Path.Combine(outDir, $"{jobId}.zip");

        using (var stream = File.Create(zipPath))
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            foreach (var f in files)
                if (File.Exists(f))
                    zip.CreateEntryFromFile(f, Path.GetFileName(f));
        }

        await db.UpdateJobStatusAsync(jobId, "Completed", zipPath, null, ct);
    }
}