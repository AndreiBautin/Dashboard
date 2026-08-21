using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vantage.Infrastructure.Persistence;

namespace Vantage.Api.Tests;

/// <summary>
/// Boots the real API host with its real DbContext registration swapped for
/// an in-memory SQLite connection, so tests exercise the full HTTP →
/// controller → Application service → EF Core round trip without needing a
/// real PostgreSQL instance. Schema is created directly from the current
/// model via EnsureCreated — this test doesn't depend on migration files
/// existing, since those are generated locally (see README), not committed.
///
/// Explicitly forces a non-Development environment so Program.cs's
/// Development-only migrate-and-seed block (which assumes migrations exist
/// and targets whatever DbContext is registered) never runs here.
/// </summary>
public sealed class SqliteWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Never actually connected to -- AddInfrastructure only needs
                // this to be present to pass its fail-fast check. The real
                // DbContext registration is replaced below.
                ["ConnectionStrings:Vantage"] = "Host=localhost;Database=unused;Username=unused;Password=unused",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Program.cs's own AddInfrastructure() call has already registered
            // VantageDbContext with UseNpgsql by the time this runs. Removing
            // just the DbContextOptions<VantageDbContext> descriptor isn't
            // enough -- EF Core also adds its own internal per-context
            // configuration entries (e.g. IDbContextOptionsConfiguration<T>)
            // when AddDbContext runs, and those are additive: leaving them in
            // place means the Npgsql config still gets applied alongside the
            // Sqlite one added below, and EF throws "only a single database
            // provider can be registered". So purge every descriptor touching
            // VantageDbContext -- by name or as a generic argument -- before
            // re-registering it fresh.
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(VantageDbContext) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericArguments().Contains(typeof(VantageDbContext))))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Keeping the connection open for the factory's lifetime is what
            // makes SQLite's ":memory:" database persist across the multiple
            // DbContext instances a single test creates (one per request).
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<VantageDbContext>(options => options.UseSqlite(_connection));
        });
    }

    /// <summary>Creates the schema from the current model and seeds it with the given data.</summary>
    public async Task InitializeDatabaseAsync(Action<VantageDbContext> seed)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VantageDbContext>();

        await dbContext.Database.EnsureCreatedAsync();
        seed(dbContext);
        await dbContext.SaveChangesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}
