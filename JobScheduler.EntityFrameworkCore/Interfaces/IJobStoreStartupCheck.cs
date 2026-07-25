namespace JobScheduler.Storage.EntityFrameworkCore.Interfaces
{
    internal interface IJobStoreStartupCheck
    {
        Task CheckAsync(CancellationToken cancellationToken);
    }
}
