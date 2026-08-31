using JobScheduler.Core.Options;
using JobScheduler.Storage.Abstractions.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobScheduler.Core.Workers
{
    internal sealed class LeaseRecoveryWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptionsMonitor<JobSchedulerOptions> _options;
        private readonly ILogger<LeaseRecoveryWorker> _logger;

        public LeaseRecoveryWorker(IServiceScopeFactory scopeFactory, IOptionsMonitor<JobSchedulerOptions> options, ILogger<LeaseRecoveryWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // workes based on options passed interval
            try
            {
                using var timer = new PeriodicTimer(_options.CurrentValue.LeaseRecoveryInterval);
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        await using var scope =
                            _scopeFactory.CreateAsyncScope();

                        var store =
                            scope.ServiceProvider.GetRequiredService<IJobStore>();

                        var recovered = await store.RecoverExpiredJobsAsync(
                            _options.CurrentValue.LeaseRecoveryBatchSize,
                            _options.CurrentValue.LeaseRecoveryInterval,
                            stoppingToken);

                        if (recovered > 0)
                        {
                            _logger.LogWarning(
                                "Recovered {RecoveredCount} jobs with expired leases.",
                                recovered);
                        }
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(
                            exception,
                            "An error occurred while recovering expired job leases.");
                    }
                }
            }
            catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
            {
            }
        }
    }
}
