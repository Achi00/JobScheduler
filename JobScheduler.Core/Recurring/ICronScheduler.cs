namespace JobScheduler.Core.Recurring
{
    public interface ICronScheduler
    {
        DateTimeOffset GetNextOccurrence(string cronExpression, string timeZoneId, DateTimeOffset fromUtc);
    }
}
