using SchemePipeline.Api.Domain;
using static SchemePipeline.Api.Domain.Tenure;

namespace SchemePipeline.Tests;

public class SchemeMathsTests
{
    private static TenureLine Line(Tenure tenure, int homes, decimal averageValue = 100_000m) =>
        new(tenure, homes, averageValue);

    private static Scheme SchemeWith(params TenureLine[] lines) => new() { Tenures = [.. lines] };

    private static TenureLine[] WorkedExample =>
    [
        Line(SocialRent, 20, 120_000m),
        Line(AffordableRent, 10, 150_000m),
        Line(SharedOwnership, 15, 260_000m),
        Line(MarketSale, 55, 320_000m),
    ];

    [Fact]
    public void Worked_example_matches_hand_calculation()
    {
        var totals = SchemeMaths.For(WorkedExample);

        Assert.Equal(new Totals(100, 45, 45.0m, 25_400_000m), totals);
    }

    [Fact]
    public void Zero_homes_gives_null_percent()
    {
        var totals = SchemeMaths.For(
            [Line(SocialRent, 0), Line(AffordableRent, 0), Line(SharedOwnership, 0), Line(MarketSale, 0)]);

        Assert.Equal(new Totals(0, 0, null, 0m), totals);
    }

    [Fact]
    public void No_lines_gives_null_percent()
    {
        Assert.Equal(new Totals(0, 0, null, 0m), SchemeMaths.For([]));
    }

    [Fact]
    public void Market_sale_only_is_zero_percent_not_null()
    {
        var totals = SchemeMaths.For([Line(MarketSale, 30, 300_000m)]);

        Assert.Equal(new Totals(30, 0, 0m, 9_000_000m), totals);
    }

    [Fact]
    public void All_affordable_is_one_hundred_percent()
    {
        var totals = SchemeMaths.For([Line(SocialRent, 10, 100_000m)]);

        Assert.Equal(new Totals(10, 10, 100m, 1_000_000m), totals);
    }

    [Theory]
    [InlineData(SocialRent)]
    [InlineData(AffordableRent)]
    [InlineData(SharedOwnership)]
    public void Each_affordable_tenure_counts(Tenure tenure)
    {
        var totals = SchemeMaths.For([Line(tenure, 1), Line(MarketSale, 1)]);

        Assert.Equal(1, totals.AffordableHomes);
        Assert.Equal(50.0m, totals.AffordablePercent);
    }

    // decimal can't be an attribute argument, so these cases can't use InlineData.
    public static TheoryData<int, int, decimal> RoundingCases => new()
    {
        { 1, 2, 33.3m },  // 33.33... rounds down
        { 2, 1, 66.7m },  // 66.66... rounds up
        { 1, 15, 6.3m },  // exactly 6.25: away from zero; .NET's default ToEven would give 6.2
    };

    [Theory]
    [MemberData(nameof(RoundingCases))]
    public void Percent_rounds_to_one_decimal_place(int affordable, int market, decimal expected)
    {
        var totals = SchemeMaths.For([Line(SocialRent, affordable), Line(MarketSale, market)]);

        Assert.Equal(expected, totals.AffordablePercent);
    }

    [Fact]
    public void Value_keeps_pennies()
    {
        var totals = SchemeMaths.For([Line(SharedOwnership, 3, 100_000.10m)]);

        Assert.Equal(300_000.30m, totals.Value);
    }

    [Fact]
    public void Portfolio_percent_is_weighted_by_homes_not_averaged()
    {
        var big = SchemeWith(Line(SocialRent, 10), Line(MarketSale, 90));
        var small = SchemeWith(Line(SocialRent, 10));

        var totals = SchemeMaths.ForPortfolio([big, small]);

        Assert.Equal(110, totals.Homes);
        Assert.Equal(20, totals.AffordableHomes);
        Assert.Equal(18.2m, totals.AffordablePercent);  // averaging 10% and 100% would give 55.0
    }

    [Fact]
    public void Portfolio_sums_across_schemes()
    {
        var totals = SchemeMaths.ForPortfolio(
            [SchemeWith(WorkedExample), SchemeWith(Line(MarketSale, 30, 300_000m))]);

        Assert.Equal(new Totals(130, 45, 34.6m, 34_400_000m), totals);
    }

    [Fact]
    public void Empty_portfolio_gives_null_percent()
    {
        Assert.Equal(new Totals(0, 0, null, 0m), SchemeMaths.ForPortfolio([]));
    }
}
