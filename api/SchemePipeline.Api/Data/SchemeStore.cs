using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using SchemePipeline.Api.Domain;

namespace SchemePipeline.Api.Data;

public class SchemeStore
{
    public const string CollectionName = "schemes";

    private readonly IMongoCollection<Scheme> _schemes;

    // Runs once per process; registering a serializer twice throws.
    // Decimal needs no registration: driver 3.x already stores it as Decimal128 (a test guards this).
    static SchemeStore()
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        ConventionRegistry.Register(
            "scheme-pipeline",
            new ConventionPack { new CamelCaseElementNameConvention(), new EnumRepresentationConvention(BsonType.String) },
            type => type.Namespace == typeof(Scheme).Namespace);
    }

    public SchemeStore(IMongoDatabase database) =>
        _schemes = database.GetCollection<Scheme>(CollectionName);

    public Task EnsureIndexesAsync(CancellationToken ct = default) =>
        _schemes.Indexes.CreateOneAsync(
            new CreateIndexModel<Scheme>(Builders<Scheme>.IndexKeys.Ascending(s => s.Status)), cancellationToken: ct);

    public Task<List<Scheme>> ListAsync(SchemeStatus? status, CancellationToken ct = default)
    {
        var filter = status is null
            ? Builders<Scheme>.Filter.Empty
            : Builders<Scheme>.Filter.Eq(s => s.Status, status.Value);
        return _schemes.Find(filter).ToListAsync(ct);
    }

    public async Task<Scheme?> GetAsync(Guid id, CancellationToken ct = default) =>
        await _schemes.Find(s => s.Id == id).FirstOrDefaultAsync(ct);

    public async Task<Scheme> AddAsync(Scheme scheme, CancellationToken ct = default)
    {
        var now = Now();
        var stored = scheme with { Id = Guid.CreateVersion7(), CreatedAt = now, UpdatedAt = now };
        await _schemes.InsertOneAsync(stored, cancellationToken: ct);
        return stored;
    }

    // Sets only the editable fields, so CreatedAt survives. Returns null when the id doesn't exist.
    public async Task<Scheme?> UpdateAsync(Guid id, Scheme changes, CancellationToken ct = default)
    {
        var update = Builders<Scheme>.Update
            .Set(s => s.Name, changes.Name)
            .Set(s => s.SiteAddress, changes.SiteAddress)
            .Set(s => s.Status, changes.Status)
            .Set(s => s.Tenures, changes.Tenures)
            .Set(s => s.UpdatedAt, Now());
        var options = new FindOneAndUpdateOptions<Scheme> { ReturnDocument = ReturnDocument.After };
        return await _schemes.FindOneAndUpdateAsync<Scheme>(s => s.Id == id, update, options, ct);
    }

    // Mongo stores milliseconds; trimming here keeps what we return identical to what we stored.
    private static DateTime Now()
    {
        var now = DateTime.UtcNow;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMillisecond));
    }
}
