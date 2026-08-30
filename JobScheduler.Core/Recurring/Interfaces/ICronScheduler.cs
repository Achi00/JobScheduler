namespace JobScheduler.Core.Recurring.Interfaces
{
    public interface ICronScheduler
    {
        DateTimeOffset GetNextOccurrence(string cronExpression, string timeZoneId, DateTimeOffset fromUtc);
    }
}
