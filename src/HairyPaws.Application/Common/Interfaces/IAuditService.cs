namespace HairyPaws.Application.Common.Interfaces;

public interface IAuditService
{
    Task WriteAsync(string action, Guid? actorUserId, string entityName, Guid entityId, CancellationToken cancellationToken);

    Task WriteAsync(
        string action,
        Guid? actorUserId,
        string entityName,
        Guid entityId,
        object? before,
        object? after,
        object? metadata,
        CancellationToken cancellationToken);
}
