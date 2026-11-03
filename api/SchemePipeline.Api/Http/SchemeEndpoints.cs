using SchemePipeline.Api.Data;
using SchemePipeline.Api.Domain;

namespace SchemePipeline.Api.Http;

public static class SchemeEndpoints
{
    public static void MapSchemeEndpoints(this IEndpointRouteBuilder app)
    {
        var schemes = app.MapGroup("/api/schemes");
        schemes.MapGet("/", List);
        schemes.MapGet("/{id:guid}", Get);
        schemes.MapPost("/", Create);
        schemes.MapPut("/{id:guid}", Update);
        app.MapGet("/api/summary", Summary);
    }

    private static async Task<IResult> List(
        string? status, string? sort, bool? desc, SchemeStore store, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        SchemeStatus? statusFilter = null;
        if (status is not null)
        {
            if (SchemeValidator.TryParseName(status, out SchemeStatus parsed, ignoreCase: true))
                statusFilter = parsed;
            else
                errors["status"] = [$"Status must be one of: {SchemeValidator.OneOf<SchemeStatus>()}."];
        }

        var sortBy = SchemeSort.Status;
        if (sort is not null && !SchemeValidator.TryParseName(sort, out sortBy, ignoreCase: true))
            errors["sort"] = [$"Sort must be one of: {SchemeValidator.OneOf<SchemeSort>()}."];

        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var found = await store.ListAsync(statusFilter, ct);
        return Results.Ok(SchemeSorting.Sort(found, sortBy, desc ?? false).Select(SchemeResponse.From));
    }

    private static async Task<IResult> Get(Guid id, SchemeStore store, CancellationToken ct) =>
        await store.GetAsync(id, ct) is { } scheme ? Results.Ok(SchemeResponse.From(scheme)) : NotFound(id);

    private static async Task<IResult> Create(SchemeRequest request, SchemeStore store, CancellationToken ct)
    {
        var (scheme, errors) = SchemeValidator.Validate(request);
        if (scheme is null)
            return Results.ValidationProblem(errors);

        var added = await store.AddAsync(scheme, ct);
        return Results.Created($"/api/schemes/{added.Id}", SchemeResponse.From(added));
    }

    // Body is validated before the database is touched, so an invalid body wins over an unknown id (400, not 404).
    private static async Task<IResult> Update(Guid id, SchemeRequest request, SchemeStore store, CancellationToken ct)
    {
        var (scheme, errors) = SchemeValidator.Validate(request);
        if (scheme is null)
            return Results.ValidationProblem(errors);

        return await store.UpdateAsync(id, scheme, ct) is { } updated
            ? Results.Ok(SchemeResponse.From(updated))
            : NotFound(id);
    }

    private static async Task<IResult> Summary(SchemeStore store, CancellationToken ct) =>
        Results.Ok(SchemeMaths.ForPortfolio(await store.ListAsync(null, ct)));

    private static IResult NotFound(Guid id) =>
        Results.Problem(statusCode: 404, title: "Scheme not found", detail: $"No scheme has id {id}.");
}
