using System.Data.Common;

namespace JobScheduler.EntityFrameworkCore.Interfaces
{
    public interface IRecurringJobStoreCommandFactory
    {
        DbCommand CreateGetDueForUpdateCommand(DbConnection connection, DateTimeOffset now, int batchSize);
    }
}
