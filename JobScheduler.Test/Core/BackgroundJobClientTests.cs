using JobScheduler.Abstractions.Jobs.Enums;
using JobScheduler.Client.Email.Success;
using JobScheduler.Core;
using JobScheduler.Core.Options;
using JobScheduler.Storage.Abstractions.Jobs;
using Microsoft.Extensions.Options;
using Moq;

namespace JobScheduler.Test.Core
{
    public class BackgroundJobClientTests
    {
        private readonly Mock<IJobStore> _jobStoreMock;
        private readonly JobSchedulerOptions _options;

        public BackgroundJobClientTests()
        {
            _jobStoreMock = new();
            _options = new JobSchedulerOptions
            {
                // defaults
            };
        }

        [Fact]
        public async Task EnqueueAsync_WhenCalled_ShouldCreateEnqueuedJob()
        {
            JobRecord? captured = null;

            _jobStoreMock
                .Setup(x => x.CreateAsync(
                        It.IsAny<JobRecord>(),
                        It.IsAny<CancellationToken>()))
                .Callback<JobRecord, CancellationToken>((job, _) => captured = job)
                .Returns(Task.CompletedTask);

            var client = new BackgroundJobClient(_jobStoreMock.Object, Options.Create(_options));

            await client.EnqueueAsync<SendEmailJob>(new SendEmailJob(Guid.NewGuid(), "welcome"));

            _jobStoreMock.Verify(x =>
                x.CreateAsync(
                    It.Is<JobRecord>(job =>
                        job.Status == JobStatus.Enqueued),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            Assert.NotNull(captured);
            Assert.Equal(JobStatus.Enqueued, captured!.Status);
            Assert.Equal(0, captured.AttemptCount);
            Assert.Equal(typeof(SendEmailJob).FullName, captured.JobType);
            Assert.False(string.IsNullOrWhiteSpace(captured.PayloadJson));
            Assert.True(captured.AvailableAt <= DateTimeOffset.UtcNow);
            Assert.NotEqual(Guid.Empty, captured.Id);
        }
    }
}
