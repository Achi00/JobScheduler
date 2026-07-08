using JobScheduler.Core.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JobScheduler.Core.HostedServices
{
    internal sealed class JobWorkerHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _workerId = $"{Environment.MachineName}-{Guid.NewGuid():N}";

        public JobWorkerHostedService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var processor = scope.ServiceProvider.GetRequiredService<JobProcessor>();

                var processed = await processor.TryProcessOneAsync(_workerId, TimeSpan.FromSeconds(5), stoppingToken);

                if (!processed)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
        }
    }
}
