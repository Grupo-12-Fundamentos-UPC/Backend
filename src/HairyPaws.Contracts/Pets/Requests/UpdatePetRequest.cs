namespace HairyPaws.Contracts.Pets.Requests;

public sealed record UpdatePetRequest
{
    public string? Name { get; init; }

    public string? Species { get; init; }

    public string? Breed { get; init; }

    public string? AgeText { get; init; }

    public string? Sex { get; init; }

    public string? Size { get; init; }

    public bool? Sterilized { get; init; }

    public bool? Vaccinated { get; init; }

    public string? Description { get; init; }

    public string? Temperament { get; init; }

    public string? MedicalHistory { get; init; }

    public string? LocationDistrict { get; init; }
}
