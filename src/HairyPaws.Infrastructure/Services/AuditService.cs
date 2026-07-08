using System.Text.Json;
using System.Text.Json.Serialization;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Domain.Audit.Entities;

namespace HairyPaws.Infrastructure.Services;

public sealed class AuditService(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : IAuditService
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    public Task WriteAsync(
        string action,
        Guid? actorUserId,
        string entityName,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        return WriteAsync(action, actorUserId, entityName, entityId, before: null, after: null, metadata: null, cancellationToken);
    }

    public async Task WriteAsync(
        string action,
        Guid? actorUserId,
        string entityName,
        Guid entityId,
        object? before,
        object? after,
        object? metadata,
        CancellationToken cancellationToken)
    {
        var auditLog = AuditLog.Create(
            entityName,
            entityId,
            action,
            actorUserId,
            Serialize(before),
            Serialize(after),
            Serialize(metadata),
            dateTimeProvider.UtcNow);

        await dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
    }

    private static string? Serialize(object? value)
    {
        return value is null
            ? null
            : JsonSerializer.Serialize(value, SerializerOptions);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
