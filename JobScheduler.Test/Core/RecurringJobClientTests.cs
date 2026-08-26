using JobScheduler.Client.Email.Success;
using JobScheduler.Core.Clients;
using JobScheduler.Core.Recurring.Interfaces;
using JobScheduler.Core.Resolvers;
using JobScheduler.Storage.Abstractions.RecurringJobs;
using Microsoft.Extensions.Time.Testing;
using Moq;
using System.Text.Json;

namespace JobScheduler.Test.Core
{
    public class RecurringJobClientTests
    {
        private readonly Mock<IRecurringJobStore> _storeMock;
        private readonly Mock<ICronScheduler> _cronSchedulerMock;
        private readonly FakeTimeProvider _timeProvider;

        public RecurringJobClientTests()
        {
            _storeMock = new Mock<IRecurringJobStore>();
            _cronSchedulerMock = new Mock<ICronScheduler>();
            _timeProvider = new FakeTimeProvider();
        }

        private RecurringJobClient CreateClient() => new(_storeMock.Object, _cronSchedulerMock.Object, _timeProvider);

        [Fact]
        public async Task AddOrUpdateAsync_WhenCalled_ShouldCreateRecordWithComputedNextRunAt()
        {
            var expectedNextRunAt = new DateTimeOffset(2026, 1, 1, 1, 0, 0, TimeSpan.Zero);

            _cronSchedulerMock
                .Setup(x => x.GetNextOccurrence("0 * * * *", "UTC", _timeProvider.GetUtcNow()))
                .Returns(expectedNextRunAt);

            RecurringJobRecord? captured = null;

            _storeMock
                .Setup(x => x.AddOrUpdateAsync(
                    It.IsAny<RecurringJobRecord>(),
                    CancellationToken.None
                )).Callback<RecurringJobRecord, CancellationToken>((record, _) => captured = record)
                .Returns(Task.CompletedTask);

            var client = CreateClient();
            var jobId = Guid.NewGuid();

            await client.AddOrUpdateAsync(jobId, "0 * * * *", new SendEmailJob(Guid.NewGuid(), "welcome"), "UTC");

            Assert.NotNull(captured);
            Assert.Equal(jobId, captured.Id);
            Assert.Equal("0 * * * *", captured.CronExpression);
            Assert.Equal("UTC", captured.TimeZoneId);
            Assert.True(captured.IsEnabled);
            Assert.Equal(expectedNextRunAt, captured.NextRunAt);
            Assert.Null(captured.LastRunAt);
        }


        [Fact]
        public async Task AddOrUpdateAsync_ShouldSerializePayload()
        {
            _cronSchedulerMock
                .Setup(x => x.GetNextOccurrence(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
                .Returns(_timeProvider.GetUtcNow().AddHours(1));

            RecurringJobRecord? captured = null;
            _storeMock
                .Setup(x => x.AddOrUpdateAsync(It.IsAny<RecurringJobRecord>(), It.IsAny<CancellationToken>()))
                .Callback<RecurringJobRecord, CancellationToken>((record, _) => captured = record)
                .Returns(Task.CompletedTask);

            var client = CreateClient();
            var originalPayload = new SendEmailJob(Guid.NewGuid(), "welcome");

            await client.AddOrUpdateAsync(Guid.NewGuid(), "0 * * * *", originalPayload, "UTC");

            Assert.NotNull(captured);
            Assert.Equal(JobTypeNameResolver.Resolve<SendEmailJob>(), captured!.JobType);

            var deserialized = JsonSerializer.Deserialize<SendEmailJob>(captured.PayloadJson);
            Assert.Equal(originalPayload, deserialized);
        }

        [Fact]
        public async Task AddOrUpdateAsync_WhenCronExpressionIsInvalid_ShouldThrowAndNotPersist()
        {
            _cronSchedulerMock
               .Setup(x => x.GetNextOccurrence(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
               .Throws(new ArgumentException("Invalid cron expression"));

            var client = CreateClient();

            await Assert.ThrowsAsync<ArgumentException>(() => client.AddOrUpdateAsync(Guid.NewGuid(), "invalid cron", CancellationToken.None));

            _storeMock.Verify(
                x => x.AddOrUpdateAsync(It.IsAny<RecurringJobRecord>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
