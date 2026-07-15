using JobScheduler.Storage.EntityFrameworkCore.Interfaces;
using JobScheduler.Storage.EntityFrameworkCore.Options;
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

        public Task CheckAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
