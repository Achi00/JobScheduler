using JobScheduler.Core.Execution.Interfaces;
using JobScheduler.Core.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace JobScheduler.Core.Execution.Resolvers
{
    internal class JobExecutorResolver : IJobExecutorResolver
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public JobExecutorResolver(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }
        public IJobExecutor Resolve(string jobType)
        {
            using var scope = _scopeFactory.CreateScope();

            var registry = scope.ServiceProvider.GetRequiredService<IJobRegistry>();

            return registry.GetExecutor(jobType);
        }
    }
}
