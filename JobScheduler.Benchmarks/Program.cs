using JobScheduler.Abstractions.Jobs.Interfaces;
using JobScheduler.Benchmarks.Handlers;
using JobScheduler.Benchmarks.Models;
using JobScheduler.Core.DependencyInjection;
using JobScheduler.Storage.SqlServer.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddJobScheduler()
    .AddServer();

builder.Services.AddSqlServerJobStorage("Server=localhost;Database=JobScheduler;Trusted_Connection=True;TrustServerCertificate=True");

builder.Services.AddJob<NoOpJob, NoOpJobHandler>();

var services = new ServiceCollection();

using var host = builder.Build();

await host.StartAsync();

var client = host.Services.GetRequiredService<IBackgroundJobClient>();

Console.WriteLine("Benchmark host started.");

await host.StopAsync();