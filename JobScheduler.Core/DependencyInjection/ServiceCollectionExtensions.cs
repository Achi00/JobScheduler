using JobScheduler.Abstractions.Jobs.Interfaces;
using JobScheduler.Core.Execution;
using JobScheduler.Core.HostedServices;
using JobScheduler.Core.Registry;
using JobScheduler.Core.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace JobScheduler.Core.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddJobSchedulerCore(this IServiceCollection services)
        {
            services.AddSingleton<IJobStore, InMemoryJobStore>();
            services.AddSingleton<JobRegistry>();

            services.AddScoped<IBackgroundJobClient, BackgroundJobClient>();
            services.AddScoped<JobProcessor>();

            services.AddHostedService<JobWorkerHostedService>();

            return services;
        }

        public static IServiceCollection AddJob<TPayload, THandler>(this IServiceCollection services) 
            where THandler : class, IJobHandler<TPayload>
        {
            var jobType = typeof(TPayload).FullName!;

            services.AddScoped<THandler>();

            services.AddSingleton<IJobExecutor>(
                new JobExecutor<TPayload, THandler>(jobType));

            return services;
        }
    }
}
