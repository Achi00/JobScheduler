using JobScheduler.Core.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Core.HostedServices
{
    // finds or claims runnable job, executes it, marks it as succeeded/failed/retrying ect..
    internal sealed class JobWorkerHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<JobWorkerHostedService> _logger;
        private readonly string _workerId = $"{Environment.MachineName}-{Guid.NewGuid():N}";

        public JobWorkerHostedService(IServiceScopeFactory scopeFactory, ILogger<JobWorkerHostedService> logger)
        {
            _scopeFactory = scopeFactory;
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

                    var processed = await processor.TryProcessOneAsync(_workerId, TimeSpan.FromSeconds(5), stoppingToken);

                    if (!processed)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Job worker {WorkedId} failed", _workerId);

                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("Job worker {WorkerId} stopped", _workerId);
        }
    }
}
