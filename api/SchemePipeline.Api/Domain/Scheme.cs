namespace SchemePipeline.Api.Domain;

// Declaration order is pipeline order; sorting relies on it.
public enum SchemeStatus { Appraisal, Planning, OnSite, Complete }

public enum Tenure { SocialRent, AffordableRent, SharedOwnership, MarketSale }

public static class TenureRules
{
    // Allow-list: a new tenure is not affordable until someone decides it is.
    public static bool IsAffordable(this Tenure tenure) =>
        tenure is Tenure.SocialRent or Tenure.AffordableRent or Tenure.SharedOwnership;
}

// AverageValue is null when the homes are not valued yet (common at appraisal).
public record TenureLine(Tenure Tenure, int Homes, decimal? AverageValue);

public record Scheme
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string SiteAddress { get; init; } = "";
    public SchemeStatus Status { get; init; }
    public List<TenureLine> Tenures { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
