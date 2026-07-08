using JobScheduler.Abstractions.Jobs.Contexts;
using JobScheduler.Abstractions.Jobs.Structs;
using JobScheduler.Core.Registry;
using JobScheduler.Core.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace JobScheduler.Core.Execution
{
    // job orcestrator, controls job lifecycle
    // TODO: claim job -> mark processing -> create JobExecutionContext -> find executor -> mark succeed/failed
    // FAILED: catch ex -> increment attempt -> if attempt < maxAttempts = mark retrying/scheduled, else mark failed
    internal sealed class JobProcessor
    {
        private readonly IJobStore _jobStore;
        private readonly JobRegistry _jobRegistry;
        private readonly IServiceScopeFactory _scopeFactory;

        public JobProcessor(IJobStore jobStore, JobRegistry jobRegistry, IServiceScopeFactory scopeFactory)
        {
            _jobRegistry = jobRegistry;
            _jobRegistry = jobRegistry;
            _scopeFactory = scopeFactory;
        }

        public async Task<bool> TryProcessOneAsync(string workerId, CancellationToken ct)
        {
            var job = await _jobStore.GetNextRunnableJobAsync(ct);

            if (job is null)
            {
                return false;
            }

            // mark job process as in processing state
            await _jobStore.MarkProcessingAsync(job.Id, ct);

            await using var scope = _scopeFactory.CreateAsyncScope();

            var context = new JobExecutionContext
            (
                jobId: JobId.FromGuid(job.Id),
                jobType: job.JobType,
                attemptCount: job.AttemptCount,
                createdAt: job.CreatedAt,
                startedAt: DateTimeOffset.UtcNow
            );

            try
            {
                var executor = _jobRegistry.GetExecutor(job.JobType);

                await executor.ExecuteAsync(scope.ServiceProvider, job.PayloadJson, context, ct);

                await _jobStore.MarkSucceededAsync(job.Id, ct);
            }
            catch (Exception ex)
            {
                await HandleFailureAsync(job, ex, ct);
            }

            return true;
        }

        private async Task HandleFailureAsync(JobRecord job, Exception ex, CancellationToken ct)
        {
            var nextAttemptCount = job.AttemptCount + 1;

            if (nextAttemptCount >= job.MaxAttempts)
            {
                await _jobStore.MarkFailedAsync(job.Id, ex.ToString(), ct);

                return;
            }

            var delay = GetRetryDelay(nextAttemptCount);

            await _jobStore.MarkRetryingAsync(job.Id, job.LockToken, ex.ToString(), DateTimeOffset.UtcNow.Add(delay), ct);
        }

        private static TimeSpan GetRetryDelay(int attemptCount)
        {
            return attemptCount switch
            {
                1 => TimeSpan.FromSeconds(10),
                2 => TimeSpan.FromMinutes(1),
                3 => TimeSpan.FromMinutes(5),
                _ => TimeSpan.FromMinutes(15)
            };
        }
    }
}
