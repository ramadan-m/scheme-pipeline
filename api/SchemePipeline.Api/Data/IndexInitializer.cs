namespace SchemePipeline.Api.Data;

// Creates the indexes in the background, retrying until the database is reachable, so the API
// starts (and /health can report 503) even when Mongo is down. The index only speeds up the
// status filter; queries are correct without it.
public class IndexInitializer(
    Func<CancellationToken, Task> ensureIndexes, ILogger<IndexInitializer> logger, TimeSpan retryDelay)
    : BackgroundService
{
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            // Without a cap, one attempt waits the driver's 30s server-selection timeout.
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            attempt.CancelAfter(AttemptTimeout);
            try
            {
                await ensureIndexes(attempt.Token);
                logger.LogInformation("Database indexes are in place");
                return;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Could not create database indexes; retrying in {Delay}", retryDelay);
            }
            await Task.Delay(retryDelay, stoppingToken);
        }
    }
}
