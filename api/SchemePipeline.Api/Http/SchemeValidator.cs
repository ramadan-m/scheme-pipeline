using SchemePipeline.Api.Domain;

namespace SchemePipeline.Api.Http;

public static class SchemeValidator
{
    public const int MaxNameLength = 200;
    public const int MaxAddressLength = 500;
    public const int MaxHomes = 100_000;
    public const decimal MaxAverageValue = 100_000_000m;

    public static (Scheme? Scheme, Dictionary<string, string[]> Errors) Validate(SchemeRequest request)
    {
        var errors = new Dictionary<string, string>();

        var name = Required(request.Name, "name", "Name", MaxNameLength, errors);
        var address = Required(request.SiteAddress, "siteAddress", "Site address", MaxAddressLength, errors);

        if (!TryParseName(request.Status, out SchemeStatus status))
            errors.TryAdd("status", $"Status must be one of: {string.Join(", ", Enum.GetNames<SchemeStatus>())}.");

        var lines = new Dictionary<Tenure, TenureLine>();
        var requested = request.Tenures ?? [];
        for (var i = 0; i < requested.Count; i++)
        {
            var line = requested[i];
            var key = $"tenures[{i}]";

            if (line is null)
            {
                errors.TryAdd(key, "Tenure line must not be empty.");
                continue;
            }

            // Checked before the (int) cast below, so 2.5 or 1e20 become a field error, not a truncation or overflow.
            var homesValid = line.Homes is >= 0 and <= MaxHomes && line.Homes == decimal.Truncate(line.Homes);
            if (!homesValid)
                errors.TryAdd($"{key}.homes", "Homes must be a whole number from 0 to 100,000.");

            if (!TryParseName(line.Tenure, out Tenure tenure))
                errors.TryAdd($"{key}.tenure", $"Tenure must be one of: {string.Join(", ", Enum.GetNames<Tenure>())}.");
            else if (!lines.TryAdd(tenure, new TenureLine(tenure, homesValid ? (int)line.Homes : 0, line.AverageValue)))
                errors.TryAdd($"{key}.tenure", $"{tenure} appears more than once.");

            if (line.AverageValue is <= 0 or > MaxAverageValue)
                errors.TryAdd($"{key}.averageValue", "Average value, if given, must be more than 0 and at most 100,000,000.");
        }

        if (errors.Count > 0)
            return (null, errors.ToDictionary(e => e.Key, e => new[] { e.Value }));

        var scheme = new Scheme
        {
            Name = name,
            SiteAddress = address,
            Status = status,
            Tenures = Enum.GetValues<Tenure>()
                .Select(t => lines.TryGetValue(t, out var l) ? l : new TenureLine(t, 0, null))
                .ToList(),
        };
        return (scheme, new());
    }

    private static string Required(string? value, string key, string label, int maxLength, Dictionary<string, string> errors)
    {
        var trimmed = value?.Trim() ?? "";
        if (trimmed.Length == 0)
            errors.TryAdd(key, $"{label} is required.");
        else if (trimmed.Length > maxLength)
            errors.TryAdd(key, $"{label} must be {maxLength} characters or fewer.");
        return trimmed;
    }

    // Enum.TryParse alone would also accept "2", "99" or "Planning, OnSite", so only declared names count.
    public static bool TryParseName<T>(string? value, out T result, bool ignoreCase = false) where T : struct, Enum
    {
        result = default;
        var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        return value is not null && Enum.GetNames<T>().Contains(value, comparer) && Enum.TryParse(value, ignoreCase, out result);
    }

    public static string OneOf<T>() where T : struct, Enum => string.Join(", ", Enum.GetNames<T>());
}
