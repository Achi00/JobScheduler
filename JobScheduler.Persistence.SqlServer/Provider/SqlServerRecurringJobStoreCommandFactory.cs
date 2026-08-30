using JobScheduler.EntityFrameworkCore.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace JobScheduler.Storage.SqlServer.Provider
{
    // follows SqlServerJobStoreCommandFactory philosohpy
    internal sealed class SqlServerRecurringJobStoreCommandFactory : IRecurringJobStoreCommandFactory
    {
        public DbCommand CreateGetDueForUpdateCommand(DbConnection connection, DateTimeOffset now, int batchSize)
        {
            ArgumentNullException.ThrowIfNull(connection);

            var command = connection.CreateCommand();

            command.CommandText = $@"
               SELECT TOP (@batchSize) * FROM RecurringJob WITH (READPAST, UPDLOCK, ROWLOCK)
                WHERE IsEnabled = 1 AND NextRunAt <= @now
                ORDER BY NextRunAt ASC;
            ";

            command.Parameters.Add(new SqlParameter("@batchSize", batchSize));
            command.Parameters.Add(new SqlParameter("@now", now));

            return command;
        }
    }
}
