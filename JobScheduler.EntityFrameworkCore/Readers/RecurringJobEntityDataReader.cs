using JobScheduler.EntityFrameworkCore.Entities;
using System.Data.Common;

namespace JobScheduler.EntityFrameworkCore.Readers
{
    internal static class RecurringJobEntityDataReader
    {
        public static RecurringJobEntity Read(DbDataReader reader)
        {
            var id = reader.GetOrdinal("Id");
            var jobType = reader.GetOrdinal("JobType");
            var payloadJson = reader.GetOrdinal("PayloadJson");
            var cronExpression = reader.GetOrdinal("CronExpression");
            var timeZoneId = reader.GetOrdinal("TimeZoneId");
            var isEnabled = reader.GetOrdinal("IsEnabled");
            var nextRunAt = reader.GetOrdinal("NextRunAt");
            var lastRunAt = reader.GetOrdinal("LastRunAt");

            return new RecurringJobEntity
            {
                Id = reader.GetGuid(id),
                JobType = reader.GetString(jobType),
                PayloadJson = reader.GetString(payloadJson),
                CronExpression = reader.GetString(cronExpression),
                TimeZoneId = reader.GetString(timeZoneId),
                IsEnabled = reader.GetBoolean(isEnabled),

                NextRunAt = reader.IsDBNull(nextRunAt)
                    ? null
                    : reader.GetFieldValue<DateTimeOffset>(nextRunAt),

                LastRunAt = reader.IsDBNull(lastRunAt)
                    ? null
                    : reader.GetFieldValue<DateTimeOffset>(lastRunAt)
            };
        }
    }

}
