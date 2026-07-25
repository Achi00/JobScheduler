using JobScheduler.Abstractions.Jobs.Enums;
using JobScheduler.EntityFrameworkCore.Entities;
using System.Data.Common;

namespace JobScheduler.Storage.EntityFrameworkCore.Readers
{
    internal static class JobEntityDataReader
    {
        // helper reader class to avoid boilerplate mapping and parsing of DbDataReader object
        public static JobEntity Read(DbDataReader reader)
        {
            return new JobEntity
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                JobType = reader.GetString(reader.GetOrdinal("JobType")),
                PayloadJson = reader.GetString(
                    reader.GetOrdinal("PayloadJson")),

                Status = (JobStatus)reader.GetInt32(
                    reader.GetOrdinal("Status")),

                AttemptCount = reader.GetInt32(
                    reader.GetOrdinal("AttemptCount")),

                MaxAttempts = reader.GetInt32(
                    reader.GetOrdinal("MaxAttempts")),

                CreatedAt = reader.GetFieldValue<DateTimeOffset>(
                    reader.GetOrdinal("CreatedAt")),

                AvailableAt = GetNullableDateTimeOffset(
                    reader,
                    "AvailableAt"),

                StartedAt = GetNullableDateTimeOffset(
                    reader,
                    "StartedAt"),

                CompletedAt = GetNullableDateTimeOffset(
                    reader,
                    "CompletedAt"),

                LockedBy = GetNullableString(
                    reader,
                    "LockedBy"),

                LockedUntil = GetNullableDateTimeOffset(
                    reader,
                    "LockedUntil"),

                LockToken = reader.GetInt64(
                    reader.GetOrdinal("LockToken")),

                UpdatedAt = reader.GetFieldValue<DateTimeOffset>(
                    reader.GetOrdinal("UpdatedAt")),

                LastErrorMessage = GetNullableString(
                    reader,
                    "LastErrorMessage"),

                LastErrorType = GetNullableString(
                    reader,
                    "LastErrorType"),

                LastErrorDetails = GetNullableString(
                    reader,
                    "LastErrorDetails")
            };
        }

        private static string? GetNullableString(
            DbDataReader reader,
            string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);

            return reader.IsDBNull(ordinal)
                ? null
                : reader.GetString(ordinal);
        }

        private static DateTimeOffset? GetNullableDateTimeOffset(
            DbDataReader reader,
            string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);

            return reader.IsDBNull(ordinal)
                ? null
                : reader.GetFieldValue<DateTimeOffset>(ordinal);
        }
    }
}
