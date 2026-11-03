using SchemePipeline.Api.Domain;
using static SchemePipeline.Api.Domain.SchemeStatus;

namespace SchemePipeline.Tests;

public class SchemeSortingTests
{
    private static Scheme S(string name, SchemeStatus status = Planning, params TenureLine[] lines) =>
        new() { Name = name, Status = status, Tenures = [.. lines] };

    private static string[] Names(IEnumerable<Scheme> schemes) => schemes.Select(s => s.Name).ToArray();

    [Fact]
    public void Status_sorts_in_pipeline_order_not_alphabetically()
    {
        Scheme[] schemes = [S("a", Complete), S("b", Appraisal), S("c", OnSite), S("d", Planning)];

        var sorted = SchemeSorting.Sort(schemes, SchemeSort.Status, descending: false);

        // Alphabetical would be Appraisal, Complete, OnSite, Planning.
        Assert.Equal([Appraisal, Planning, OnSite, Complete], sorted.Select(s => s.Status));
    }

    [Fact]
    public void Descending_status_reverses_the_pipeline()
    {
        Scheme[] schemes = [S("a", Planning), S("b", Complete), S("c", Appraisal)];

        var sorted = SchemeSorting.Sort(schemes, SchemeSort.Status, descending: true);

        Assert.Equal([Complete, Planning, Appraisal], sorted.Select(s => s.Status));
    }

    [Fact]
    public void Ties_break_by_name_ignoring_case_even_when_descending()
    {
        Scheme[] schemes = [S("beech"), S("Ash"), S("cedar")];

        var sorted = SchemeSorting.Sort(schemes, SchemeSort.Status, descending: true);

        Assert.Equal(["Ash", "beech", "cedar"], Names(sorted));
    }

    [Fact]
    public void Value_sort_uses_computed_totals()
    {
        var cheap = S("cheap", Planning, new TenureLine(Tenure.MarketSale, 10, 100_000m));
        var dear = S("dear", Planning, new TenureLine(Tenure.MarketSale, 2, 1_000_000m));

        var sorted = SchemeSorting.Sort([dear, cheap], SchemeSort.Value, descending: false);

        Assert.Equal(["cheap", "dear"], Names(sorted));  // £1.0m before £2.0m
    }

    [Fact]
    public void Schemes_with_no_homes_sort_first_by_affordable_percent()
    {
        var empty = S("empty");
        var half = S("half", Planning, new(Tenure.SocialRent, 1, 1m), new(Tenure.MarketSale, 1, 1m));

        var sorted = SchemeSorting.Sort([half, empty], SchemeSort.Affordable, descending: false);

        Assert.Equal(["empty", "half"], Names(sorted));
    }
}
