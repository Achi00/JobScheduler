# JobScheduler

A distributed, database-backed background job scheduler for .NET — built from scratch as a learning project exploring concurrency, distributed locking, and storage-engine design. Conceptually similar to Hangfire, but built independently to understand *why* systems like it are designed the way they are, not to replace them.

Runs on SQL Server. Supports fire-and-forget jobs, delayed jobs, recurring (cron) jobs, automatic retries with exponential backoff, crash recovery via lease expiry, and batch job claiming for throughput.

## Why this exists

This started as a portfolio project and became a deliberate exercise in building the concurrency-safety mechanisms that real job schedulers depend on — instead of assuming they "just work" underneath a library. Every core design decision (locking strategy, fencing tokens, lease recovery, batching) was arrived at by reasoning through a real failure mode first, then validated by killing the process mid-execution and watching the system recover correctly.

## Features

- **Fire-and-forget jobs** — `EnqueueAsync<TPayload>(payload)`
- **Delayed jobs** — `ScheduleAsync<TPayload>(payload, runAt)`
- **Recurring jobs** — cron-based, timezone-aware, via `AddOrUpdateAsync(id, cronExpression, payload, timeZoneId)`
- **Automatic retries** — configurable max attempts, exponential backoff
- **Crash recovery** — a dedicated lease-recovery worker reclaims jobs left behind by workers that crashed or hung mid-execution
- **Batch claiming** — workers claim multiple jobs per database round trip instead of one at a time
- **Pluggable storage** — storage is behind an abstraction (`IJobStore`); SQL Server is the only implementation today, but nothing above the storage layer depends on it directly, currently only SqlServer added
- **No message broker required** — built entirely on database polling + row-level locking; RabbitMQ/Kafka are not dependencies

## Architecture

The solution is split into independent layers, each depending only inward:

```
JobScheduler.Abstractions          - public contracts: Job, JobStatus, IJobHandler<T>, JobNameAttribute
JobScheduler.Core                  - the engine: JobProcessor, JobRegistry, RecurringJobProcessor,
                                      BackgroundJobClient, RecurringJobClient, the hosted workers
JobScheduler.Storage.Abstractions  - storage-provider contracts: IJobStore, IRecurringJobStore, IUnitOfWork
JobScheduler.Storage.SqlServer     - SQL Server implementation: raw-SQL claim queries, EF Core for
                                      everything else
```

`Core` never references a concrete storage provider. A future Postgres or Dapper-based store would implement `Storage.Abstractions` and require zero changes to `Core`.

### The three background workers

| Worker | Responsibility |
|---|---|
| `JobProcessingWorker` | Claims batches of runnable jobs, executes them (bounded concurrency), marks outcomes |
| `RecurringJobSchedulerWorker` | Polls due recurring definitions, enqueues instances, computes next occurrence |
| `LeaseRecoveryWorker` | Reclaims jobs whose worker lease expired (crashed/hung workers) |

Each runs independently at its own - slow-running job never delays recurring-job scheduling or lease recovery, and vice versa.

## How job claiming works

Concurrent workers (possibly across multiple host processes) all compete for the same claimable rows. This is handled entirely at the database layer using SQL Server locking hints - no distributed lock service, no external coordination:

```sql
SELECT TOP (@BatchSize) *
FROM Jobs WITH (UPDLOCK, READPAST, ROWLOCK, READCOMMITTEDLOCK)
WHERE Status IN (Enqueued, Retrying, Scheduled) AND AvailableAt <= @Now
ORDER BY AvailableAt, CreatedAt
```

- `UPDLOCK` — claims the row atomically at read time, closing the gap between "check if available" and "mark as claimed"
- `READPAST` — a worker that finds a row already locked by another worker skips it instead of blocking, so concurrent claims never queue up single-file
- `ROWLOCK` — keeps the lock scoped to individual rows, not pages or the whole table

Ef core does not provide abstractions or methods for this, that is why I manyaly have writen query in `SqlServerJobStoreCommandFactory` class

### Fencing tokens - surviving crashed workers safely

Every claimed job gets an incrementing `LockToken`. Every state-changing write (`MarkSucceeded`, `MarkFailed`, `MarkRetrying`) is conditioned on that exact token still being current:

