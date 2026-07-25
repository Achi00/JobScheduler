using JobScheduler.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobScheduler.EntityFrameworkCore.Persistence.Configurations
{
    internal sealed class JobStateEntityConfiguration : IEntityTypeConfiguration<JobStateEntity>
    {
        public void Configure(EntityTypeBuilder<JobStateEntity> builder)
        {
            builder.ToTable("JobStates");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Reason)
                .HasMaxLength(1000);

            builder.HasIndex(x => new
            {
                x.JobId,
                x.CreatedAt
            });
        }
    }
}
