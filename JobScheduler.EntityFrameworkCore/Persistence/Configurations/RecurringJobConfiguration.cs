using JobScheduler.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScheduler.EntityFrameworkCore.Persistence.Configurations
{
    public sealed class RecurringJobConfiguration : IEntityTypeConfiguration<RecurringJobEntity>
    {
        public void Configure(EntityTypeBuilder<RecurringJobEntity> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.JobType)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.PayloadJson)
                .IsRequired();

            builder.Property(x => x.CronExpression)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.TimeZoneId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.IsEnabled)
                .IsRequired();

            builder.Property(x => x.NextRunAt);

            builder.Property(x => x.LastRunAt);

            builder.HasIndex(x => new
            {
                x.IsEnabled,
                x.NextRunAt
            });
        }
    }
}
