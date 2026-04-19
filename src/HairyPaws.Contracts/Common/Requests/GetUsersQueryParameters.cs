namespace HairyPaws.Contracts.Common.Requests;

public sealed record GetUsersQueryParameters
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Role { get; init; }

    public string? Status { get; init; }

    public string? VerificationStatus { get; init; }

    public string? Search { get; init; }
}
