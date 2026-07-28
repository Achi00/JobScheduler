using JobScheduler.Core.Execution.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace JobScheduler.Core.Execution
{
    internal sealed class ServiceProviderJobExecutionScopeFactory : IJobExecutionScopeFactory
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ServiceProviderJobExecutionScopeFactory(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        public IJobExecutionScope CreateScope() =>
            new ServiceProviderJobExecutionScope(_scopeFactory.CreateAsyncScope());
    }
}
