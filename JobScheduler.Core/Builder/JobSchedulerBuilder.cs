using Microsoft.Extensions.DependencyInjection;

namespace JobScheduler.Core.Builder
{
    internal sealed class JobSchedulerBuilder : IJobSchedulerBuilder
    {
        public IServiceCollection Services { get; }

        public JobSchedulerBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }
}
