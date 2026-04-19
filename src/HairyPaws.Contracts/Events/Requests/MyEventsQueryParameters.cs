namespace HairyPaws.Contracts.Events.Requests;

public sealed record MyEventsQueryParameters
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Status { get; init; }

    public string? Search { get; init; }

    public DateTimeOffset? FromDate { get; init; }

    public DateTimeOffset? ToDate { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }
}
