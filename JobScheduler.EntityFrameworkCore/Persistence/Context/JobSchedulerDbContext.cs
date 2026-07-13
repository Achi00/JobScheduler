using JobScheduler.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobScheduler.EntityFrameworkCore.Persistence.Context
{
    public sealed class JobSchedulerDbContext : DbContext
    {
        internal DbSet<JobEntity> Jobs => Set<JobEntity>();
        internal DbSet<JobStateEntity> JobStates => Set<JobStateEntity>();
        public JobSchedulerDbContext()
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobSchedulerDbContext).Assembly);
        }
    }
}
