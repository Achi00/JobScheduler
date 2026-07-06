using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JobScheduler.Abstractions.Interfaces
{
    public interface IJobHandler<in TPayload>
    {
        Task HandleAsync(TPayload payload);
    }

    public interface IJobHandler<TPayload> : IJobHandler
    {
        Task ExecuteAsync(TPayload payload, CancellationToken ct);

        // default interface method bridges generic to non-generic
        Task IJobHandler.ExecuteAsync(string payloadJson, CancellationToken ct)
        {
            var payload = JsonSerializer.Deserialize<TPayload>(payloadJson)!;
            return ExecuteAsync(payload, ct);
        }
    }
}
