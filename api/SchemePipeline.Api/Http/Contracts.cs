namespace SchemePipeline.Api.Http;

public record TenureLineRequest(string? Tenure, int Homes, decimal? AverageValue);

public record SchemeRequest(string? Name, string? SiteAddress, string? Status, List<TenureLineRequest>? Tenures);
