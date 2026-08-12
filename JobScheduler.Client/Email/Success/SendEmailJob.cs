using JobScheduler.Abstractions.Jobs.Attributes;

namespace JobScheduler.Client.Email.Success
{
    [JobName("SendEmail")]
    public sealed record SendEmailJob(Guid UserId, string TemplateName);
}
