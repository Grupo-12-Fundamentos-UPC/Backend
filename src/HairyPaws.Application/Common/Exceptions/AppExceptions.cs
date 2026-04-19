namespace HairyPaws.Application.Common.Exceptions;

public abstract class AppException(string code, string message, int statusCode, object? details = null)
    : Exception(message)
{
    public string Code { get; } = code;

    public int StatusCode { get; } = statusCode;

    public object? Details { get; } = details;
}

public sealed class NotFoundException(string message)
    : AppException("NOT_FOUND", message, 404);

public sealed class UnauthorizedAppException(string message)
    : AppException("UNAUTHORIZED", message, 401);

public sealed class ForbiddenAppException(string message)
    : AppException("FORBIDDEN", message, 403);

public sealed class ConflictException(string message, object? details = null)
    : AppException("CONFLICT", message, 409, details);

public sealed class BusinessRuleViolationException(string message, object? details = null)
    : AppException("BUSINESS_RULE_VIOLATION", message, 422, details);
