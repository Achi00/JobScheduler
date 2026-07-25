using JobScheduler.Core.Execution;
using JobScheduler.Core.Options;
using JobScheduler.Core.Registry;
using JobScheduler.Storage.Abstractions.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace JobScheduler.Test.Core
{
    // internal dependencis accessable by InternalsVisibleTo
    public class JobProcessorTest
    {
        private readonly Mock<IJobStore> _jobStoreMock;
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly Mock<IJobRegistry> _jobRegistryMock;
        private readonly Mock<ILogger<JobProcessor>> _loggerMock;


        private readonly JobSchedulerOptions _options;

        public JobProcessorTest()
        {
            _jobStoreMock = new();
            _scopeFactoryMock = new();
            _jobRegistryMock = new();
            _loggerMock = new();

            _options = new JobSchedulerOptions
            {
                // defaults
            };
        }

        private JobProcessor CreateProcessor()
        {
            return new JobProcessor(
                _jobStoreMock.Object,
                _jobRegistryMock.Object,
                _scopeFactoryMock.Object,
                Options.Create(_options),
                _loggerMock.Object);
        }

        [Fact]
        public async Task TryProcessOneAsync_WhenNoJobIsAvailable_ShouldReturnWithoutProcessing()
        {
            // arrange
            _jobStoreMock.Setup(x => x.TryClaimNextRunnableJobAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
                    .ReturnsAsync((JobRecord?)null
            );

            var processor = CreateProcessor();

            // act
            await processor.TryProcessOneAsync("worker-test-1", CancellationToken.None);

            // assert
            _jobStoreMock.Verify(
                x => x.MarkSucceededAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _jobStoreMock.Verify(
                x => x.MarkRetryingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<long>(),
                    It.IsAny<JobError>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _jobStoreMock.Verify(
                x => x.MarkFailedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<long>(),
                    It.IsAny<JobError>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
