using HairyPaws.Domain.Common.Abstractions;
using HairyPaws.Domain.Identity.Entities;

namespace HairyPaws.Domain.Audit.Entities;

public sealed class AuditLog : Entity
{
    private AuditLog()
    {
    }

    private AuditLog(
        string entityName,
        Guid entityId,
        string action,
        Guid? performedByUserId,
        string? beforeJson,
        string? afterJson,
        string? metadataJson,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        EntityName = NormalizeRequired(entityName);
        EntityId = entityId;
        Action = NormalizeRequired(action);
        PerformedByUserId = performedByUserId;
        BeforeJson = NormalizeOptional(beforeJson);
        AfterJson = NormalizeOptional(afterJson);
        MetadataJson = NormalizeOptional(metadataJson);
        CreatedAt = createdAt;
    }

    public string EntityName { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public Guid? PerformedByUserId { get; private set; }

    public User? PerformedByUser { get; private set; }

    public string? BeforeJson { get; private set; }

    public string? AfterJson { get; private set; }

    public string? MetadataJson { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static AuditLog Create(
        string entityName,
        Guid entityId,
        string action,
        Guid? performedByUserId,
        string? beforeJson,
        string? afterJson,
        string? metadataJson,
        DateTimeOffset createdAt)
    {
        return new AuditLog(entityName, entityId, action, performedByUserId, beforeJson, afterJson, metadataJson, createdAt);
    }

    private static string NormalizeRequired(string value) => value.Trim();

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
