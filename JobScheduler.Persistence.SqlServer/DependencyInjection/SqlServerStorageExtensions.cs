using JobScheduler.EntityFrameworkCore.DependencyInjection;
using JobScheduler.EntityFrameworkCore.Persistence.Context;
using JobScheduler.Storage.EntityFrameworkCore.Interfaces;
using JobScheduler.Storage.SqlServer.Options;
using JobScheduler.Storage.SqlServer.Provider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JobScheduler.Storage.SqlServer.DependencyInjection
{
    public static class SqlServerStorageExtensions
    {
        public static IServiceCollection AddSqlServerJobStorage(
            this IServiceCollection services,
            string connectionString,
            Action<SqlServerJobStoreOptions>? configure = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            services.AddEntityFrameworkJobStorage(options => options.UseSqlServer(connectionString));
            
            services.AddOptions<SqlServerJobStoreOptions>();

            if (configure is not null)
            {
                services.Configure(configure);
            }

            services.AddSingleton<IJobStoreCommandFactory, SqlServerJobStoreCommandFactory>();

            return services;
        }
    }
}
