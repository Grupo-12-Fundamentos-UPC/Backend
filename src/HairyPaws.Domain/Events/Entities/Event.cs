using HairyPaws.Domain.Common.Abstractions;
using HairyPaws.Domain.Events.Enums;
using HairyPaws.Domain.Organizations.Entities;

namespace HairyPaws.Domain.Events.Entities;

public sealed class Event : AuditableEntity
{
    private Event()
    {
    }

    private Event(
        Guid organizationId,
        string title,
        string description,
        DateTimeOffset eventDate,
        string? location,
        bool isVolunteerEvent,
        DateTimeOffset utcNow)
    {
        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Title = NormalizeRequired(title);
        Description = NormalizeRequired(description);
        EventDate = eventDate;
        Location = NormalizeOptional(location);
        IsVolunteerEvent = isVolunteerEvent;
        Status = EventStatus.Draft;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public Guid OrganizationId { get; private set; }

    public Organization Organization { get; private set; } = null!;

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset EventDate { get; private set; }

    public string? Location { get; private set; }

    public bool IsVolunteerEvent { get; private set; }

    public string? ImagePath { get; private set; }

    public EventStatus Status { get; private set; }

    public static Event Create(
        Guid organizationId,
        string title,
        string description,
        DateTimeOffset eventDate,
        string? location,
        bool isVolunteerEvent,
        DateTimeOffset utcNow)
    {
        return new Event(organizationId, title, description, eventDate, location, isVolunteerEvent, utcNow);
    }

    public bool BelongsToOrganization(Guid organizationId) => OrganizationId == organizationId;

    public bool IsPubliclyVisible() => DeletedAt is null && Status == EventStatus.Published;

    public bool CanPublish() => DeletedAt is null && Status == EventStatus.Draft;

    public bool CanCancel() => DeletedAt is null && Status is EventStatus.Draft or EventStatus.Published;

    public bool CanUpdate() => DeletedAt is null && Status != EventStatus.Cancelled;

    public IReadOnlyCollection<string> GetPublishValidationErrors(DateTimeOffset utcNow)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(Title))
        {
            errors.Add("Title is required before publishing.");
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            errors.Add("Description is required before publishing.");
        }

        if (EventDate <= utcNow)
        {
            errors.Add("Event date must be in the future before publishing.");
        }

        return errors;
    }

    public void Update(
        string title,
        string description,
        DateTimeOffset eventDate,
        string? location,
        bool isVolunteerEvent,
        DateTimeOffset utcNow)
    {
        Title = NormalizeRequired(title);
        Description = NormalizeRequired(description);
        EventDate = eventDate;
        Location = NormalizeOptional(location);
        IsVolunteerEvent = isVolunteerEvent;
        UpdatedAt = utcNow;
    }

    public void SetImage(string? imagePath, DateTimeOffset utcNow)
    {
        ImagePath = NormalizeOptional(imagePath);
        UpdatedAt = utcNow;
    }

    public void Publish(DateTimeOffset utcNow)
    {
        Status = EventStatus.Published;
        UpdatedAt = utcNow;
    }

    public void Cancel(DateTimeOffset utcNow)
    {
        Status = EventStatus.Cancelled;
        UpdatedAt = utcNow;
    }

    private static string NormalizeRequired(string value) => value.Trim();

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
