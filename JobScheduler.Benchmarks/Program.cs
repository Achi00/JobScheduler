using JobScheduler.Abstractions.Jobs.Interfaces;
using JobScheduler.Benchmarks.Handlers;
using JobScheduler.Benchmarks.Models;
using JobScheduler.Core.DependencyInjection;
using JobScheduler.Storage.SqlServer.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddJobScheduler()
    .AddServer();

builder.Services.AddSqlServerJobStorage("Server=localhost;Database=JobScheduler;Trusted_Connection=True;TrustServerCertificate=True");

builder.Services.AddJob<NoOpJob, NoOpJobHandler>();

using var host = builder.Build();

var client = host.Services.GetRequiredService<IBackgroundJobClient>();

Console.WriteLine("Benchmark host started.");

// seeding
int count = 5000;

Console.WriteLine($"Seeding {count} jobs");

var benchmarkId = Guid.NewGuid();

for (var i = 0; i < count; i++)
{
    await client.EnqueueAsync(
        new NoOpJob(benchmarkId, i));
}

Console.WriteLine("Seeding completed");

await host.StartAsync();

Console.WriteLine("Scheduler started");

// workers should drain jobs in db
var sw = Stopwatch.StartNew();

await host.StopAsync();