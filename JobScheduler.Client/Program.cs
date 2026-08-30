using JobScheduler.Abstractions.Jobs.Interfaces;
using JobScheduler.Client.Email.Failure;
using JobScheduler.Client.Email.Success;
using JobScheduler.Client.LockTokenTest;
using JobScheduler.Core.DependencyInjection;
using JobScheduler.Storage.SqlServer.DependencyInjection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// TODO: add CleanupWorker, MetricsWorker... hosted services in future
// TODO: seperate client and server nodes to save resources if no work is to do

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// register my job scheduler DI, using builder pattern
builder.Services
    .AddJobScheduler()
    .AddServer();

builder.Services.AddSqlServerJobStorage(builder.Configuration.GetConnectionString("Default")!);

// add custom job handlers
builder.Services.AddJob<SendEmailJob, SendEmailJobHandler>();
// add failing job handler for testing
builder.Services.AddJob<FailingJob, FailingJobHandler>();
// simulationg delayed handler with lockeduntil is soon to expire
builder.Services.AddJob<SlowJob, SlowJobHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
