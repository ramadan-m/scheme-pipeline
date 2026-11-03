using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using SchemePipeline.Api.Data;
using SchemePipeline.Api.Http;

var builder = WebApplication.CreateBuilder(args);

var mongoUrl = new MongoUrl(builder.Configuration.GetConnectionString("Mongo")
    ?? throw new InvalidOperationException("ConnectionStrings:Mongo is not set."));

builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoUrl));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoUrl.DatabaseName));
builder.Services.AddSingleton<SchemeStore>();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// A body that can't be read as JSON (e.g. "homes": "abc") is the client's error: keep its 400, don't report a 500.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = ex => ex is BadHttpRequestException bad ? bad.StatusCode : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();

await app.Services.GetRequiredService<SchemeStore>().EnsureIndexesAsync();

app.MapSchemeEndpoints();
app.MapGet("/health", async (IMongoDatabase db, ILogger<Program> logger, CancellationToken ct) =>
{
    // Without a cap, a down database makes this wait the driver's 30s server-selection timeout.
    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
    timeout.CancelAfter(TimeSpan.FromSeconds(2));
    try
    {
        await db.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: timeout.Token);
        return Results.Ok(new { status = "ok" });
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Health check failed: database ping did not succeed within 2s");
        return Results.Problem(statusCode: 503, title: "Database unavailable");
    }
});

app.Run();
