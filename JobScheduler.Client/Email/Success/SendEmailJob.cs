namespace JobScheduler.Client.Email.Success
{
    public sealed record SendEmailJob(Guid UserId, string TemplateName);
}
