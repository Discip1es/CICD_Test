using MyApp.Application.Abstractions;
using MyApp.Application.Services;
using MyApp.Api;
using MyApp.Api.Endpoints;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging via Serilog.
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Health checks: liveness + readiness.
builder.Services.AddHealthChecks();

// OpenAPI (Swagger) in Development/Staging.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// RFC 7807 problem details for domain errors.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiErrorHandler>();

// Application services (composition root).
builder.Services.AddSingleton<ICalculationHistoryStore, InMemoryCalculationHistoryStore>();
builder.Services.AddSingleton<CalculatorService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.MapCalculatorEndpoints();

app.Run();

/// <summary>Entry point marker (used by WebApplicationFactory in integration tests).</summary>
public partial class Program;
