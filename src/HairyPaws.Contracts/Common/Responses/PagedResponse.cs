namespace HairyPaws.Contracts.Common.Responses;

public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int PageNumber,
    int PageSize,
    long TotalCount,
    int TotalPages);
