using FluentAssertions;
using HairyPaws.Domain.Pets.Entities;
using HairyPaws.Domain.Pets.Enums;

namespace HairyPaws.Tests.Unit.Domain;

public sealed class PetTests
{
    [Fact]
    public void GetPublishValidationErrors_ShouldRequirePhotoDescriptionLocationAndKnownSizeSex()
    {
        var pet = Pet.CreateForOwner(
            Guid.NewGuid(),
            "Milo",
            PetSpecies.Dog,
            "Mixed",
            "2 years",
            PetSex.Unknown,
            PetSize.Unknown,
            sterilized: true,
            vaccinated: true,
            description: null,
            temperament: "Calm",
            medicalHistory: null,
            locationDistrict: null,
            DateTimeOffset.UtcNow);

        var errors = pet.GetPublishValidationErrors(photoCount: 0);

        errors.Should().Contain(error => error.Contains("Sex"));
        errors.Should().Contain(error => error.Contains("Size"));
        errors.Should().Contain(error => error.Contains("Description"));
        errors.Should().Contain(error => error.Contains("Location district"));
        errors.Should().Contain(error => error.Contains("At least one photo"));
    }

    [Fact]
    public void Publish_ShouldSetStatusAndPublishedAt()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var pet = Pet.CreateForOwner(
            Guid.NewGuid(),
            "Milo",
            PetSpecies.Dog,
            "Mixed",
            "2 years",
            PetSex.Male,
            PetSize.Medium,
            sterilized: true,
            vaccinated: true,
            description: "Friendly",
            temperament: "Playful",
            medicalHistory: "Healthy",
            locationDistrict: "Barranco",
            utcNow);

        pet.AddPhoto("/uploads/pets/photos/test.jpg", 1, utcNow);
        pet.Publish(utcNow.AddMinutes(5));

        pet.Status.Should().Be(PetStatus.Available);
        pet.PublishedAt.Should().Be(utcNow.AddMinutes(5));
    }

    [Fact]
    public void PendingAdoptionAndAdoptedTransitions_ShouldFollowExpectedFlow()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var pet = Pet.CreateForOwner(
            Guid.NewGuid(),
            "Milo",
            PetSpecies.Dog,
            "Mixed",
            "2 years",
            PetSex.Male,
            PetSize.Medium,
            sterilized: true,
            vaccinated: true,
            description: "Friendly",
            temperament: "Playful",
            medicalHistory: "Healthy",
            locationDistrict: "Barranco",
            utcNow);

        pet.AddPhoto("/uploads/pets/photos/test.jpg", 1, utcNow);
        pet.Publish(utcNow.AddMinutes(1));

        pet.CanMoveToPendingAdoption().Should().BeTrue();
        pet.MarkPendingAdoption(utcNow.AddMinutes(2));
        pet.Status.Should().Be(PetStatus.PendingAdoption);
        pet.CanMoveToAdopted().Should().BeTrue();

        pet.MarkAdopted(utcNow.AddMinutes(3));

        pet.Status.Should().Be(PetStatus.Adopted);
        pet.CanReceiveAdoptionRequests().Should().BeFalse();
    }
}
