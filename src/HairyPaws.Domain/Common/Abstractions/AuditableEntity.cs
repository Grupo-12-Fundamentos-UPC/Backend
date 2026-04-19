namespace HairyPaws.Domain.Common.Abstractions;

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
