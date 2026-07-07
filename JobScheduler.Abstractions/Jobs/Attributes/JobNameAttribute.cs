namespace JobScheduler.Abstractions.Jobs.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class JobNameAttribute : Attribute
    {
        public JobNameAttribute(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Name = name;
        }

        public string Name { get; }
    }
}
