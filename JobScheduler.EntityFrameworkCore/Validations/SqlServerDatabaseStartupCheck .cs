using JobScheduler.Storage.EntityFrameworkCore.Enums;
using JobScheduler.Storage.EntityFrameworkCore.Interfaces;
using JobScheduler.Storage.EntityFrameworkCore.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace JobScheduler.Storage.EntityFrameworkCore.Validations
{
    internal sealed class SqlServerDatabaseStartupCheck : IJobStoreStartupCheck
    {
        private readonly SqlServerJobStoreOptions _options;
        private readonly SqlServerConnectionOptions _connectionOptions;
        private readonly ILogger<SqlServerDatabaseStartupCheck> _logger;

        public SqlServerDatabaseStartupCheck(
            SqlServerJobStoreOptions options, 
            SqlServerConnectionOptions connectionOptions, 
            ILogger<SqlServerDatabaseStartupCheck> logger)
        {
            _options = options;
            _connectionOptions = connectionOptions;
            _logger = logger;
        }

        public async Task CheckAsync(CancellationToken cancellationToken)
        {
            if (_options.ReadCommittedSnapshotRequirement == DatabaseSettingValidationMode.Ignore)
            {
                return;
            }

            var isEnabled = await IsReadCommittedSnapshotEnabledAsync(cancellationToken);

            if (isEnabled)
            {
                _logger.LogDebug("READ_COMMITTED_SNAPSHOT is enabled for the scheduler database.");

                return;
            }

            const string message =
                "READ_COMMITTED_SNAPSHOT is disabled for the scheduler database. " +
                "The scheduler can continue operating, but reader/writer blocking " +
                "may reduce throughput. Consider enabling it with: " +
                "ALTER DATABASE [DatabaseName] SET READ_COMMITTED_SNAPSHOT ON;";

            switch (_options.ReadCommittedSnapshotRequirement)
            {
                case DatabaseSettingValidationMode.Warn:
                    _logger.LogWarning("{Message}", message);
                    break;

                case DatabaseSettingValidationMode.Require:
                    throw new InvalidOperationException(message);

                case DatabaseSettingValidationMode.Ignore:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(_options.ReadCommittedSnapshotRequirement));
            }
        }

        private async Task<bool> IsReadCommittedSnapshotEnabledAsync(CancellationToken cancellationToken)
        {
            await using var connection = new SqlConnection(_connectionOptions.ConnectionString);

            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT is_read_committed_snapshot_on
                FROM sys.databases
                WHERE database_id = DB_ID();
            """;

            var result = await command.ExecuteScalarAsync(cancellationToken);

            return result != null && result != DBNull.Value && Convert.ToBoolean(result);
        }
    }
}
