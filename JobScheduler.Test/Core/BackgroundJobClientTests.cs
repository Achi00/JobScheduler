using JobScheduler.Abstractions.Jobs.Enums;
using JobScheduler.Client.Email.Success;
using JobScheduler.Core;
using JobScheduler.Core.Options;
using JobScheduler.Core.Resolvers;
using JobScheduler.Storage.Abstractions.Jobs;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using System.Text.Json;

namespace JobScheduler.Test.Core
{
    public class BackgroundJobClientTests
    {
        private readonly Mock<IJobStore> _jobStoreMock;
        private readonly JobSchedulerOptions _options;
        private readonly FakeTimeProvider _timeProvider;

        public BackgroundJobClientTests()
        {
            _jobStoreMock = new();
            _timeProvider = new FakeTimeProvider();
            _timeProvider.SetUtcNow(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
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

            var client = new BackgroundJobClient(_jobStoreMock.Object, Options.Create(_options), _timeProvider);

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
            Assert.Equal(JobTypeNameResolver.Resolve<SendEmailJob>(), captured.JobType);
            Assert.False(string.IsNullOrWhiteSpace(captured.PayloadJson));
            Assert.True(captured.AvailableAt <= DateTimeOffset.UtcNow);
            Assert.NotEqual(Guid.Empty, captured.Id);
        }

        [Fact]
        public async Task EnqueueAsync_ShouldCreateJobWithEnqueuedStatusAndZeroAttempts()
        {
            JobRecord? captured = null;

            _jobStoreMock
                .Setup(x => x.CreateAsync(
                        It.IsAny<JobRecord>(),
                        It.IsAny<CancellationToken>()))
                .Callback<JobRecord, CancellationToken>((job, _) => captured = job)
                .Returns(Task.CompletedTask);

            var client = new BackgroundJobClient(_jobStoreMock.Object, Options.Create(_options), _timeProvider);

            await client.EnqueueAsync<SendEmailJob>(new SendEmailJob(Guid.NewGuid(), "welcome"));

            Assert.NotNull(captured);
            Assert.Equal(JobStatus.Enqueued, captured!.Status);
            Assert.Equal(0, captured.AttemptCount);
            Assert.Null(captured.CompletedAt);
            Assert.Null(captured.LockedBy);

            _jobStoreMock.Verify(x =>
                x.CreateAsync(
                    It.IsAny<JobRecord>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EnqueueAsync_ShouldSerializePayload()
        {
            JobRecord? captured = null;

            _jobStoreMock
                .Setup(x => x.CreateAsync(
                        It.IsAny<JobRecord>(),
                        It.IsAny<CancellationToken>()))
                .Callback<JobRecord, CancellationToken>((job, _) => captured = job)
                .Returns(Task.CompletedTask);

            var client = new BackgroundJobClient(_jobStoreMock.Object, Options.Create(_options), _timeProvider);

            var originalPayload = new SendEmailJob(Guid.NewGuid(), "welcome");

            await client.EnqueueAsync<SendEmailJob>(originalPayload);

            Assert.NotNull(captured);
            Assert.False(string.IsNullOrWhiteSpace(captured!.PayloadJson));

            // deserialize back to object
            var deserialized = JsonSerializer.Deserialize<SendEmailJob>(captured.PayloadJson);

            Assert.Equal(originalPayload, deserialized);
        }

        [Fact]
        public async Task EnqueueAsync_ShouldSetJobTypeFromHandlerRegistration()
        {
            JobRecord? captured = null;

            _jobStoreMock
                .Setup(x => x.CreateAsync(
                        It.IsAny<JobRecord>(),
                        It.IsAny<CancellationToken>()))
                .Callback<JobRecord, CancellationToken>((job, _) => captured = job)
                .Returns(Task.CompletedTask);

            var client = new BackgroundJobClient(_jobStoreMock.Object, Options.Create(_options), _timeProvider);

            var originalPayload = new SendEmailJob(Guid.NewGuid(), "welcome");

            await client.EnqueueAsync<SendEmailJob>(originalPayload);

            Assert.NotNull(captured);
            Assert.Equal(captured.JobType, JobTypeNameResolver.Resolve<SendEmailJob>());
        }

        [Fact]
        public async Task EnqueueAsync_ShouldSetAvailableAtToNow()
        {
            JobRecord? captured = null;

            _jobStoreMock
                .Setup(x => x.CreateAsync(
                        It.IsAny<JobRecord>(),
                        It.IsAny<CancellationToken>()))
                .Callback<JobRecord, CancellationToken>((job, _) => captured = job)
                .Returns(Task.CompletedTask);

            var client = new BackgroundJobClient(_jobStoreMock.Object, Options.Create(_options), _timeProvider);

            var before = DateTimeOffset.UtcNow;

            await client.EnqueueAsync<SendEmailJob>(new SendEmailJob(Guid.NewGuid(), "welcome"));

            var after = DateTimeOffset.UtcNow;

            Assert.NotNull(captured);
            Assert.Equal(_timeProvider.GetUtcNow(), captured!.AvailableAt);
        }

        //[Fact]
        //public async Task EnqueueAsync_ShouldUseJobNameAttributeWhenPresent()
        //{
        //    JobRecord? captured = null;

        //    _jobStoreMock
        //        .Setup(x => x.CreateAsync(
        //                It.IsAny<JobRecord>(),
        //                It.IsAny<CancellationToken>()))
        //        .Callback<JobRecord, CancellationToken>((job, _) => captured = job)
        //        .Returns(Task.CompletedTask);

        //    var client = new BackgroundJobClient(_jobStoreMock.Object, Options.Create(_options), _timeProvider);

        //    var before = DateTimeOffset.UtcNow;

        //    await client.EnqueueAsync<SendEmailJob>(new SendEmailJob(Guid.NewGuid(), "welcome"));

        //    var after = DateTimeOffset.UtcNow;

        //    Assert.NotNull(captured);
        //    Assert.Equal(_timeProvider.GetUtcNow(), captured!.AvailableAt);
        //}

        //[Fact]
        //public async Task EnqueueAsync_ShouldFallBackToFullNameWhenAttributeMissing()
        //{
        //    // some payload type with no [JobName]
        //    //await client.EnqueueAsync<UnannotatedJob>(new UnannotatedJob(...));
        //    //Assert.Equal(typeof(UnannotatedJob).FullName, captured!.JobType);
        //}
    }
}
