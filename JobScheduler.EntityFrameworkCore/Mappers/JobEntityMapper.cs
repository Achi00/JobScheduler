using JobScheduler.EntityFrameworkCore.Entities;
using JobScheduler.Storage.Abstractions.Jobs;

namespace JobScheduler.Storage.EntityFrameworkCore.Mappers
{
    internal class JobEntityMapper
    {
        public static JobRecord ToRecord(JobEntity entity)
        {
            return new JobRecord
            {
                Id = entity.Id,
                JobType = entity.JobType,
                PayloadJson = entity.PayloadJson,
                Status = entity.Status,
                AttemptCount = entity.AttemptCount,
                MaxAttempts = entity.MaxAttempts,
                CreatedAt = entity.CreatedAt,
                AvailableAt = entity.AvailableAt,
                StartedAt = entity.StartedAt,
                CompletedAt = entity.CompletedAt,
                LockedUntil = entity.LockedUntil,
                LockedBy = entity.LockedBy,
                LockToken = entity.LockToken,
                LastErrorMessage = entity.LastErrorMessage,
                LastErrorType = entity.LastErrorType,
                LastErrorDetails = entity.LastErrorDetails,
                IsDeleted = entity.IsDeleted,
                DeletedAt = entity.DeletedAt
            };
        }

        public static JobEntity ToEntity(JobRecord record)
        {
            return new JobEntity
            {
                Id = record.Id,
                JobType = record.JobType,
                PayloadJson = record.PayloadJson,
                Status = record.Status,
                AttemptCount = record.AttemptCount,
                MaxAttempts = record.MaxAttempts,
                CreatedAt = record.CreatedAt,
                AvailableAt = record.AvailableAt,
                StartedAt = record.StartedAt,
                CompletedAt = record.CompletedAt,
                LockedUntil = record.LockedUntil,
                LockedBy = record.LockedBy,
                LockToken = record.LockToken,
                LastErrorMessage = record.LastErrorMessage,
                LastErrorType = record.LastErrorType,
                LastErrorDetails = record.LastErrorDetails,
                IsDeleted = record.IsDeleted,
                DeletedAt = record.DeletedAt
            };
        }
    }
}
