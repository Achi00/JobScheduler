using JobScheduler.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScheduler.EntityFrameworkCore.Persistence.Configurations
{
    internal sealed class JobEntityConfiguration : IEntityTypeConfiguration<JobEntity>
    {
        public void Configure(EntityTypeBuilder<JobEntity> builder)
        {
            builder.ToTable("Jobs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.JobType)
            .HasMaxLength(512)
            .IsRequired();

            builder.Property(x => x.PayloadJson)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.LockedBy)
                .HasMaxLength(256);

            builder.Property(x => x.LastErrorMessage)
                .HasMaxLength(2000);

            builder.Property(x => x.LastErrorType)
                .HasMaxLength(512);

            builder.HasIndex(x => new
            {
                x.Status,
                x.AvailableAt,
                x.CreatedAt
            });

            builder.HasIndex(x => x.LockedUntil);

            builder.HasIndex(x => new
            {
                x.Id,
                x.LockToken
            });

            builder.HasMany(x => x.States)
                .WithOne(x => x.Job)
                .HasForeignKey(x => x.JobId);
        }
    }
}
