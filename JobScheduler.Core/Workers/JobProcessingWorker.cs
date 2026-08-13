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
    internal sealed class JobProcessingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        // using IOptionsMonitor for live config update, this service is always singleton, otherwise IOptions
        private readonly IOptionsMonitor<JobSchedulerOptions> _options;
        private readonly ILogger<JobProcessingWorker> _logger;
        private readonly string _workerId = $"{Environment.MachineName}-{Guid.NewGuid():N}";

        public JobProcessingWorker(
            IServiceScopeFactory scopeFactory,
            IOptionsMonitor<JobSchedulerOptions> options,
            ILogger<JobProcessingWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var workerCount = _options.CurrentValue.WorkerCount;

            _logger.LogInformation(
                "Starting {WorkerCount} job processing workers.",
                workerCount);

            var workers = Enumerable.Range(0, workerCount)
                .Select(workerNumber =>
                    ProcessLoopAsync(workerNumber, stoppingToken))
                .ToArray();

            await Task.WhenAll(workers);
        }

        private async Task ProcessLoopAsync(
            int workerNumber,
            CancellationToken stoppingToken)
        {
            var workerId =
                $"{Environment.MachineName}-worker-{workerNumber}-{Guid.NewGuid():N}";

            _logger.LogInformation(
                "Job worker {WorkerId} started.",
                workerId);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope =
                        _scopeFactory.CreateAsyncScope();

                    var processor =
                        scope.ServiceProvider.GetRequiredService<JobProcessor>();

                    var result =
                        await processor.TryProcessOneAsync(
                            workerId,
                            stoppingToken);

                    if (result == JobProcessResult.NoJobAvailable)
                    {
                        await Task.Delay(
                            _options.CurrentValue.PollingInterval,
                            stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Job worker {WorkerId} failed.",
                        workerId);

                    await Task.Delay(
                        _options.CurrentValue.PollingInterval,
                        stoppingToken);
                }
            }

            _logger.LogInformation(
                "Job worker {WorkerId} stopped.",
                workerId);
        }
    }
}
