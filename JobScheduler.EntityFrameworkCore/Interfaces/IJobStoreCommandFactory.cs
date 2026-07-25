using System.Data.Common;

namespace JobScheduler.Storage.EntityFrameworkCore.Interfaces
{
    // provider only deals with sql text, parameters, server locking, ect...
    public interface IJobStoreCommandFactory
    {
        DbCommand CreateClaimNextRunnableJobCommand(
            DbConnection connection,
            string workerId,
            TimeSpan lockDuration);

        DbCommand CreateRecoverExpiredJobsCommand(
            DbConnection connection,
            int batchSize,
            TimeSpan recoveryDelay);
    }
}
