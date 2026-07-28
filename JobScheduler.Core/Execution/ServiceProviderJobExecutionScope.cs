using JobScheduler.Core.Execution.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace JobScheduler.Core.Execution
{
    internal sealed class ServiceProviderJobExecutionScope : IJobExecutionScope
    {
        private readonly AsyncServiceScope _scope;

        public ServiceProviderJobExecutionScope(AsyncServiceScope scope) => _scope = scope;

        public IServiceProvider ServiceProvider => _scope.ServiceProvider;

        public ValueTask DisposeAsync() => _scope.DisposeAsync();
    }
}
