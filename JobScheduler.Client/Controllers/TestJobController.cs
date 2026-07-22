using JobScheduler.Abstractions.Jobs.Enums;
using JobScheduler.Storage.Abstractions.Jobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JobScheduler.Client.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestJobController : ControllerBase
    {
        private readonly IJobStore _jobStore;

        public TestJobController(IJobStore jobStore)
        {
            _jobStore = jobStore;
        }

        [HttpPost]
        public async Task<IActionResult> Create()
        {
            var job = new JobRecord
            {
                Id = Guid.NewGuid(),
                JobType = "SendEmail",
                PayloadJson = "{}",
                Status = JobStatus.Enqueued,
                AttemptCount = 0,
                MaxAttempts = 3,
                AvailableAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _jobStore.CreateAsync(job, CancellationToken.None);

            return Ok(job.Id);
        }
    }
}
