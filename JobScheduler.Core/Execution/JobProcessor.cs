using JobScheduler.Abstractions.Jobs.Contexts;
using JobScheduler.Abstractions.Jobs.Structs;
using JobScheduler.Core.Enums;
using JobScheduler.Core.Errors;
using JobScheduler.Core.Options;
using JobScheduler.Core.Registry;
using JobScheduler.Core.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
            IOptions<JobSchedulerOptions> options,
            ILogger<JobProcessor> logger)
        {
            _jobStore = jobStore;
            _jobRegistry = jobRegistry;
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<JobProcessResult> TryProcessOneAsync(string workerId, CancellationToken ct)
        {
            // TryClaimNextRunnableJobAsync mark's job as processing state
            var job = await _jobStore.TryClaimNextRunnableJobAsync(workerId, _options.LockDuration, ct);

            if (job is null)
            {
                return JobProcessResult.NoJobAvailable;
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
                
                _logger.LogInformation(
                    "Starting job {JobId} of type {JobType}, attempt {Attempt}",
                    job.Id, 
                    job.JobType, 
                    job.AttemptCount
                );

                await executor.ExecuteAsync(scope.ServiceProvider, job.PayloadJson, context, ct);

                var result = await _jobStore.MarkSucceededAsync(job.Id, job.LockToken, ct);

                return HandleSucceededTransitionResult(job, result);
            }
            catch (Exception ex)
            {
                return await HandleFailureAsync(job, ex, ct);
            }
        }

        // TODO: do all private methods more organized way!!!!
        private async Task<JobProcessResult> HandleFailureAsync(JobRecord job, Exception ex, CancellationToken ct)
        {
            var error = JobError.FromException(ex);

            if (job.AttemptCount >= job.MaxAttempts)
            {
                var result = await _jobStore.MarkFailedAsync(job.Id, job.LockToken, error, ct);
                
                return HandleFailedTransitionResult(job, result);
            }

            var delay = GetRetryDelay(job.AttemptCount);
            var availableAt = DateTimeOffset.UtcNow.Add(delay);

            var retryResult = await _jobStore
                .MarkRetryingAsync(
                    job.Id, 
                    job.LockToken, 
                    error, 
                    DateTimeOffset.UtcNow.Add(delay), 
                    ct
                );

            return HandleRetryTransitionResult(job, retryResult, availableAt);
        }

        private JobProcessResult HandleSucceededTransitionResult(
            JobRecord job,
            JobStateChangeResult result)
        {
            switch (result)
            {
                case JobStateChangeResult.Applied:
                    _logger.LogInformation(
                        "Job {JobId} completed successfully.",
                        job.Id);

                    return JobProcessResult.Succeeded;

                case JobStateChangeResult.LockTokenMismatch:
                    _logger.LogWarning(
                        "Job {JobId} executed successfully, but this worker no longer owns it. Success state update skipped.",
                        job.Id);

                    return JobProcessResult.LostOwnership;

                case JobStateChangeResult.NotFound:
                    _logger.LogWarning(
                        "Job {JobId} executed successfully, but the job record no longer exists.",
                        job.Id);

                    return JobProcessResult.StateChangeFailed;

                case JobStateChangeResult.InvalidState:
                    _logger.LogWarning(
                        "Job {JobId} executed successfully, but it was not in Processing state when marking succeeded.",
                        job.Id);

                    return JobProcessResult.StateChangeFailed;

                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }
        }

        private JobProcessResult HandleRetryTransitionResult(
            JobRecord job,
            JobStateChangeResult result,
            DateTimeOffset availableAt)
        {
            switch (result)
            {
                case JobStateChangeResult.Applied:
                    _logger.LogWarning(
                        "Job {JobId} failed on attempt {Attempt}/{MaxAttempts}. Retrying at {AvailableAt}.",
                        job.Id,
                        job.AttemptCount,
                        job.MaxAttempts,
                        availableAt);

                    return JobProcessResult.ScheduledRetry;

                case JobStateChangeResult.LockTokenMismatch:
                    _logger.LogWarning(
                        "Job {JobId} execution failed, but this worker no longer owns it. Retry state update skipped.",
                        job.Id);

                    return JobProcessResult.LostOwnership;

                case JobStateChangeResult.NotFound:
                    _logger.LogWarning(
                        "Job {JobId} execution failed, but the job record no longer exists.",
                        job.Id);

                    return JobProcessResult.StateChangeFailed;

                case JobStateChangeResult.InvalidState:
                    _logger.LogWarning(
                        "Job {JobId} execution failed, but it was not in Processing state when marking retrying.",
                        job.Id);

                    return JobProcessResult.StateChangeFailed;

                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }
        }

        private JobProcessResult HandleFailedTransitionResult(
            JobRecord job,
            JobStateChangeResult result)
        {
            switch (result)
            {
                case JobStateChangeResult.Applied:
                    _logger.LogInformation(
                        "Job {JobId} marked as failed after {AttemptCount} attempts.",
                        job.Id,
                        job.AttemptCount);

                    return JobProcessResult.FailedPermanently;

                case JobStateChangeResult.LockTokenMismatch:
                    _logger.LogWarning(
                        "Job {JobId} reached max attempts, but this worker no longer owns it. Failed state update skipped.",
                        job.Id);

                    return JobProcessResult.LostOwnership;

                case JobStateChangeResult.NotFound:
                    _logger.LogWarning(
                        "Job {JobId} reached max attempts, but the job record no longer exists.",
                        job.Id);

                    return JobProcessResult.StateChangeFailed;

                case JobStateChangeResult.InvalidState:
                    _logger.LogWarning(
                        "Job {JobId} reached max attempts, but it was not in Processing state when marking failed.",
                        job.Id);

                    return JobProcessResult.StateChangeFailed;

                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
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
