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
    public void Name_sort_ignores_case_both_ways()
    {
        Scheme[] schemes = [S("cedar"), S("Beech"), S("ash")];

        // Case-sensitive (ordinal) order would put "Beech" before "ash".
        Assert.Equal(["ash", "Beech", "cedar"], Names(SchemeSorting.Sort(schemes, SchemeSort.Name, descending: false)));
        Assert.Equal(["cedar", "Beech", "ash"], Names(SchemeSorting.Sort(schemes, SchemeSort.Name, descending: true)));
    }

    [Fact]
    public void Homes_sort_uses_total_homes_across_tenures()
    {
        // Names are in a different order to homes, so a name tie-break can't pass this by accident.
        var many = S("a", Planning, new TenureLine(Tenure.SocialRent, 4, null), new TenureLine(Tenure.MarketSale, 5, null));
        var few = S("b", Planning, new TenureLine(Tenure.MarketSale, 2, null));
        var some = S("c", Planning, new TenureLine(Tenure.MarketSale, 5, null));

        var sorted = SchemeSorting.Sort([many, few, some], SchemeSort.Homes, descending: false);

        Assert.Equal(["b", "c", "a"], Names(sorted));  // 2, 5, 9
    }

    [Fact]
    public void Affordable_sort_uses_the_percentage_not_the_home_count()
    {
        // By % the order is b (25%) then a (50%); by home count or by name it would be a then b.
        var half = S("a", Planning, new TenureLine(Tenure.SocialRent, 1, null), new TenureLine(Tenure.MarketSale, 1, null));
        var quarter = S("b", Planning, new TenureLine(Tenure.SocialRent, 1, null), new TenureLine(Tenure.MarketSale, 3, null));

        Assert.Equal(["b", "a"], Names(SchemeSorting.Sort([half, quarter], SchemeSort.Affordable, descending: false)));
        Assert.Equal(["a", "b"], Names(SchemeSorting.Sort([half, quarter], SchemeSort.Affordable, descending: true)));
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
