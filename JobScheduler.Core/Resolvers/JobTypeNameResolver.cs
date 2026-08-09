using JobScheduler.Abstractions.Jobs.Attributes;
using System.Reflection;

namespace JobScheduler.Core.Resolvers
{
    internal static class JobTypeNameResolver
    {
        public static string Resolve<TPayload>()
        {
            var attr = typeof(TPayload).GetCustomAttribute<JobNameAttribute>();
            // fallback on old default if attribute was not applied
            return attr?.Name ?? typeof(TPayload).FullName!;  
        }
    }
}
