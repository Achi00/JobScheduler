using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JobScheduler.Abstractions.Interfaces
{
    // user inplamented
    public interface IJobHandler<in TPayload>
    {
        Task HandleAsync(TPayload payload, CancellationToken cancellationToken);
    }
}
