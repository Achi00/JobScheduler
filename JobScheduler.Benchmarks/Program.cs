using JobScheduler.Abstractions.Jobs.Interfaces;
using JobScheduler.Benchmarks.Handlers;
using JobScheduler.Benchmarks.Helpers;
using JobScheduler.Benchmarks.Models;
using JobScheduler.Core.DependencyInjection;
using JobScheduler.Core.Options;
using JobScheduler.Storage.SqlServer.DependencyInjection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Diagnostics;

const string connectionString =
    "Server=localhost;Database=JobScheduler;Trusted_Connection=True;TrustServerCertificate=True";

var builder = Host.CreateApplicationBuilder(args);

// options
builder.Services.Configure<JobSchedulerOptions>(
    builder.Configuration.GetSection("JobSchedulerOptions")
);

// register services from code
builder.Services
    .AddJobScheduler()
    .AddServer();

// storage
builder.Services.AddSqlServerJobStorage(connectionString);

// custom job model + handlers
builder.Services.AddJob<NoOpJob, NoOpJobHandler>();

using var host = builder.Build();

var client = host.Services.GetRequiredService<IBackgroundJobClient>();

var options = host.Services.GetRequiredService<IOptions<JobSchedulerOptions>>();


Console.WriteLine("Benchmark host started.");

const int count = 5000;

Console.WriteLine($"Seeding {count} jobs...");

var benchmarkId = Guid.NewGuid();

for (var i = 0; i < count; i++)
{
    await client.EnqueueAsync(
        new NoOpJob(benchmarkId, i));
}

Console.WriteLine("Seeding completed.");

await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();

var stopwatch = Stopwatch.StartNew();

await host.StartAsync();

while (true)
{
    var remaining =
        await DataAccess.GetRemainingJobsAsync(
            connection,
            benchmarkId,
            "JobScheduler.Benchmarks.Models.NoOpJob",
            CancellationToken.None);

    Console.WriteLine($"Remaining: {remaining}");

    if (remaining == 0)
    {
        break;
    }

    await Task.Delay(100);
}

stopwatch.Stop();

var elapsed = stopwatch.Elapsed;
var jobsPerSecond = count / elapsed.TotalSeconds;

Console.WriteLine($"Elapsed: {elapsed}");
Console.WriteLine($"Jobs/sec: {jobsPerSecond:N0}");
Console.WriteLine($"Worker Count: {options.Value.WorkerCount}");