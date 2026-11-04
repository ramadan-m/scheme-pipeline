using MongoDB.Bson;
using MongoDB.Driver;
using SchemePipeline.Api.Data;
using SchemePipeline.Api.Domain;

namespace SchemePipeline.Tests;

[Collection(MongoCollection.Name)]
public class SchemeStoreTests(MongoFixture mongo)
{
    private readonly IMongoDatabase _db = mongo.NewDatabase();

    private SchemeStore Store() => new(_db);

    private static Scheme Riverside(SchemeStatus status = SchemeStatus.OnSite) => new()
    {
        Name = "Riverside",
        SiteAddress = "12 Mill Lane",
        Status = status,
        Tenures =
        [
            new(Tenure.SocialRent, 20, null),
            new(Tenure.AffordableRent, 0, null),
            new(Tenure.SharedOwnership, 3, 100_000.10m),
            new(Tenure.MarketSale, 55, 320_000m),
        ],
    };

    [Fact]
    public async Task Add_then_get_round_trips_every_field()
    {
        var added = await Store().AddAsync(Riverside());

        var loaded = await Store().GetAsync(added.Id);

        Assert.NotNull(loaded);
        Assert.Equal(added.Id, loaded.Id);
        Assert.Equal("Riverside", loaded.Name);
        Assert.Equal("12 Mill Lane", loaded.SiteAddress);
        Assert.Equal(SchemeStatus.OnSite, loaded.Status);
        Assert.Equal(Riverside().Tenures, loaded.Tenures);
        Assert.Equal(added.CreatedAt, loaded.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, loaded.CreatedAt.Kind);
    }

    [Fact]
    public async Task Stored_document_uses_native_types_and_readable_names()
    {
        var added = await Store().AddAsync(Riverside());

        var raw = await _db.GetCollection<BsonDocument>(SchemeStore.CollectionName)
            .Find(Builders<BsonDocument>.Filter.Empty).SingleAsync();

        Assert.Equal(BsonBinarySubType.UuidStandard, raw["_id"].AsBsonBinaryData.SubType);
        Assert.Equal(added.Id, raw["_id"].AsGuid);
        Assert.Equal("OnSite", raw["status"].AsString);
        var lines = raw["tenures"].AsBsonArray.Select(l => l.AsBsonDocument).ToList();
        Assert.Equal("SocialRent", lines[0]["tenure"].AsString);
        Assert.Equal(BsonNull.Value, lines[0]["averageValue"]);
        Assert.Equal(BsonType.Decimal128, lines[2]["averageValue"].BsonType);
        Assert.Equal(100_000.10m, lines[2]["averageValue"].AsDecimal);
    }

    [Fact]
    public async Task Update_changes_editable_fields_and_keeps_created_at()
    {
        var added = await Store().AddAsync(Riverside());
        await Task.Delay(5);

        var updated = await Store().UpdateAsync(added.Id, Riverside(SchemeStatus.Complete) with
        {
            Name = "Riverside 2",
            SiteAddress = "99 New Road",
            Tenures = [new(Tenure.MarketSale, 7, 250_000m)],
        });
        var reloaded = await Store().GetAsync(added.Id);

        Assert.NotNull(updated);
        Assert.Equal("Riverside 2", updated.Name);
        Assert.Equal(SchemeStatus.Complete, updated.Status);
        Assert.Equal(added.CreatedAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt > added.UpdatedAt);
        // Every editable field must be persisted, not just echoed back.
        Assert.Equal("99 New Road", reloaded!.SiteAddress);
        Assert.Equal([new TenureLine(Tenure.MarketSale, 7, 250_000m)], reloaded.Tenures);
    }

    [Fact]
    public async Task Ensure_indexes_creates_the_status_index()
    {
        await Store().EnsureIndexesAsync();

        var indexes = await (await _db.GetCollection<BsonDocument>(SchemeStore.CollectionName).Indexes.ListAsync()).ToListAsync();

        Assert.Contains(new BsonDocument("status", 1), indexes.Select(index => index["key"].AsBsonDocument));
    }

    [Fact]
    public async Task Unknown_id_gives_null_for_get_and_update()
    {
        var id = Guid.CreateVersion7();

        Assert.Null(await Store().GetAsync(id));
        Assert.Null(await Store().UpdateAsync(id, Riverside()));
    }

    [Fact]
    public async Task List_filters_by_status()
    {
        await Store().AddAsync(Riverside(SchemeStatus.Planning) with { Name = "A" });
        await Store().AddAsync(Riverside(SchemeStatus.OnSite) with { Name = "B" });

        var planning = await Store().ListAsync(SchemeStatus.Planning);
        var all = await Store().ListAsync(null);

        Assert.Equal("A", Assert.Single(planning).Name);
        Assert.Equal(2, all.Count);
    }
}
