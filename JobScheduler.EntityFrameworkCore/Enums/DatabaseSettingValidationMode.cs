namespace JobScheduler.Storage.EntityFrameworkCore.Enums
{
    public enum DatabaseSettingValidationMode
    {
        // dont check RCSI
        Ignore,
        // check it and log one warning if desibled
        Warn,
        // check ir and prevent scheduler startup if desibled
        Require
    }
}
