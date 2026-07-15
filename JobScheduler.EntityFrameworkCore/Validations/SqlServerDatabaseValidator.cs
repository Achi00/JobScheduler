using JobScheduler.EntityFrameworkCore.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace JobScheduler.Storage.EntityFrameworkCore.Validations
{
    internal sealed class SqlServerDatabaseValidator
    {
        private readonly JobSchedulerDbContext _context;
        private readonly ILogger<SqlServerDatabaseValidator> _logger;

        public SqlServerDatabaseValidator(
            JobSchedulerDbContext context,
            ILogger<SqlServerDatabaseValidator> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ValidateAsync(CancellationToken cancellationToken)
        {
            var connection = _context.Database.GetDbConnection();

            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await using var command = connection.CreateCommand();

                command.CommandText = """
                SELECT CAST(is_read_committed_snapshot_on AS bit)
                FROM sys.databases
                WHERE database_id = DB_ID();
                """;

                var result = await command.ExecuteScalarAsync(cancellationToken);

                var isEnabled = result is true;

                if (!isEnabled)
                {
                    _logger.LogWarning(
                        "READ_COMMITTED_SNAPSHOT is disabled for database {Database}. " +
                        "The scheduler remains functional, but increased reader/writer " +
                        "blocking may reduce throughput.",
                        connection.Database);
                }
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }
}
