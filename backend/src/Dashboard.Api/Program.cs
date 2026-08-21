using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Dashboard.Application;
using Dashboard.Infrastructure;
using Dashboard.Infrastructure.Persistence;

const string DevClientCorsPolicy = "DevClient";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddCors(options =>
{
    options.AddPolicy(DevClientCorsPolicy, policy =>
    {
        // The frontend dev server origin, pinned in frontend/vite.config.ts
        // and start-app.bat. Not Vite's default 5173, which collides with any
        // other Vite app already running. Revisit once the frontend has a
        // real deployment target — this is intentionally local-dev-only.
        policy.WithOrigins("http://localhost:5180")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors(DevClientCorsPolicy);

if (app.Environment.IsDevelopment())
{
    // Local convenience only: applies whatever migrations already exist
    // (see README for generating them) and seeds a handful of sample
    // categories/metrics/months so the dashboard has something to show.
    // Never runs outside Development, and the seeder itself is a no-op if
    // any categories already exist.
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
    await dbContext.Database.MigrateAsync();
    await DevelopmentDataSeeder.SeedAsync(dbContext);
}

app.MapControllers();

// Liveness: no dependencies, always responds if the process and DI
// container came up successfully. Safe to assert in automated tests
// without a database running.
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => !check.Tags.Contains("ready"),
});

// Readiness: includes the database connectivity check. Used for manual
// verification during local setup (see README) once Postgres is running
// and migrated — not asserted in the automated smoke test suite, since
// that would make tests depend on external environment state.
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.Run();

// Exposes the generated Program class to Dashboard.Api.Tests via
// WebApplicationFactory<Program>, which needs a public type to bootstrap
// the test host.
public partial class Program;
