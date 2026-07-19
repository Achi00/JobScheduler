using JobScheduler.Persistence.SqlServer.Enums;

namespace JobScheduler.Persistence.SqlServer.Options
{
    public sealed class SqlServerJobStoreOptions
    {
        public RcsiValidationMode RcsiValidation { get; set; } = RcsiValidationMode.Warn;
    }
}
