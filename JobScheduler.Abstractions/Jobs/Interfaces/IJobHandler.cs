namespace JobScheduler.Abstractions.Jobs.Interfaces
{
    // user inplamented
    public interface IJobHandler<in TPayload>
    {
        Task HandleAsync(TPayload payload, CancellationToken cancellationToken);
    }
}
