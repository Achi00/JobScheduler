using JobScheduler.Abstractions.Jobs.Interfaces;
using JobScheduler.Abstractions.Jobs.Structs;
using JobScheduler.Client.EmailServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JobScheduler.Client.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobController : ControllerBase
    {
        // implamentation comes from core layer
        private readonly IBackgroundJobClient _jobs;

        public JobController(IBackgroundJobClient jobs)
        {
            _jobs = jobs;
        }

        [HttpGet("{jobId:guid}")]
        public async Task<IActionResult> GetJob(Guid jobId, [FromServices] IBackgroundJobReader reader, CancellationToken cancellationToken)
        {
            var job = await reader.GetByIdAsync(JobId.FromGuid(jobId), cancellationToken);

            if (job == null)
            {
                return NotFound();
            }

            return Ok(job);
        }

        [HttpPost("send-email")]
        public async Task<IActionResult> SendEmail(CancellationToken cancellationToken)
        {
            var jobId = await _jobs.EnqueueAsync(
                new SendEmailJob(Guid.NewGuid(), "welcome"),
                cancellationToken);

            return Accepted(new
            {
                JobId = jobId
            });
        }
    }
}
