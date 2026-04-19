using HairyPaws.Domain.Adoption.Entities;
using HairyPaws.Domain.Common.Abstractions;
using HairyPaws.Domain.Visits.Enums;

namespace HairyPaws.Domain.Visits.Entities;

public sealed class Visit : Entity
{
    private Visit()
    {
    }

    private Visit(
        Guid adoptionRequestId,
        DateTimeOffset scheduledAt,
        string? location,
        string? notes,
        DateTimeOffset utcNow)
    {
        Id = Guid.NewGuid();
        AdoptionRequestId = adoptionRequestId;
        ScheduledAt = scheduledAt;
        Location = NormalizeOptional(location);
        Status = VisitStatus.Pending;
        Notes = NormalizeOptional(notes);
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public Guid AdoptionRequestId { get; private set; }

    public AdoptionRequest AdoptionRequest { get; private set; } = null!;

    public DateTimeOffset ScheduledAt { get; private set; }

    public string? Location { get; private set; }

    public VisitStatus Status { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Visit Create(
        Guid adoptionRequestId,
        DateTimeOffset scheduledAt,
        string? location,
        string? notes,
        DateTimeOffset utcNow)
    {
        return new Visit(adoptionRequestId, scheduledAt, location, notes, utcNow);
    }

    public bool HasActiveStatus() => IsActiveStatus(Status);

    public bool CanBeApprovedByAdopter() => Status == VisitStatus.Pending;

    public bool CanBeRejectedByAdopter() => Status == VisitStatus.Pending;

    public bool CanBeCancelledByManager() => Status is VisitStatus.Pending or VisitStatus.Approved;

    public bool CanBeCompletedByManager() => Status == VisitStatus.Approved;

    public static bool IsActiveStatus(VisitStatus status) => status is VisitStatus.Pending or VisitStatus.Approved;

    public void Approve(string? notes, DateTimeOffset utcNow)
    {
        Status = VisitStatus.Approved;
        Notes = NormalizeOptional(notes) ?? Notes;
        UpdatedAt = utcNow;
    }

    public void Reject(string? notes, DateTimeOffset utcNow)
    {
        Status = VisitStatus.Rejected;
        Notes = NormalizeOptional(notes);
        UpdatedAt = utcNow;
    }

    public void Cancel(string? notes, DateTimeOffset utcNow)
    {
        Status = VisitStatus.Cancelled;
        Notes = NormalizeOptional(notes);
        UpdatedAt = utcNow;
    }

    public void Complete(string? notes, DateTimeOffset utcNow)
    {
        Status = VisitStatus.Completed;
        Notes = NormalizeOptional(notes) ?? Notes;
        UpdatedAt = utcNow;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
