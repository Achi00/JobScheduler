using JobScheduler.Abstractions.Jobs.Interfaces;
using JobScheduler.Client.Email.Failure;
using JobScheduler.Client.Email.Success;
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


// TESTING
using (var scope = app.Services.CreateScope())
{
    var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobClient>();

    await recurringJobs.AddOrUpdateAsync(
        recurringJobId: new Guid("11111111-1111-1111-1111-111111111111"),
        // runs every minute, NextRunAt should be now + 1 minute
        cronExpression: "* * * * *",
        payload: new SendEmailJob(Guid.NewGuid(), "recurring-digest"),
        timeZoneId: "UTC");
}

app.Run();
