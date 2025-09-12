using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Models;

namespace Persistence.EntityTypeConfigs;

public sealed class JobItemConfig : IEntityTypeConfiguration<JobItem>
{
    public void Configure(EntityTypeBuilder<JobItem> e)
    {
        e.ToTable("JobItems");
        e.HasKey(x => x.Id);
        e.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        e.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)");
        e.Property(x => x.Error);
        e.HasIndex(x => new { x.JobId, x.Status });
    }
}