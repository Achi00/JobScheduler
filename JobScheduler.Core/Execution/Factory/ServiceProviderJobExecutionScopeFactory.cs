using JobScheduler.Core.Execution.Interfaces;
using JobScheduler.Core.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace JobScheduler.Core.Execution.Factory
{
    internal class ServiceProviderJobExecutionScopeFactory : IJobExecutionScopeFactory
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IJobRegistry _registry;

        public ServiceProviderJobExecutionScopeFactory(IServiceScopeFactory scopeFactory, IJobRegistry registry)
        {
            _scopeFactory = scopeFactory;
            _registry = registry;
        }

        public IJobRegistry CreateScope()
        {
            var scope = _scopeFactory.CreateAsyncScope();
            return new ServiceProviderJobExecutionScope(scope, _registry);
        }

        internal sealed class ServiceProviderJobExecutionScope : IJobRegistry
        {
            private readonly AsyncServiceScope _scope;
            private readonly IJobRegistry _registry;

            public ServiceProviderJobExecutionScope(AsyncServiceScope scope, IJobRegistry registry)
            {
                _scope = scope;
                _registry = registry;
            }

            public IJobExecutor GetExecutor(string jobType)
            {
                var executorType = _registry.GetExecutorType(jobType); // registry maps jobType -> Type
                return (IJobExecutor)_scope.ServiceProvider.GetRequiredService(executorType);
            }

            public ValueTask DisposeAsync() => _scope.DisposeAsync();
        }
    }
}
