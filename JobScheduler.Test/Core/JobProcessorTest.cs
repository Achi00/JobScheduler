using JobScheduler.Abstractions.Jobs.Contexts;
using JobScheduler.Abstractions.Jobs.Enums;
using JobScheduler.Core.Enums;
using JobScheduler.Core.Execution;
using JobScheduler.Core.Execution.Interfaces;
using JobScheduler.Core.Options;
using JobScheduler.Core.Registry.Interfaces;
using JobScheduler.Storage.Abstractions.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace JobScheduler.Test.Core
{
    // internal dependencis accessable by InternalsVisibleTo
    public class JobProcessorTest
    {
        private readonly Mock<IJobStore> _jobStoreMock;
        private readonly Mock<IJobExecutionScopeFactory> _executionScopeFactoryMock;
        private readonly Mock<IJobRegistry> _jobRegistryMock;
        private readonly Mock<ILogger<JobProcessor>> _loggerMock;
        private readonly Mock<IOptionsMonitor<JobSchedulerOptions>> _optionsMock;


        private readonly JobSchedulerOptions _options;

        public JobProcessorTest()
        {
            _jobStoreMock = new();
            _executionScopeFactoryMock = new();
            _jobRegistryMock = new();
            _loggerMock = new();

            _options = new JobSchedulerOptions
            {
                // defaults
            };

            _optionsMock = new Mock<IOptionsMonitor<JobSchedulerOptions>>();
            _optionsMock
                .Setup(x => x.CurrentValue)
                .Returns(_options);
        }

        private JobProcessor CreateProcessor()
        {
            var timeProviderMock = new Mock<TimeProvider>();

            return new JobProcessor(
                _jobStoreMock.Object,
                _jobRegistryMock.Object,
                _executionScopeFactoryMock.Object,
                _optionsMock.Object,
                timeProviderMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task TryProcessOneAsync_WhenNoJobIsAvailable_ShouldReturnWithoutProcessing()
        {
            // arrange
            var job = new JobRecord
            {
                Id = Guid.NewGuid(),
                JobType = "SendEmail",
                PayloadJson = "{}",
                Status = JobStatus.Enqueued,
                AttemptCount = 1,
                MaxAttempts = 3,
                CreatedAt = DateTimeOffset.UtcNow,
                AvailableAt = DateTimeOffset.UtcNow
            };
            _jobStoreMock.Setup(x => x.TryClaimNextRunnableJobAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
                    .ReturnsAsync((JobRecord?)null
            );

            var processor = CreateProcessor();

            // act
            await processor.ProcessAsync("worker-1", CancellationToken.None);

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

        [Fact]
        public async Task TryProcessOneAsync_WhenJobIsAvailable_ShouldContinueProcessing()
        {
            // Arrange
            var job = new JobRecord
            {
                Id = Guid.NewGuid(),
                JobType = "SendEmail",
                PayloadJson = "{}",
                Status = JobStatus.Enqueued,
                AttemptCount = 1,
                MaxAttempts = 3,
                CreatedAt = DateTimeOffset.UtcNow,
                AvailableAt = DateTimeOffset.UtcNow
            };

            _jobStoreMock
                .Setup(x => x.TryClaimNextRunnableJobAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            _jobStoreMock
                .Setup(s => s.MarkSucceededAsync(
                    job.Id,
                    job.LockToken,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(JobStateChangeResult.Applied);

            var executor = SetupSuccessfulExecutor();

            var processor = CreateProcessor();

            var result = await processor.ProcessAsync("worker-1", CancellationToken.None);

            Assert.Equal(JobProcessResult.Succeeded, result);

            _jobStoreMock.Verify(
                s => s.MarkSucceededAsync(
                    job.Id, 
                    job.LockToken, 
                    It.IsAny<CancellationToken>()), 
                Times.Once);

            executor.Verify(
                x => x.ExecuteAsync(
                    It.IsAny<IServiceProvider>(),
                    It.IsAny<string>(),
                    It.IsAny<JobExecutionContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task TryProcessOneAsync_WhenExecutionThrowsAndAttemptsRemain_ShouldScheduleRetry()
        {
            var job = new JobRecord
            {
                Id = Guid.NewGuid(),
                JobType = "SendEmail",
                PayloadJson = "{}",
                Status = JobStatus.Enqueued,
                AttemptCount = 1,
                MaxAttempts = 3,
                CreatedAt = DateTimeOffset.UtcNow,
                AvailableAt = DateTimeOffset.UtcNow
            };

            _jobStoreMock
                .Setup(x => x.TryClaimNextRunnableJobAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            _jobStoreMock
                .Setup(x => x.MarkRetryingAsync(
                    job.Id,
                    job.LockToken,
                    It.IsAny<JobError>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(JobStateChangeResult.Applied);

            var executor = SetupThrowingExecutor(new Exception("error"));

            // act
            var processor = CreateProcessor();

            var result = await processor.ProcessAsync(
                "worker-1",
                CancellationToken.None);

            // assert
            _jobStoreMock.Verify(
                x => x.MarkFailedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<long>(),
                    It.IsAny<JobError>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _jobStoreMock.Verify(
                x => x.MarkSucceededAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _jobStoreMock.Verify(
                x => x.MarkRetryingAsync(
                    job.Id,
                    job.LockToken,
                    It.Is<JobError>(e => e.Message == "error" && e.Type == typeof(Exception).FullName),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task TryProcessOneAsync_WhenExecutionThrowsAndMaxAttemptsReached_ShouldMarkFailed()
        {
            var job = new JobRecord
            {
                Id = Guid.NewGuid(),
                JobType = "SendEmail",
                PayloadJson = "{}",
                Status = JobStatus.Enqueued,
                AttemptCount = 3,
                MaxAttempts = 3,
                CreatedAt = DateTimeOffset.UtcNow,
                AvailableAt = DateTimeOffset.UtcNow
            };

            _jobStoreMock
                .Setup(x => x.TryClaimNextRunnableJobAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            _jobStoreMock
                .Setup(x => x.MarkFailedAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<long>(),
                        It.IsAny<JobError>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(JobStateChangeResult.Applied);

            var executor = SetupThrowingExecutor(new Exception("error"));

            _jobStoreMock
                .Setup(x => x.MarkRetryingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<long>(),
                    It.IsAny<JobError>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(JobStateChangeResult.Applied);

            // act
            var processor = CreateProcessor();

            var result = await processor.ProcessAsync(
                "worker-1",
                CancellationToken.None);

            // assert
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
                Times.Once);

            _jobStoreMock.Verify(
                x => x.MarkSucceededAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task TryProcessOneAsync_WhenMarkSucceededReturnsLockTokenMismatch_ShouldReportLostOwnership()
        {
            var job = new JobRecord
            {
                Id = Guid.NewGuid(),
                JobType = "SendEmail",
                PayloadJson = "{}",
                Status = JobStatus.Enqueued,
                AttemptCount = 1,
                MaxAttempts = 3,
                CreatedAt = DateTimeOffset.UtcNow,
                AvailableAt = DateTimeOffset.UtcNow
            };

            // returns job
            _jobStoreMock
                .Setup(x => x.TryClaimNextRunnableJobAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            // tries to update, in this case token mismatch
            _jobStoreMock
                .Setup(x => x.MarkSucceededAsync(
                        job.Id,
                        job.LockToken,
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(JobStateChangeResult.LockTokenMismatch);

            var executor = SetupSuccessfulExecutor();

            var processor = CreateProcessor();

            var result = await processor.ProcessAsync(
                "worker-1",
                CancellationToken.None);

            _jobStoreMock.Verify(
                x => x.MarkSucceededAsync(
                    job.Id,
                    job.LockToken,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            Assert.Equal(JobProcessResult.LostOwnership, result);

            executor.Verify(
                x => x.ExecuteAsync(
                    It.IsAny<IServiceProvider>(),
                    It.IsAny<string>(),
                    It.IsAny<JobExecutionContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task TryProcessOneAsync_WhenMarkRetryingReturnsLockTokenMismatch_ShouldReportLostOwnership()
        {
            var job = new JobRecord
            {
                Id = Guid.NewGuid(),
                JobType = "SendEmail",
                PayloadJson = "{}",
                Status = JobStatus.Enqueued,
                AttemptCount = 1,
                MaxAttempts = 3,
                CreatedAt = DateTimeOffset.UtcNow,
                AvailableAt = DateTimeOffset.UtcNow
            };

            // returns job
            _jobStoreMock
                .Setup(x => x.TryClaimNextRunnableJobAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            _jobStoreMock
                .Setup(x => x.MarkRetryingAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<long>(),
                        It.IsAny<JobError>(),
                        It.IsAny<DateTimeOffset>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(JobStateChangeResult.LockTokenMismatch);

            var executor = SetupThrowingExecutor(new Exception("error"));

            var processor = CreateProcessor();

            var result = await processor.ProcessAsync(
                "worker-1",
                CancellationToken.None);

            _jobStoreMock.Verify(
                x => x.MarkRetryingAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<long>(),
                        It.IsAny<JobError>(),
                        It.IsAny<DateTimeOffset>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            _jobStoreMock.Verify(
                x => x.MarkFailedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<long>(),
                    It.IsAny<JobError>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _jobStoreMock.Verify(
                x => x.MarkSucceededAsync(
                    job.Id,
                    job.LockToken,
                    It.IsAny<CancellationToken>()),
                Times.Never);

            Assert.Equal(JobProcessResult.LostOwnership, result);
        }

        [Fact]
        public async Task TryProcessOneAsync_WhenMarkFailedReturnsLockTokenMismatch_ShouldReportLostOwnership()
        {
            var job = new JobRecord
            {
                Id = Guid.NewGuid(),
                JobType = "SendEmail",
                PayloadJson = "{}",
                Status = JobStatus.Enqueued,
                AttemptCount = 4,
                MaxAttempts = 3,
                CreatedAt = DateTimeOffset.UtcNow,
                AvailableAt = DateTimeOffset.UtcNow
            };

            // returns job
            _jobStoreMock
                .Setup(x => x.TryClaimNextRunnableJobAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            _jobStoreMock
                .Setup(x => x.MarkFailedAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<long>(),
                        It.IsAny<JobError>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(JobStateChangeResult.LockTokenMismatch);

            var executor = SetupThrowingExecutor(new Exception("error"));

            var processor = CreateProcessor();

            var result = await processor.ProcessAsync(
                "worker-1",
                CancellationToken.None);

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
                Times.Once);

            _jobStoreMock.Verify(
                x => x.MarkSucceededAsync(
                    job.Id,
                    job.LockToken,
                    It.IsAny<CancellationToken>()),
                Times.Never);

            Assert.Equal(JobProcessResult.LostOwnership, result);
        }

        [Fact]
        public async Task TryProcessOneAsync_WhenMarkSucceededReturnsNotFound_ShouldReportStateChangeFailed()
        {
            var job = new JobRecord
            {
                Id = Guid.NewGuid(),
                JobType = "SendEmail",
                PayloadJson = "{}",
                Status = JobStatus.Enqueued,
                AttemptCount = 1,
                MaxAttempts = 3,
                CreatedAt = DateTimeOffset.UtcNow,
                AvailableAt = DateTimeOffset.UtcNow
            };

            // returns job
            _jobStoreMock
                .Setup(x => x.TryClaimNextRunnableJobAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            _jobStoreMock
                .Setup(x => x.MarkSucceededAsync(
                    job.Id,
                    job.LockToken,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(JobStateChangeResult.NotFound);
            
            var executor = SetupSuccessfulExecutor();

            var processor = CreateProcessor();

            var result = await processor.ProcessAsync(
                "worker-1",
                CancellationToken.None);

            _jobStoreMock.Verify(
                x => x.MarkSucceededAsync(
                    job.Id,
                    job.LockToken,
                    It.IsAny<CancellationToken>()),
                Times.Once);

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

            Assert.Equal(JobProcessResult.StateChangeFailed, result);

            executor.Verify(
                x => x.ExecuteAsync(
                    It.IsAny<IServiceProvider>(),
                    It.IsAny<string>(),
                    It.IsAny<JobExecutionContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task TryProcessOneAsync_WhenMarkSucceededReturnsInvalidState_ShouldReportStateChangeFailed()
        {
            var job = new JobRecord
            {
                Id = Guid.NewGuid(),
                JobType = "SendEmail",
                PayloadJson = "{}",
                Status = JobStatus.Enqueued,
                AttemptCount = 1,
                MaxAttempts = 3,
                CreatedAt = DateTimeOffset.UtcNow,
                AvailableAt = DateTimeOffset.UtcNow
            };

            // returns job
            _jobStoreMock
                .Setup(x => x.TryClaimNextRunnableJobAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            _jobStoreMock
                .Setup(x => x.MarkSucceededAsync(
                    job.Id,
                    job.LockToken,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(JobStateChangeResult.InvalidState);

            var executor = SetupSuccessfulExecutor();

            var processor = CreateProcessor();

            var result = await processor.ProcessAsync(
                "worker-1",
                CancellationToken.None);

            Assert.Equal(JobProcessResult.StateChangeFailed, result);

            _jobStoreMock.Verify(
                x => x.MarkSucceededAsync(
                    job.Id,
                    job.LockToken,
                    It.IsAny<CancellationToken>()),
                Times.Once);

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

            executor.Verify(
                x => x.ExecuteAsync(
                    It.IsAny<IServiceProvider>(),
                    It.IsAny<string>(),
                    It.IsAny<JobExecutionContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task TryProcessOneAsync_WhenMarkRetryingReturnsNotFound_ShouldReportStateChangeFailed()
        {
            var job = new JobRecord
            {
                Id = Guid.NewGuid(),
                JobType = "SendEmail",
                PayloadJson = "{}",
                Status = JobStatus.Enqueued,
                AttemptCount = 1,
                MaxAttempts = 3,
                CreatedAt = DateTimeOffset.UtcNow,
                AvailableAt = DateTimeOffset.UtcNow
            };

            // returns job
            _jobStoreMock
                .Setup(x => x.TryClaimNextRunnableJobAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            _jobStoreMock
                .Setup(x => x.MarkRetryingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<long>(),
                    It.IsAny<JobError>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(JobStateChangeResult.NotFound);

            var executor = SetupSuccessfulExecutor();

            var processor = CreateProcessor();

            var result = await processor.ProcessAsync(
                "worker-1",
                CancellationToken.None);

            Assert.Equal(JobProcessResult.StateChangeFailed, result);

            _jobStoreMock.Verify(x =>
                x.MarkSucceededAsync(
                    job.Id,
                    job.LockToken,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _jobStoreMock.Verify(
                x => x.MarkRetryingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<long>(),
                    It.IsAny<JobError>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _jobStoreMock.Verify(
                x => x.MarkFailedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<long>(),
                    It.IsAny<JobError>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            executor.Verify(
                x => x.ExecuteAsync(
                    It.IsAny<IServiceProvider>(),
                    It.IsAny<string>(),
                    It.IsAny<JobExecutionContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task TryProcessOneAsync_WhenMarkRetryingReturnsInvalidState_ShouldReportStateChangeFailed()
        {
            var job = new JobRecord
            {
                Id = Guid.NewGuid(),
                JobType = "SendEmail",
                PayloadJson = "{}",
                Status = JobStatus.Enqueued,
                AttemptCount = 1,
                MaxAttempts = 3,
                CreatedAt = DateTimeOffset.UtcNow,
                AvailableAt = DateTimeOffset.UtcNow
            };

            // returns job
            _jobStoreMock
                .Setup(x => x.TryClaimNextRunnableJobAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            _jobStoreMock
                .Setup(x => x.MarkRetryingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<long>(),
                    It.IsAny<JobError>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(JobStateChangeResult.InvalidState);

            var executor = SetupSuccessfulExecutor();

            var processor = CreateProcessor();

            var result = await processor.ProcessAsync(
                "worker-1",
                CancellationToken.None);

            Assert.Equal(JobProcessResult.StateChangeFailed, result);

            executor.Verify(
                x => x.ExecuteAsync(
                    It.IsAny<IServiceProvider>(),
                    It.IsAny<string>(),
                    It.IsAny<JobExecutionContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task TryProcessOneAsync_WhenMarkFailedReturnsNotFound_ShouldReportStateChangeFailed()
        {
            var job = new JobRecord
            {
                Id = Guid.NewGuid(),
                JobType = "SendEmail",
                PayloadJson = "{}",
                Status = JobStatus.Enqueued,
                AttemptCount = 3,
                MaxAttempts = 3,
                CreatedAt = DateTimeOffset.UtcNow,
                AvailableAt = DateTimeOffset.UtcNow
            };

            // returns job
            _jobStoreMock
                .Setup(x => x.TryClaimNextRunnableJobAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            _jobStoreMock
                .Setup(x => x.MarkFailedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<long>(),
                    It.IsAny<JobError>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(JobStateChangeResult.NotFound);

            var executor = SetupThrowingExecutor(new Exception("Mark failed could not found job"));

            var processor = CreateProcessor();

            var result = await processor.ProcessAsync(
                "worker-1",
                CancellationToken.None);

            Assert.Equal(JobProcessResult.StateChangeFailed, result);
        }

        [Fact]
        public async Task TryProcessOneAsync_WhenMarkFailedReturnsInvalidState_ShouldReportStateChangeFailed()
        {
            var job = new JobRecord
            {
                Id = Guid.NewGuid(),
                JobType = "SendEmail",
                PayloadJson = "{}",
                Status = JobStatus.Enqueued,
                AttemptCount = 3,
                MaxAttempts = 3,
                CreatedAt = DateTimeOffset.UtcNow,
                AvailableAt = DateTimeOffset.UtcNow
            };

            // returns job
            _jobStoreMock
                .Setup(x => x.TryClaimNextRunnableJobAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            _jobStoreMock
                .Setup(x => x.MarkFailedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<long>(),
                    It.IsAny<JobError>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(JobStateChangeResult.InvalidState);

            var executor = SetupThrowingExecutor(new Exception("Mark failed returned invalid state"));

            var processor = CreateProcessor();

            var result = await processor.ProcessAsync(
                "worker-1",
                CancellationToken.None);

            Assert.Equal(JobProcessResult.StateChangeFailed, result);

            executor.Verify(
                x => x.ExecuteAsync(
                    It.IsAny<IServiceProvider>(),
                    It.IsAny<string>(),
                    It.IsAny<JobExecutionContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // helper
        // success
        private Mock<IJobExecutor> SetupSuccessfulExecutor()
        {
            var executor = new Mock<IJobExecutor>();

            executor
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<IServiceProvider>(),
                    It.IsAny<string>(),
                    It.IsAny<JobExecutionContext>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var scope = new Mock<IJobExecutionScope>();

            scope.SetupGet(x => x.ServiceProvider)
                 .Returns(Mock.Of<IServiceProvider>());

            _executionScopeFactoryMock
                .Setup(x => x.CreateScope())
                .Returns(scope.Object);

            _jobRegistryMock
                .Setup(x => x.GetExecutor("SendEmail"))
                .Returns(executor.Object);

            return executor;
        }

        // fail/retry
        private Mock<IJobExecutor> SetupThrowingExecutor(Exception? exception = null)
        {
            exception ??= new InvalidOperationException("Simulated failure");

            var executor = new Mock<IJobExecutor>();

            executor
                .Setup(x => x.ExecuteAsync(
                    It.IsAny<IServiceProvider>(),
                    It.IsAny<string>(),
                    It.IsAny<JobExecutionContext>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            var scope = new Mock<IJobExecutionScope>();

            scope.SetupGet(x => x.ServiceProvider)
                 .Returns(Mock.Of<IServiceProvider>());

            _executionScopeFactoryMock
                .Setup(x => x.CreateScope())
                .Returns(scope.Object);

            _jobRegistryMock
                .Setup(x => x.GetExecutor("SendEmail"))
                .Returns(executor.Object);

            return executor;
        }
    }
}
