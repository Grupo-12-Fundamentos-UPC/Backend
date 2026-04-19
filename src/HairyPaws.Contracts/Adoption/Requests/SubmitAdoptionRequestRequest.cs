namespace HairyPaws.Contracts.Adoption.Requests;

public sealed record SubmitAdoptionRequestRequest
{
    public Guid PetId { get; init; }

    public string ContactPhone { get; init; } = string.Empty;

    public string? LivingConditions { get; init; }

    public bool HasPreviousPets { get; init; }

    public string WhyAdopt { get; init; } = string.Empty;
}
