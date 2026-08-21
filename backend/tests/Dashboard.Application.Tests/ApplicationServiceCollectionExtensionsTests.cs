using Microsoft.Extensions.DependencyInjection;

namespace Dashboard.Application.Tests;

/// <summary>
/// Proves the Application layer's DI registration entry point can be
/// called without throwing. Real use-case service registrations get their
/// own tests as they're added in Phase 2.
/// </summary>
public class ApplicationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddApplication_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        Assert.Same(services, result);
    }
}
