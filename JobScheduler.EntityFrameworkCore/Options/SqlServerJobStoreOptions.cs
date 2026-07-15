using JobScheduler.Storage.EntityFrameworkCore.Enums;

namespace JobScheduler.Storage.EntityFrameworkCore.Options
{
    public sealed class SqlServerJobStoreOptions
    {
        public DatabaseSettingValidationMode ReadCommittedSnapshotRequirement { get; set; }
            = DatabaseSettingValidationMode.Warn;
    }
}
