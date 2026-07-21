using JobScheduler.EntityFrameworkCore.Persistence.Context;
using JobScheduler.EntityFrameworkCore.Storage;
using JobScheduler.Storage.Abstractions.Jobs;
using JobScheduler.Storage.EntityFrameworkCore.Persistence.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JobScheduler.EntityFrameworkCore.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityFrameworkJobStorage(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> configureDbContext)
        {
            services.AddDbContext<JobSchedulerDbContext>(configureDbContext);

            services.AddScoped<IJobStore, EntityFrameworkJobStore>();

            return services;
        }
    }
}
