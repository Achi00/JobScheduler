namespace JobScheduler.Core.Exceptions
{
    // internal implamentation but public usage, user might want to catch this type of ex
    public sealed class JobExecutorNotFoundException : Exception
    {
        public string JobType { get; }

        public JobExecutorNotFoundException(string jobType)
            : base($"No job executor registered for job type '{jobType}'.")
        {
            JobType = jobType;
        }
    }
}
