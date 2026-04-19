namespace HairyPaws.Contracts.Common.Responses;

public sealed record ErrorResponse(
    string Code,
    string Message,
    object? Details,
    string TraceId);
