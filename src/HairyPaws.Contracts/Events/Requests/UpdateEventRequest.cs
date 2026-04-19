namespace HairyPaws.Contracts.Events.Requests;

public sealed record UpdateEventRequest
{
    public string? Title { get; init; }

    public string? Description { get; init; }

    public DateTimeOffset? EventDate { get; init; }

    public string? Location { get; init; }

    public bool? IsVolunteerEvent { get; init; }
}
