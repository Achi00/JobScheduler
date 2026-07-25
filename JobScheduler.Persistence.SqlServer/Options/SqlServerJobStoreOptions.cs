using JobScheduler.Storage.SqlServer.Enums;

namespace JobScheduler.Storage.SqlServer.Options
{
    public sealed class SqlServerJobStoreOptions
    {
        public RcsiValidationMode RcsiValidation { get; set; } = RcsiValidationMode.Warn;
    }
}
