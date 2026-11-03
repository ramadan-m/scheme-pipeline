namespace SchemePipeline.Api.Domain;

public enum SchemeSort { Status, Name, Homes, Affordable, Value }

public static class SchemeSorting
{
    public static List<Scheme> Sort(IEnumerable<Scheme> schemes, SchemeSort by, bool descending) =>
        (by switch
        {
            SchemeSort.Name => Order(schemes, s => s.Name, descending, StringComparer.OrdinalIgnoreCase),
            SchemeSort.Homes => Order(schemes, s => SchemeMaths.For(s.Tenures).Homes, descending),
            SchemeSort.Affordable => Order(schemes, s => SchemeMaths.For(s.Tenures).AffordablePercent, descending),
            SchemeSort.Value => Order(schemes, s => SchemeMaths.For(s.Tenures).Value, descending),
            _ => Order(schemes, s => s.Status, descending),
        })
        .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    private static IOrderedEnumerable<Scheme> Order<TKey>(
        IEnumerable<Scheme> schemes, Func<Scheme, TKey> key, bool descending, IComparer<TKey>? comparer = null) =>
        descending ? schemes.OrderByDescending(key, comparer) : schemes.OrderBy(key, comparer);
}
