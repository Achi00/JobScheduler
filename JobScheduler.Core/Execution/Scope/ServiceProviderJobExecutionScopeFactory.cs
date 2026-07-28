using JobScheduler.Core.Execution.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace JobScheduler.Core.Execution.Scope
{
    // creates scope in one place, moved from JobProcessor so it does not know anything about DI or job states and oposite
    internal sealed class ServiceProviderJobExecutionScopeFactory : IJobExecutionScopeFactory
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ServiceProviderJobExecutionScopeFactory(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        public IJobExecutionScope CreateScope() =>
            new ServiceProviderJobExecutionScope(_scopeFactory.CreateAsyncScope());
    }
}
