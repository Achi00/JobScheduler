using Cronos;
using JobScheduler.Core.Recurring.Interfaces;

namespace JobScheduler.Core.Recurring
{
    internal sealed class CronosScheduler : ICronScheduler
    {
        public DateTimeOffset GetNextOccurrence(string cronExpression, string timeZoneId, DateTimeOffset fromUtc)
        {
            CronExpression cron;
            // parse expression
            try
            {
                cron = CronExpression.Parse(cronExpression);
            }
            catch (CronFormatException ex)
            {
                throw new ArgumentException($"Invalid cron expression '{cronExpression}'.", nameof(cronExpression), ex);
            }

            // try get timezon by provided id
            TimeZoneInfo zone;

            try
            {
                zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException ex)
            {
                throw new ArgumentException($"Unknown timezone id {timeZoneId}", nameof(timeZoneId), ex);
            }
            // returns nullable struct
            var next = cron.GetNextOccurrence(fromUtc, zone);

            if (next is null)
            {
                throw new InvalidOperationException($"Cron expression '{cronExpression}' has no future occurrences.");
            }

            return next.Value;
        }
    }
}
