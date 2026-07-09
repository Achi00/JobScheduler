namespace JobScheduler.Client.EmailServices
{
    public sealed record SendEmailJob(Guid UserId, string TemplateName);
}
