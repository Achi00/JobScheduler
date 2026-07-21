using JobScheduler.Client.Email.Failure;
using JobScheduler.Client.Email.Success;
using JobScheduler.Core.DependencyInjection;
using JobScheduler.EntityFrameworkCore.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// register my job scheduler DI
builder.Services.AddJobSchedulerCore();

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

app.Run();
