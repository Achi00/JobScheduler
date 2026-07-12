using JobScheduler.Abstractions.Jobs.Contexts;
using JobScheduler.Abstractions.Jobs.Structs;
using JobScheduler.Core.Errors;
using JobScheduler.Core.Options;
using JobScheduler.Core.Registry;
using JobScheduler.Core.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        private readonly JobSchedulerOptions _options;
        private readonly ILogger<JobProcessor> _logger;

        public JobProcessor(
            IJobStore jobStore, 
            JobRegistry jobRegistry, 
            IServiceScopeFactory scopeFactory,
            JobSchedulerOptions options,
            ILogger<JobProcessor> logger)
        {
            _jobStore = jobStore;
            _jobRegistry = jobRegistry;
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        public async Task<bool> TryProcessOneAsync(string workerId, CancellationToken ct)
        {
            // TryClaimNextRunnableJobAsync mark's job as processing state
            var job = await _jobStore.TryClaimNextRunnableJobAsync(workerId, _options.LockDuration, ct);

            if (job is null)
            {
                return false;
            }

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

                _logger.LogInformation("Starting job {JobId} of type {JobType}, attempt {Attempt}", job.Id, job.JobType, job.AttemptCount);

                var succeeded = await _jobStore.MarkSucceededAsync(job.Id, job.LockToken, ct);

                if (!succeeded)
                {
                    _logger.LogWarning("Job {JobId} was not marked as succeeded because lock token did not match", job.Id);
                }
                else
                {
                    _logger.LogInformation("Job {JobId} completed successfully", job.Id);
                }
            }
            catch (Exception ex)
            {
                await HandleFailureAsync(job, ex, ct);
            }

            return true;
        }

        private async Task HandleFailureAsync(JobRecord job, Exception ex, CancellationToken ct)
        {
            var error = JobError.FromException(ex);

            if (job.AttemptCount >= job.MaxAttempts)
            {

                var markedFailure = await _jobStore.MarkFailedAsync(job.Id, job.LockToken, error, ct);

                if (!markedFailure)
                {
                    _logger.LogWarning("Job {JobId} was not marked failed because lock token did not match", job.Id);
                }

                _logger.LogInformation("Job {JobId} marked as failed after {AttemptCount} tryes", job.Id, job.AttemptCount);

                return;
            }

            var delay = GetRetryDelay(job.AttemptCount);

            var markedRetry = await _jobStore.MarkRetryingAsync(job.Id, job.LockToken, error, DateTimeOffset.UtcNow.Add(delay), ct);

            if (!markedRetry)
            {
                _logger.LogWarning("Job {JobId} was not marked Retrying because lock token did not match", job.Id);
            }
        }

        private static TimeSpan GetRetryDelay(int attemptCount)
        {
            return attemptCount switch
            {
                // testing
                1 => TimeSpan.FromSeconds(5),
                2 => TimeSpan.FromSeconds(5),
                3 => TimeSpan.FromSeconds(5),
                _ => TimeSpan.FromSeconds(5) 
                //1 => TimeSpan.FromSeconds(10),
                //2 => TimeSpan.FromMinutes(1),
                //3 => TimeSpan.FromMinutes(5),
                //_ => TimeSpan.FromMinutes(15)
            };
        }
    }
}
