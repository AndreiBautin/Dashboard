using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Dashboard.Api.Tests;

/// <summary>
/// Boots the real API host for integration tests, but supplies a
/// syntactically valid (never-connected-to) connection string so that
/// AddInfrastructure's fail-fast check passes during host startup. Tests
/// that exercise the liveness endpoint never touch the database, so no
/// real PostgreSQL instance needs to be running for this test suite to
/// pass — that keeps `dotnet test` hermetic and CI-friendly.
///
/// Explicitly forces a non-Development environment so Program.cs's
/// Development-only migrate-and-seed block never runs here — that block
/// would try to reach the placeholder connection string above and fail.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Dashboard"] =
                    "Host=localhost;Database=vantage_test_placeholder;Username=placeholder;Password=placeholder",
            });
        });
    }
}
