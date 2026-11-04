using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Bson;
using MongoDB.Driver;
using SchemePipeline.Api.Data;

namespace SchemePipeline.Tests;

[Collection(MongoCollection.Name)]
public class ApiTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IMongoDatabase _db;

    public ApiTests(MongoFixture mongo)
    {
        // A database name in the URL also becomes the auth database, so point auth back at "admin".
        var url = new MongoUrlBuilder(mongo.ConnectionString)
        {
            DatabaseName = $"api_{Guid.NewGuid():N}",
            AuthenticationSource = "admin",
        }.ToMongoUrl();
        _db = mongo.Client.GetDatabase(url.DatabaseName);
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:Mongo", url.ToString()));
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    // 20 social rent with no value yet + 10 market sale at £300k.
    private static object Body(
        string name = "Riverside", string status = "Planning", object[]? tenures = null, string siteAddress = "12 Mill Lane") => new
    {
        name,
        siteAddress,
        status,
        tenures = tenures ??
        [
            new { tenure = "SocialRent", homes = 20, averageValue = (decimal?)null },
            new { tenure = "MarketSale", homes = 10, averageValue = (decimal?)300_000m },
        ],
    };

    private static object[] MarketSale(int homes, decimal value) =>
        [new { tenure = "MarketSale", homes, averageValue = value }];

    private async Task<JsonElement> Create(object body)
    {
        var response = await _client.PostAsJsonAsync("/api/schemes", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static string[] Names(JsonElement list) =>
        list.EnumerateArray().Select(s => s.GetProperty("name").GetString()!).ToArray();

    [Fact]
    public async Task Create_returns_201_with_location_and_computed_totals()
    {
        var response = await _client.PostAsJsonAsync("/api/schemes", Body());
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/schemes/{json.GetProperty("id").GetGuid()}", response.Headers.Location?.ToString());
        Assert.Equal("Planning", json.GetProperty("status").GetString());
        Assert.Equal(4, json.GetProperty("tenures").GetArrayLength());
        var totals = json.GetProperty("totals");
        Assert.Equal(30, totals.GetProperty("homes").GetInt32());
        Assert.Equal(20, totals.GetProperty("affordableHomes").GetInt32());
        Assert.Equal(66.7m, totals.GetProperty("affordablePercent").GetDecimal());
        Assert.Equal(3_000_000m, totals.GetProperty("value").GetDecimal());
        Assert.Equal(20, totals.GetProperty("unvaluedHomes").GetInt32());
    }

    [Fact]
    public async Task Created_scheme_can_be_fetched_by_its_location()
    {
        var response = await _client.PostAsJsonAsync("/api/schemes", Body());

        var fetched = await _client.GetFromJsonAsync<JsonElement>(response.Headers.Location);

        Assert.Equal("Riverside", fetched.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Invalid_body_returns_validation_problem_with_field_errors()
    {
        var response = await _client.PostAsJsonAsync("/api/schemes", Body(name: " ", status: "Plannng"));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = json.GetProperty("errors");
        Assert.Equal("Name is required.", errors.GetProperty("name")[0].GetString());
        Assert.True(errors.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task Fractional_homes_is_a_field_error()
    {
        var response = await _client.PostAsJsonAsync("/api/schemes",
            Body(tenures: [new { tenure = "SocialRent", homes = 2.5m, averageValue = (decimal?)null }]));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(json.GetProperty("errors").TryGetProperty("tenures[0].homes", out _));
    }

    [Fact]
    public async Task Unreadable_body_returns_400_not_500()
    {
        var response = await _client.PostAsJsonAsync("/api/schemes",
            Body(tenures: [new { tenure = "SocialRent", homes = "abc", averageValue = (decimal?)null }]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Null_tenure_line_returns_400_not_500()
    {
        var response = await _client.PostAsJsonAsync("/api/schemes", Body(tenures: [null!]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("0199e5a2-0000-7000-8000-000000000000")]
    [InlineData("not-a-guid")]
    public async Task Get_unknown_or_malformed_id_returns_404(string id)
    {
        var response = await _client.GetAsync($"/api/schemes/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(false, HttpStatusCode.BadRequest)]  // invalid body: 400 wins over the unknown id
    [InlineData(true, HttpStatusCode.NotFound)]
    public async Task Put_to_unknown_id(bool validBody, HttpStatusCode expected)
    {
        var body = validBody ? Body() : Body(name: "");

        var response = await _client.PutAsJsonAsync($"/api/schemes/{Guid.CreateVersion7()}", body);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Put_to_malformed_id_returns_404_even_with_invalid_body()
    {
        var response = await _client.PutAsJsonAsync("/api/schemes/not-a-guid", Body(name: ""));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_put_leaves_the_stored_scheme_unchanged()
    {
        var id = (await Create(Body())).GetProperty("id").GetGuid();

        var response = await _client.PutAsJsonAsync($"/api/schemes/{id}", Body(name: ""));
        var stored = await _client.GetFromJsonAsync<JsonElement>($"/api/schemes/{id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Riverside", stored.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Valid_put_updates_and_keeps_created_at()
    {
        var created = await Create(Body());
        var id = created.GetProperty("id").GetGuid();

        var response = await _client.PutAsJsonAsync(
            $"/api/schemes/{id}", Body(name: "Riverside 2", status: "OnSite", siteAddress: "99 New Road"));
        var updated = await response.Content.ReadFromJsonAsync<JsonElement>();
        var stored = await _client.GetFromJsonAsync<JsonElement>($"/api/schemes/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Riverside 2", updated.GetProperty("name").GetString());
        Assert.Equal("OnSite", updated.GetProperty("status").GetString());
        Assert.Equal("99 New Road", stored.GetProperty("siteAddress").GetString());
        Assert.Equal(created.GetProperty("createdAt").GetDateTime(), updated.GetProperty("createdAt").GetDateTime());
    }

    [Fact]
    public async Task List_filters_by_status_and_sorts_by_value()
    {
        await Create(Body(name: "mid"));                                        // £3m
        await Create(Body(name: "cheap", tenures: MarketSale(1, 100_000m)));    // £0.1m
        await Create(Body(name: "dear", tenures: MarketSale(100, 500_000m)));   // £50m
        await Create(Body(name: "building", status: "OnSite"));

        var onSite = await _client.GetFromJsonAsync<JsonElement>("/api/schemes?status=onsite");
        var byValue = await _client.GetFromJsonAsync<JsonElement>("/api/schemes?status=Planning&sort=value&desc=true");

        Assert.Equal(["building"], Names(onSite));
        Assert.Equal(["dear", "mid", "cheap"], Names(byValue));
    }

    [Theory]
    [InlineData("status=Plannng", "status")]
    [InlineData("sort=price", "sort")]
    [InlineData("status=2", "status")]
    public async Task Unknown_query_values_return_400_not_everything(string query, string field)
    {
        var response = await _client.GetAsync($"/api/schemes?{query}");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(json.GetProperty("errors").TryGetProperty(field, out _));
    }

    [Fact]
    public async Task Summary_totals_every_scheme()
    {
        await Create(Body());
        await Create(Body(name: "B", status: "OnSite", tenures: MarketSale(10, 300_000m)));

        var totals = await _client.GetFromJsonAsync<JsonElement>("/api/summary");

        Assert.Equal(40, totals.GetProperty("homes").GetInt32());
        Assert.Equal(20, totals.GetProperty("affordableHomes").GetInt32());
        Assert.Equal(50.0m, totals.GetProperty("affordablePercent").GetDecimal());
        Assert.Equal(6_000_000m, totals.GetProperty("value").GetDecimal());
        Assert.Equal(20, totals.GetProperty("unvaluedHomes").GetInt32());
    }

    [Fact]
    public async Task Unexpected_error_returns_500_problem_details_without_internals()
    {
        // A stored document the code can't read (unknown status) is a realistic unexpected failure.
        await _db.GetCollection<BsonDocument>(SchemeStore.CollectionName).InsertOneAsync(new BsonDocument
        {
            { "_id", new BsonBinaryData(Guid.CreateVersion7(), GuidRepresentation.Standard) },
            { "name", "Corrupt" },
            { "status", "Bogus" },
        });

        var response = await _client.GetAsync("/api/schemes");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("Bogus", body);
        Assert.DoesNotContain("Exception", body);
        Assert.DoesNotContain("SchemePipeline", body);
    }

    [Fact]
    public async Task Health_reports_ok_when_mongo_is_up()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Startup_creates_the_status_index_in_the_background()
    {
        var indexes = _db.GetCollection<BsonDocument>(SchemeStore.CollectionName).Indexes;
        var deadline = DateTime.UtcNow.AddSeconds(10);

        // The client is created in the constructor, so the host has started; the index follows shortly.
        while (true)
        {
            var keys = (await (await indexes.ListAsync()).ToListAsync()).Select(i => i["key"].AsBsonDocument);
            if (keys.Contains(new BsonDocument("status", 1))) return;
            Assert.True(DateTime.UtcNow < deadline, "The status index was not created within 10s of startup.");
            await Task.Delay(100);
        }
    }
}
