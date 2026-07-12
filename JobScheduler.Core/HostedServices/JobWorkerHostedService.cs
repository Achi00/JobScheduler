using JobScheduler.Core.Enums;
using JobScheduler.Core.Execution;
using JobScheduler.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobScheduler.Core.HostedServices
{
    // finds or claims runnable job, executes it, marks it as succeeded/failed/retrying ect..
    internal sealed class JobWorkerHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        // using IOptionsMonitor for live config update, this service is always singleton, otherwise IOptions
        private readonly IOptionsMonitor<JobSchedulerOptions> _options;
        private readonly ILogger<JobWorkerHostedService> _logger;
        private readonly string _workerId = $"{Environment.MachineName}-{Guid.NewGuid():N}";

        public JobWorkerHostedService(
            IServiceScopeFactory scopeFactory,
            IOptionsMonitor<JobSchedulerOptions> options,
            ILogger<JobWorkerHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Job worker {WorkerId} started.", _workerId);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();

                    var processor = scope.ServiceProvider.GetRequiredService<JobProcessor>();

                    var result = await processor.TryProcessOneAsync(_workerId, stoppingToken);

                    if (result == JobProcessResult.NoJobAvailable)
                    {
                        await Task.Delay(
                            _options.CurrentValue.PollingInterval,
                            stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Job worker {WorkedId} failed", _workerId);

                    await Task.Delay(_options.CurrentValue.PollingInterval, stoppingToken);
                }
            }

            _logger.LogInformation("Job worker {WorkerId} stopped", _workerId);
        }
    }
}
