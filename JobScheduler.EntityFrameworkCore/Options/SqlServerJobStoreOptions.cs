using JobScheduler.Storage.EntityFrameworkCore.Enums;

namespace JobScheduler.Storage.EntityFrameworkCore.Options
{
    public sealed class SqlServerJobStoreOptions
    {
        public SnapshotIsolationRequirement ReadCommittedSnapshotRequirement { get; set; }
            = SnapshotIsolationRequirement.Warn;
    }
}
