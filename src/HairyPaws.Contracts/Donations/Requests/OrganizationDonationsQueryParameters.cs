namespace HairyPaws.Contracts.Donations.Requests;

public sealed record OrganizationDonationsQueryParameters
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Status { get; init; }

    public string? DonationType { get; init; }

    public string? Search { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }
}
