using JobScheduler.Core.Recurring;
using JobScheduler.Storage.Abstractions.Jobs;
using JobScheduler.Storage.Abstractions.RecurringJobs;

namespace JobScheduler.Core.Execution
{
    internal sealed class RecurringJobProcessor
    {
        private readonly IRecurringJobStore _recurringStore;
        private readonly IJobStore _jobStore;
        private readonly ICronScheduler _cronScheduler;
        private readonly TimeProvider _timeProvider;
    }
}
