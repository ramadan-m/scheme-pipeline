namespace SchemePipeline.Api.Domain;

public record Totals(int Homes, int AffordableHomes, decimal? AffordablePercent, decimal Value);

public static class SchemeMaths
{
    public static Totals For(IEnumerable<TenureLine> lines)
    {
        var list = lines.ToList();
        var homes = list.Sum(l => l.Homes);
        var affordable = list.Where(l => l.Tenure.IsAffordable()).Sum(l => l.Homes);
        var value = list.Sum(l => l.Homes * l.AverageValue);

        decimal? percent = homes == 0
            ? null
            : Math.Round(affordable * 100m / homes, 1, MidpointRounding.AwayFromZero);

        return new Totals(homes, affordable, percent, value);
    }

    public static Totals ForPortfolio(IEnumerable<Scheme> schemes) =>
        For(schemes.SelectMany(s => s.Tenures));
}
