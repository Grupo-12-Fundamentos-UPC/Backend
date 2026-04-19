namespace HairyPaws.Contracts.Adoption.Requests;

public sealed record AdoptionRequestsQueryParameters
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Status { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }
}
