namespace HairyPaws.Contracts.Notifications.Requests;

public sealed record NotificationsQueryParameters
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public bool? IsRead { get; init; }

    public string? Type { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }
}
