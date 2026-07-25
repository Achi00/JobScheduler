using JobScheduler.Core.Options;
using JobScheduler.Storage.Abstractions.Jobs;
using Microsoft.Extensions.DependencyInjection;
using JobScheduler.Core.Registry;
using JobScheduler.Core.Execution;
using Moq;

namespace JobScheduler.Test.Core
{
    // internal dependencis accessable by InternalsVisibleTo
    internal class JobProcessorTest
    {
        private readonly Mock<IJobStore> _jobStoreMock;
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly Mock<IJobRegistry> _jobRegistryMock;

        private readonly JobSchedulerOptions _options;

        public JobProcessorTest()
        {
            _jobStoreMock = new();
            _scopeFactoryMock = new();
            _jobRegistryMock = new();

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
                _options);
        }
    }
}
