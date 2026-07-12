using JobScheduler.Abstractions.Jobs.Enums;
using JobScheduler.Abstractions.Jobs.Interfaces;
using JobScheduler.Abstractions.Jobs.Structs;
using JobScheduler.Core.Options;
using JobScheduler.Core.Storage;
using System.Text.Json;

namespace JobScheduler.Core
{
    internal class BackgroundJobClient : IBackgroundJobClient
    {
        private readonly IJobStore _jobStore;
        private readonly JobSchedulerOptions _options;

        public BackgroundJobClient(IJobStore jobStore, JobSchedulerOptions options)
        {
            _jobStore = jobStore;
            _options = options;
        }
        public async Task<JobId> EnqueueAsync<TPayload>(TPayload payload, CancellationToken cancellationToken = default)
        {
            var jobId = JobId.New();

            var job = new JobRecord
            {
                Id = jobId.Value,
                // TODO: later will be using JobNameAttribute
                JobType = typeof(TPayload).FullName!,
                PayloadJson = JsonSerializer.Serialize(payload),
                Status = JobStatus.Enqueued,
                CreatedAt = DateTimeOffset.UtcNow,
                // if AvailableAt is null, means runnable immediately
                AvailableAt = null,
                AttemptCount = 0,
                MaxAttempts = _options.DefaultMaxAttempts
            };
            await _jobStore.CreateAsync(job, cancellationToken);

            return jobId;
        }

        public async Task<JobId> ScheduleAsync<TPayload>(TPayload payload, DateTimeOffset runAt, CancellationToken cancellationToken = default)
        {
            var jobId = JobId.New();

            var now = DateTimeOffset.UtcNow;

            var job = new JobRecord
            {
                Id = jobId.Value,
                // TODO: later will be using JobNameAttribute
                JobType = typeof(TPayload).FullName!,
                PayloadJson = JsonSerializer.Serialize(payload),
                Status = runAt <= now ? JobStatus.Enqueued : JobStatus.Scheduled,
                CreatedAt = now,
                AvailableAt = runAt <= now ? null : runAt.ToUniversalTime(),
                AttemptCount = 0,
                MaxAttempts = 3
            };

            await _jobStore.CreateAsync(job, cancellationToken);

            return jobId;
        }
    }
}
