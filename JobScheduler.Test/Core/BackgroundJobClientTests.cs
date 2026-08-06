using JobScheduler.Core.Options;
using JobScheduler.Storage.Abstractions.Jobs;
using Microsoft.Extensions.Options;
using Moq;

namespace JobScheduler.Test.Core
{
    public class BackgroundJobClientTests
    {
        private readonly Mock<IJobStore> _jobStoreMock;
        private readonly IOptions<JobSchedulerOptions> _options;

        public BackgroundJobClientTests()
        {
            _jobStoreMock = new();
        }
    }
}
