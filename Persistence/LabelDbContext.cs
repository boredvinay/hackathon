using Microsoft.EntityFrameworkCore;
using Persistence.Models;

namespace Persistence;

public sealed class LabelDbContext(DbContextOptions<LabelDbContext> options) : DbContext(options)
{
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobItem> JobItems => Set<JobItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyConfigurationsFromAssembly(typeof(LabelDbContext).Assembly);
    }
}