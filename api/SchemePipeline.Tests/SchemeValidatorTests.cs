using SchemePipeline.Api.Domain;
using SchemePipeline.Api.Http;

namespace SchemePipeline.Tests;

public class SchemeValidatorTests
{
    private static SchemeRequest Valid() => new(
        "  Riverside Phase 1 ", "12 Mill Lane, Leeds", "Planning",
        [new("MarketSale", 55, 320_000m), new("SocialRent", 20, 120_000m)]);

    private static SchemeRequest WithLine(TenureLineRequest line) => Valid() with { Tenures = [line] };

    private static Dictionary<string, string[]> Errors(SchemeRequest request) =>
        SchemeValidator.Validate(request).Errors;

    [Fact]
    public void Valid_request_is_trimmed_and_normalised_to_all_four_tenures()
    {
        var (scheme, errors) = SchemeValidator.Validate(Valid());

        Assert.Empty(errors);
        Assert.NotNull(scheme);
        Assert.Equal("Riverside Phase 1", scheme.Name);
        Assert.Equal(SchemeStatus.Planning, scheme.Status);
        TenureLine[] expected =
        [
            new(Tenure.SocialRent, 20, 120_000m),
            new(Tenure.AffordableRent, 0, null),
            new(Tenure.SharedOwnership, 0, null),
            new(Tenure.MarketSale, 55, 320_000m),
        ];
        Assert.Equal(expected, scheme.Tenures);
    }

    [Fact]
    public void Missing_tenure_list_means_zero_homes()
    {
        var (scheme, errors) = SchemeValidator.Validate(Valid() with { Tenures = null });

        Assert.Empty(errors);
        Assert.Equal(4, scheme!.Tenures.Count);
        Assert.All(scheme.Tenures, l => Assert.Equal(0, l.Homes));
    }

    [Fact]
    public void Reports_every_error_at_once()
    {
        var (scheme, errors) = SchemeValidator.Validate(new SchemeRequest(null, "   ", null, null));

        Assert.Null(scheme);
        Assert.Equal(new[] { "name", "siteAddress", "status" }, errors.Keys.Order());
    }

    [Fact]
    public void Status_error_lists_the_valid_options()
    {
        var errors = Errors(Valid() with { Status = "Unknown" });

        Assert.Equal("Status must be one of: Appraisal, Planning, OnSite, Complete.", errors["status"][0]);
    }

    [Theory]
    [InlineData("onsite")]
    [InlineData("2")]
    [InlineData("")]
    public void Status_must_be_a_declared_name(string status)
    {
        Assert.Contains("status", Errors(Valid() with { Status = status }).Keys);
    }

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void Name_length_limit(int length, bool valid)
    {
        var errors = Errors(Valid() with { Name = new string('a', length) });

        Assert.Equal(valid, !errors.ContainsKey("name"));
    }

    [Fact]
    public void Null_tenure_line_is_an_error_not_a_crash()
    {
        var (scheme, errors) = SchemeValidator.Validate(Valid() with { Tenures = [new("SocialRent", 1, 1m), null] });

        Assert.Null(scheme);
        Assert.Equal("tenures[1]", Assert.Single(errors.Keys));
    }

    [Fact]
    public void Unknown_tenure_is_rejected()
    {
        Assert.Contains("tenures[0].tenure", Errors(WithLine(new("FirstHomes", 1, 1m))).Keys);
    }

    [Fact]
    public void Duplicate_tenure_is_flagged_on_the_repeat()
    {
        var errors = Errors(Valid() with { Tenures = [new("SocialRent", 1, 1m), new("SocialRent", 2, 1m)] });

        Assert.Equal("tenures[1].tenure", Assert.Single(errors.Keys));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(100_000, true)]
    [InlineData(100_001, false)]
    public void Homes_range(int homes, bool valid)
    {
        var errors = Errors(WithLine(new("MarketSale", homes, 250_000m)));

        Assert.Equal(valid, !errors.ContainsKey("tenures[0].homes"));
    }

    public static TheoryData<int, decimal?, bool> ValueCases => new()
    {
        { 1, -1m, false },
        { 1, 0m, false },     // £0 is a typo now that "unknown" is null
        { 1, 0.01m, true },
        { 1, 100_000_000m, true },
        { 1, 100_000_000.01m, false },
        { 20, null, true },   // homes known, value not yet: allowed at appraisal
        { 0, null, true },
    };

    [Theory]
    [MemberData(nameof(ValueCases))]
    public void Average_value_rules(int homes, decimal? value, bool valid)
    {
        var errors = Errors(WithLine(new("MarketSale", homes, value)));

        Assert.Equal(valid, !errors.ContainsKey("tenures[0].averageValue"));
    }
}
