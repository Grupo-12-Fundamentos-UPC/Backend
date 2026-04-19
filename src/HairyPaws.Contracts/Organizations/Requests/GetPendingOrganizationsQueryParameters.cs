namespace HairyPaws.Contracts.Organizations.Requests;

public sealed record GetPendingOrganizationsQueryParameters
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }
}
