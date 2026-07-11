using JobScheduler.Abstractions.Jobs.Enums;
using JobScheduler.Abstractions.Jobs.Interfaces;
using JobScheduler.Abstractions.Jobs.Structs;
using JobScheduler.Core.Storage;
using System.Text.Json;

namespace JobScheduler.Core
{
    internal class BackgroundJobClient : IBackgroundJobClient
    {
        private readonly IJobStore _jobStore;

        public BackgroundJobClient(IJobStore jobStore)
        {
            _jobStore = jobStore;
        }
        public async Task<Guid> EnqueueAsync<TPayload>(TPayload payload, CancellationToken cancellationToken = default)
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
                AvailableAt = null
            };
            await _jobStore.CreateAsync(job, cancellationToken);

            return jobId.Value;
        }

        public async Task<Guid> ScheduleAsync<TPayload>(TPayload payload, DateTimeOffset runAt, CancellationToken cancellationToken = default)
        {
            var jobId = JobId.New();

            var job = new JobRecord
            {
                Id = jobId.Value,
                // TODO: later will be using JobNameAttribute
                JobType = typeof(TPayload).FullName!,
                PayloadJson = JsonSerializer.Serialize(payload),
                Status = JobStatus.Scheduled,
                CreatedAt = DateTimeOffset.UtcNow,
                AvailableAt = null
            };

            await _jobStore.CreateAsync(job, cancellationToken);

            return jobId.Value;
        }
    }
}
