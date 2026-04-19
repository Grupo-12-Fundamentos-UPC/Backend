namespace HairyPaws.Contracts.Events.Requests;

public sealed record CreateEventRequest
{
    public Guid OrganizationId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public DateTimeOffset EventDate { get; init; }

    public string? Location { get; init; }

    public bool IsVolunteerEvent { get; init; }
}