```sql
UPDATE Jobs SET Status = @Succeeded, ...
WHERE Id = @jobId AND Status = @Processing AND LockToken = @expectedLockToken
```

If a worker crashes or hangs past its lease duration, `LeaseRecoveryWorker` reclaims the job and **increments the token**. If the original worker was only paused (not dead) and wakes up later to report a result, its write matches zero rows — the stale token no longer matches — and the result is safely rejected instead of corrupting whatever the new claimant is doing.

This was validated by actually killing the host process mid-execution of a long-running job and confirming the recovered row's lock token changed and the original worker's (simulated) write would be rejected.

## Getting started

```bash
git clone <repo-url>
cd JobScheduler
dotnet ef database update --project JobScheduler.Storage.SqlServer
```

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddJobScheduler()
    .AddSqlServerJobStorage(connectionString)
    .AddJob<SendEmailJob, SendEmailJobHandler>()
    .AddServer(); // registers the three background workers

using var host = builder.Build();
await host.RunAsync();
```

Enqueue a job from anywhere in the app:

```csharp
public class OrdersController : ControllerBase
{
    private readonly IBackgroundJobClient _jobs;

    public OrdersController(IBackgroundJobClient jobs) => _jobs = jobs;

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(CancellationToken ct)
    {
        var jobId = await _jobs.EnqueueAsync(new SendEmailJob(userId, "order-confirmation"), ct);
        return Accepted(new { JobId = jobId });
    }
}
```

Register a recurring job (typically once, at startup):

```csharp
await recurringJobs.AddOrUpdateAsync(
    recurringJobId: someFixedGuid,
    cronExpression: "0 2 * * *",
    payload: new CleanupJob(),
    timeZoneId: "Asia/Tbilisi");
```

## Configuration

```json
{
  "JobSchedulerOptions": {
    "PollingInterval": "00:00:02",
    "LockDuration": "00:05:00",
    "LeaseRecoveryInterval": "00:01:00",
    "LeaseRecoveryBatchSize": 100,
    "DefaultMaxAttempts": 3,
    "WorkerCount": 2,
    "BatchSize": 10,
    "MaxConcurrencyPerBatch": 5,
    "RecurringCheckInterval": "00:01:00"
  }
}
```

## Performance

Benchmarked by seeding 5,000 no-op jobs and measuring end-to-end throughput on a local SQL Server instance.

| Configuration | Throughput |
|---|---|
| 4 workers, claim one job at a time (original design) | ~194 jobs/sec |
| 1 worker, batch size 10 | ~295 jobs/sec |
| 2 workers, batch size 10 | ~317 jobs/sec |
| 4 workers, batch size 50 | ~321 jobs/sec (plateau) |

**What changed:** the original design issued one database round trip per claimed job. Batch claiming (`TOP (@BatchSize)` instead of `TOP (1)`, with bounded concurrent execution per batch via a semaphore) reduced claim round trips roughly 10x, for a ~60% throughput improvement.

**Where the new ceiling comes from:** measuring `Batch Requests/sec` during a run shows request volume closely matches `(jobs ÷ batch size) + jobs` — the claim path is now batched, but marking a job succeeded is still one round trip per job. Increasing batch size or worker count past this point yields no further gain, because the bottleneck moved, not disappeared. Batching job completion (via a table-valued parameter or grouped `MERGE`) would be the next optimization, and was scoped but deliberately not built — the value of correctly *diagnosing* the new bottleneck was the point of this exercise.

## What's not here

This is a learning project, not a Hangfire replacement. No dashboard UI, no storage backends beyond SQL Server, no job continuations/batches/chains, no distributed cron leader election across multiple hosts. The focus was the correctness of the core engine — claiming, locking, retrying, and recovering — not feature breadth.

## Testing

- Unit tests cover `JobProcessor`, `BackgroundJobClient`, `RecurringJobClient`, and the cron-resolution logic, using mocked storage and a fake `TimeProvider` for deterministic time-based assertions (retry backoff, scheduled-job timing) without real delays.
- Every core failure mode was also validated live against a running SQL Server instance: successful execution, exhausted retries, delayed scheduling, recurring job firing on schedule, and lease recovery after a simulated process crash.
