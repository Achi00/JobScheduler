using Microsoft.Extensions.DependencyInjection;

namespace JobScheduler.Core.Builder
{
    public interface IJobSchedulerBuilder
    {
        IServiceCollection Services { get; }
    }
}
