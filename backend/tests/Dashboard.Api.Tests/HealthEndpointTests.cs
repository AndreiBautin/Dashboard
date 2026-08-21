using System.Net;

namespace Dashboard.Api.Tests;

/// <summary>
/// Proves the whole DI graph (Api → Infrastructure → Application → Domain)
/// wires up and the host starts successfully — the actual point of Phase 1.
/// Deliberately does not assert on /health/ready, which requires a real
/// PostgreSQL connection; that's verified manually per the README once
/// Postgres is installed and migrated locally.
/// </summary>
public class HealthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HealthEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
