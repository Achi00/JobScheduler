namespace JobScheduler.Abstractions.Jobs.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class JobNameAttribute : Attribute
    {
        public string Name { get; }

        public JobNameAttribute(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Name = name;
        }
    }
}
