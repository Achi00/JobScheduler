namespace JobScheduler.Storage.Abstractions.Jobs
{
    public sealed record JobError(string Message, string Type, string Details)
    {
        public static JobError FromException(Exception exception)
        {
            return new JobError
            (
                Message: exception.Message,
                Type: exception.GetType().FullName ?? exception.GetType().Name,
                Details: exception.ToString()
            );
        }
    }
}
