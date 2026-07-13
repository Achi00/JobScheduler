namespace JobScheduler.Core.Enums
{
    internal enum JobStateChangeResult
    {
        Applied = 1,

        // job row does not exist anymore
        NotFound = 2,

        // worker tried to update a job it no longer owns
        LockTokenMismatch = 3,

        // job exists but is not in the expected state
        InvalidState = 4
    }
}
