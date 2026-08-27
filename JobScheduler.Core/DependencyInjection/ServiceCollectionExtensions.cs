using JobScheduler.Abstractions.Jobs.Interfaces;
using JobScheduler.Core.Clients;
using JobScheduler.Core.Execution;
using JobScheduler.Core.Execution.Interfaces;
using JobScheduler.Core.Execution.Scope;
using JobScheduler.Core.HostedServices;
using JobScheduler.Core.Options;
using JobScheduler.Core.Recurring;
using JobScheduler.Core.Recurring.Interfaces;
using JobScheduler.Core.Registry;
using JobScheduler.Core.Registry.Interfaces;
using JobScheduler.Core.Resolvers;
using JobScheduler.Core.Storage;
using JobScheduler.Core.Workers;
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

            //services.AddSingleton<JobRegistry>();
            services.AddSingleton<IJobRegistry, JobRegistry>();

            // clients
            services.AddScoped<IBackgroundJobClient, BackgroundJobClient>();
            services.AddScoped<IRecurringJobClient, RecurringJobClient>();
            
            // readers
            services.AddScoped<IBackgroundJobReader, BackgroundJobReader>();
            
            // processors
            services.AddScoped<JobProcessor>();
            services.AddScoped<RecurringJobProcessor>();

            //time
            services.AddSingleton(TimeProvider.System);
            
            // cron
            services.AddSingleton<ICronScheduler, CronosScheduler>();

            services.AddScoped<IJobExecutionScopeFactory, ServiceProviderJobExecutionScopeFactory>();

            return services;
        }

        public static IServiceCollection AddJobSchedulerServer(this IServiceCollection services)
        {
            services.AddHostedService<JobProcessingWorker>();
            services.AddHostedService<LeaseRecoveryWorker>();
            services.AddHostedService<RecurringJobSchedulerWorker>();

            return services;
        }

        // used to register clients job handler
        public static IServiceCollection AddJob<TPayload, THandler>(this IServiceCollection services) 
            where THandler : class, IJobHandler<TPayload>
        {
            //var jobType = typeof(TPayload).FullName!;
            var jobType = JobTypeNameResolver.Resolve<TPayload>();

            services.AddScoped<THandler>();

            services.AddSingleton<IJobExecutor>(
                new JobExecutor<TPayload, THandler>(jobType));

            return services;
        }
    }
}
