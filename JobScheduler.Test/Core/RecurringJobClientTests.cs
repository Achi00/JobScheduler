using JobScheduler.Client.Email.Success;
using JobScheduler.Core.Clients;
using JobScheduler.Core.Recurring.Interfaces;
using JobScheduler.Core.Resolvers;
using JobScheduler.Storage.Abstractions.RecurringJobs;
using Microsoft.Extensions.Time.Testing;
using Moq;

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

        private RecurringJobClient CreateCleint() => new(_storeMock.Object, _cronSchedulerMock.Object, _timeProvider);

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

            var client = CreateCleint();
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
    }
}
