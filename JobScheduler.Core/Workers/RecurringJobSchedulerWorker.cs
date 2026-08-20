using JobScheduler.Core.Execution;
using JobScheduler.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobScheduler.Core.Workers
{
    /*
     * uses single running loop, no parallelism 
     */
    internal sealed class RecurringJobSchedulerWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptionsMonitor<JobSchedulerOptions> _options;
        private readonly ILogger<RecurringJobSchedulerWorker> _logger;

        public RecurringJobSchedulerWorker(IServiceScopeFactory scopeFactory, IOptionsMonitor<JobSchedulerOptions> options, ILogger<RecurringJobSchedulerWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Recurring job scheduler started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var processor = scope.ServiceProvider.GetRequiredService<RecurringJobProcessor>();

                    var dispatched = await processor.DispatchDueJobsAsync(stoppingToken);

                    if (dispatched > 0)
                    {
                        _logger.LogInformation("Dispatched {Count} recurring job instance(s).", dispatched);
                    }
                }
                // stopping toket passed
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Recurring job scheduler failed.");
                }

                await Task.Delay(_options.CurrentValue.RecurringCheckInterval, stoppingToken);
            }

            _logger.LogInformation("Recurring job scheduler stopped.");
        }
    }
}
