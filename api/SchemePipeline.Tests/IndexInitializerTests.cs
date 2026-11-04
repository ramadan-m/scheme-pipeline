using Microsoft.Extensions.Logging.Abstractions;
using SchemePipeline.Api.Data;

namespace SchemePipeline.Tests;

public class IndexInitializerTests
{
    [Fact]
    public async Task Retries_until_the_database_is_reachable_then_stops()
    {
        var attempts = 0;
        Task Ensure(CancellationToken _) =>
            ++attempts < 3 ? throw new TimeoutException("database down") : Task.CompletedTask;
        var initializer = new IndexInitializer(Ensure, NullLogger<IndexInitializer>.Instance, TimeSpan.FromMilliseconds(10));

        await initializer.StartAsync(CancellationToken.None);
        await initializer.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(3, attempts);  // two failures, one success, then no more calls
    }
}
