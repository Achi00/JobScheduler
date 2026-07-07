using JobScheduler.Core.Execution;

namespace JobScheduler.Core.Registry
{
    // maps types to executors
    internal sealed class JobRegistry
    {
        private Dictionary<string, IJobExecutor> _executors;

        public JobRegistry(IEnumerable<IJobExecutor> executors)
        {
            _executors = executors.ToDictionary(x => x.JobType, StringComparer.OrdinalIgnoreCase);
        }

        public IJobExecutor GetExecutor(string jobType)
        {
            if (!_executors.TryGetValue(jobType, out var executor))
            {
                throw new InvalidOperationException($"No job executor registered for job type '{jobType}'");
            }

            return executor;
        }
    }
}
