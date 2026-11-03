using SchemePipeline.Api.Domain;

namespace SchemePipeline.Api.Http;

public record SchemeResponse(
    Guid Id, string Name, string SiteAddress, SchemeStatus Status,
    List<TenureLine> Tenures, Totals Totals, DateTime CreatedAt, DateTime UpdatedAt)
{
    public static SchemeResponse From(Scheme s) =>
        new(s.Id, s.Name, s.SiteAddress, s.Status, s.Tenures, SchemeMaths.For(s.Tenures), s.CreatedAt, s.UpdatedAt);
}

// Homes is decimal so 2.5 reaches the validator (a field error) instead of failing JSON binding.
public record TenureLineRequest(string? Tenure, decimal Homes, decimal? AverageValue);

public record SchemeRequest(string? Name, string? SiteAddress, string? Status, List<TenureLineRequest?>? Tenures);
