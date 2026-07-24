using JobScheduler.Abstractions.Jobs.Interfaces;
using JobScheduler.Core.Execution;
using JobScheduler.Core.HostedServices;
using JobScheduler.Core.Options;
using JobScheduler.Core.Registry;
using JobScheduler.Core.Storage;
using JobScheduler.Core.Workers;
using JobScheduler.Storage.Abstractions.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace JobScheduler.Core.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddJobSchedulerCore
        (
            this IServiceCollection services, 
            Action<JobSchedulerOptions>? configure = null
        )
        {
            var optionsBuilder = services.AddOptions<JobSchedulerOptions>();

            if (configure != null)
            {
                optionsBuilder.Configure(configure);
            }

            // validates and fails on app's startup, not when service first asks for them
            optionsBuilder
            .Validate(options => options.PollingInterval > TimeSpan.Zero,
                "PollingInterval must be greater than zero.")
            .Validate(options => options.LockDuration > TimeSpan.Zero,
                "LockDuration must be greater than zero.")
            .Validate(options => options.DefaultMaxAttempts > 0,
                "DefaultMaxAttempts must be greater than zero.")
            .Validate(options => options.WorkerCount > 0,
                "WorkerCount must be greater than zero.")
            .ValidateOnStart();

            services.AddSingleton<JobRegistry>();

            services.AddScoped<IBackgroundJobClient, BackgroundJobClient>();
            services.AddScoped<IBackgroundJobReader, BackgroundJobReader>();
            services.AddScoped<JobProcessor>();

            return services;
        }

        public static IServiceCollection AddJobSchedulerServer(this IServiceCollection services)
        {
            services.AddHostedService<JobProcessingWorker>();
            services.AddHostedService<LeaseRecoveryWorker>();

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
