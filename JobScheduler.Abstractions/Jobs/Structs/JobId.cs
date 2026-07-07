namespace JobScheduler.Abstractions.Jobs.Structs
{
    public readonly record struct JobId(Guid Value)
    {
        public static JobId New() => new(Guid.NewGuid());
        public static JobId FromGuid(Guid value) => new(value);
        public override string ToString() => Value.ToString();
    }
}
