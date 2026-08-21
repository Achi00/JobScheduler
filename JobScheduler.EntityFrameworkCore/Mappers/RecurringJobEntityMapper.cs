using JobScheduler.EntityFrameworkCore.Entities;
using JobScheduler.Storage.Abstractions.RecurringJobs;

namespace JobScheduler.EntityFrameworkCore.Mappers
{
    internal static class RecurringJobEntityMapper
    {
        public static RecurringJobRecord ToRecord(RecurringJobEntity entity)
        {
            return new RecurringJobRecord(
                entity.Id,
                entity.JobType,
                entity.PayloadJson,
                entity.CronExpression,
                entity.TimeZoneId,
                entity.IsEnabled,
                entity.NextRunAt,
                entity.LastRunAt);
        }

        public static RecurringJobEntity ToEntity(RecurringJobRecord record)
        {
            return new RecurringJobEntity
            {
                Id = record.Id,
                JobType = record.JobType,
                PayloadJson = record.PayloadJson,
                CronExpression = record.CronExpression,
                TimeZoneId = record.TimeZoneId,
                IsEnabled = record.IsEnabled,
                NextRunAt = record.NextRunAt,
                LastRunAt = record.LastRunAt
            };
        }

        // needed because AddOrUpdateAsync must mutate tracked entity for updates
        // not replace it with a new detached instance
        public static void ApplyTo(RecurringJobEntity entity, RecurringJobRecord record)
        {
            entity.JobType = record.JobType;
            entity.PayloadJson = record.PayloadJson;
            entity.CronExpression = record.CronExpression;
            entity.TimeZoneId = record.TimeZoneId;
            entity.IsEnabled = record.IsEnabled;
            // NextRunAt/LastRunAt intentionally not overwritten
            // those are owned by GetDueForUpdateAsync/UpdateNextRunAsync's claim flow,
            // not by a user calling AddOrUpdateRecurring() to change the cron schedule
        }
    }
}
