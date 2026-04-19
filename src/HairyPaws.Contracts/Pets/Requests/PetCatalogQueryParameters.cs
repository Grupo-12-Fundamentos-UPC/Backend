namespace HairyPaws.Contracts.Pets.Requests;

public sealed record PetCatalogQueryParameters
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Species { get; init; }

    public string? Sex { get; init; }

    public string? Size { get; init; }

    public string? LocationDistrict { get; init; }

    public string? Search { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }
}
