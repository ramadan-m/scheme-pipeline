using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace SchemePipeline.Tests;

// One throwaway Mongo container per test run; each test gets its own empty database.
public class MongoFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:8.0").Build();

    public MongoClient Client { get; private set; } = null!;

    public string ConnectionString => _container.GetConnectionString();

    public IMongoDatabase NewDatabase() => Client.GetDatabase($"test_{Guid.NewGuid():N}");

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Client = new MongoClient(_container.GetConnectionString());
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public class MongoCollection : ICollectionFixture<MongoFixture>
{
    public const string Name = "mongo";
}
