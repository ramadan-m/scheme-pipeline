using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SchemePipeline.Tests;

public class StartupTests
{
    [Fact]
    public async Task Api_starts_and_health_reports_503_when_the_database_is_unreachable()
    {
        // Nothing listens on port 1, so every database call fails.
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:Mongo", "mongodb://127.0.0.1:1/down"));
        var clock = Stopwatch.StartNew();

        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        // Startup must not wait on the database; /health caps its own ping at 2s.
        Assert.InRange(clock.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }
}
