using HairyPaws.Domain.Common.Abstractions;

namespace HairyPaws.Domain.Pets.Entities;

public sealed class PetPhoto : Entity
{
    private PetPhoto()
    {
    }

    private PetPhoto(Guid petId, string filePath, int sortOrder, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        PetId = petId;
        FilePath = NormalizeRequired(filePath);
        SortOrder = sortOrder;
        CreatedAt = createdAt;
    }

    public Guid PetId { get; private set; }

    public Pet Pet { get; private set; } = null!;

    public string FilePath { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static PetPhoto Create(Guid petId, string filePath, int sortOrder, DateTimeOffset createdAt)
    {
        return new PetPhoto(petId, filePath, sortOrder, createdAt);
    }

    private static string NormalizeRequired(string value) => value.Trim();
}
