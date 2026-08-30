using JobScheduler.Abstractions.Jobs.Enums;
using JobScheduler.Abstractions.Jobs.Interfaces;
using JobScheduler.Abstractions.Jobs.Structs;
using JobScheduler.Core.Options;
using JobScheduler.Core.Resolvers;
using JobScheduler.Storage.Abstractions.Jobs;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace JobScheduler.Core.Clients
{
    internal sealed class BackgroundJobClient : IBackgroundJobClient
    {
        private readonly IJobStore _jobStore;
        private readonly IOptionsMonitor<JobSchedulerOptions> _options;
        private readonly TimeProvider _timeProvider;

        public BackgroundJobClient(IJobStore jobStore, IOptionsMonitor<JobSchedulerOptions> options, TimeProvider timeProvider)
        {
            _jobStore = jobStore;
            _options = options;
            _timeProvider = timeProvider;
        }
        public async Task<JobId> EnqueueAsync<TPayload>(TPayload payload, CancellationToken cancellationToken = default)
        {
            var jobId = JobId.New();
            var now = _timeProvider.GetUtcNow();

            var job = new JobRecord
            {
                Id = jobId.Value,
                JobType = JobTypeNameResolver.Resolve<TPayload>(),
                PayloadJson = JsonSerializer.Serialize(payload),
                Status = JobStatus.Enqueued,
                CreatedAt = now,
                AvailableAt = now,
                AttemptCount = 0,
                MaxAttempts = _options.CurrentValue.DefaultMaxAttempts
            };
            await _jobStore.CreateAsync(job, cancellationToken);

            return jobId;
        }

        public async Task<JobId> ScheduleAsync<TPayload>(TPayload payload, DateTimeOffset runAt, CancellationToken cancellationToken = default)
        {
            var jobId = JobId.New();

            var now = _timeProvider.GetUtcNow();

            var job = new JobRecord
            {
                Id = jobId.Value,
                JobType = JobTypeNameResolver.Resolve<TPayload>(),
                PayloadJson = JsonSerializer.Serialize(payload),
                Status = runAt <= now ? JobStatus.Enqueued : JobStatus.Scheduled,
                CreatedAt = now,
                AvailableAt = runAt <= now ? now : runAt.ToUniversalTime(),
                AttemptCount = 0,
                MaxAttempts = _options.CurrentValue.DefaultMaxAttempts
            };

            await _jobStore.CreateAsync(job, cancellationToken);

            return jobId;
        }
    }
}
