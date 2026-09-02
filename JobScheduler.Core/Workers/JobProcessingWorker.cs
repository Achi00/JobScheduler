using JobScheduler.Core.Enums;
using JobScheduler.Core.Execution;
using JobScheduler.Core.Options;
using JobScheduler.Storage.Abstractions.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobScheduler.Core.HostedServices
{
    // finds or claims runnable job, executes it, marks it as succeeded/failed/retrying ect..
    // handles multiple Worker loops, worker: 0 -> scope -> DbContext -> Processor ...
    // TryClaimNextRunnableJobAsync makes this safer because of db locking, READPAST/UPDLOCK...
    internal sealed class JobProcessingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IJobStore _jobStore;
        // using IOptionsMonitor for live config update, this service is always singleton, otherwise IOptions
        private readonly IOptionsMonitor<JobSchedulerOptions> _options;
        private readonly ILogger<JobProcessingWorker> _logger;

        public JobProcessingWorker(
            IServiceScopeFactory scopeFactory,
            IJobStore jobStore,
            IOptionsMonitor<JobSchedulerOptions> options,
            ILogger<JobProcessingWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _jobStore = jobStore;
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

        // single running loop, id incremented as enumerable goes, each loop gets different id, will be used for LockedBy
        private async Task ProcessLoopAsync(int workerNumber,CancellationToken stoppingToken)
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
                    await using var scope = _scopeFactory.CreateAsyncScope();

                    var processor = scope.ServiceProvider.GetRequiredService<JobProcessor>();

                    // TryClaimNextRunnableJobAsync mark's job as processing state
                    var job = await _jobStore.TryClaimNextRunnableJobAsync(workerId, _options.CurrentValue.BatchSize, _options.CurrentValue.LockDuration, ct);

                    if (job is null)
                    {
                        return Task.FromResult(JobProcessResult.NoJobAvailable);
                    }

                    var result = await processor.ProcessAsync(
                            job,
                            stoppingToken);

                    if (result == JobProcessResult.NoJobAvailable)
                    {
                        // adding small randomized delay to avoid thundering herd / synchronized polling problem, causes large load/spikes for syncronized jobs
                        await DelayWithJitterAsync(stoppingToken);
                    }
                }
                // just cancel
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (stoppingToken.IsCancellationRequested)
                {
                    // host is tearing down mid-iteration,expected shutdown race between many workers, can dispose objectt when some worker is mid cycle
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Job worker {WorkerId} failed.",
                        workerId);

                    try
                    {
                        await DelayWithJitterAsync(stoppingToken);
                    }
                    catch (OperationCanceledException)
                        when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }

            _logger.LogInformation(
                "Job worker {WorkerId} stopped.",
                workerId);
        }

        private Task DelayWithJitterAsync(CancellationToken cancellationToken)
        {
            var baseDelay = _options.CurrentValue.PollingInterval;

            var maxJitterMilliseconds = Math.Max(
                1,
                (int)baseDelay.TotalMilliseconds / 5);

            var jitterMilliseconds =
                Random.Shared.Next(0, maxJitterMilliseconds + 1);

            return Task.Delay(
                baseDelay + TimeSpan.FromMilliseconds(jitterMilliseconds),
                cancellationToken);
        }
    }
}
