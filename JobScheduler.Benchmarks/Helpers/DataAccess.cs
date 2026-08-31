using JobScheduler.Abstractions.Jobs.Enums;
using Microsoft.Data.SqlClient;

namespace JobScheduler.Benchmarks.Helpers
{
    public static class DataAccess
    {
        // helper method to get values on seeded data with sql connection
        public static async Task<long> GetRemainingJobsAsync(SqlConnection connection, Guid benchmarkId, string jobType, CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();

            command.CommandText = """
                 SELECT COUNT_BIG(*)
                 FROM Jobs
                 WHERE JobType = @JobType
                   AND Status <> @Succeeded
                   AND JSON_VALUE(PayloadJson, '$.BenchmarkId') = @BenchmarkId;
                 """;

            command.Parameters.AddWithValue("@JobType", jobType);
            command.Parameters.AddWithValue(
                "@Succeeded",
                (int)JobStatus.Succeeded);
            command.Parameters.AddWithValue(
                "@BenchmarkId",
                benchmarkId.ToString());

            var result =
                await command.ExecuteScalarAsync(cancellationToken);

            return Convert.ToInt64(result);
        }
    }
}
