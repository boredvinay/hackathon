using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Models;

namespace Persistence.EntityTypeConfigs;

public sealed class JobConfig : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> e)
    {
        e.ToTable("Jobs");
        e.HasKey(x => x.Id);
        e.Property(x => x.Type).HasConversion<string>().HasMaxLength(16);
        e.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        e.Property(x => x.IdempotencyKey).HasMaxLength(100).IsUnicode(false);
        e.Property(x => x.TemplateKey).HasMaxLength(128);
        e.Property(x => x.TemplateVersion).HasMaxLength(32);
        e.Property(x => x.ResultPath).HasMaxLength(512);
        e.Property(x => x.Error);
        e.HasIndex(x => x.IdempotencyKey).HasDatabaseName("IX_Jobs_IdemKey");
        e.HasMany(x => x.Items).WithOne(x => x.Job!).HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
    }
}