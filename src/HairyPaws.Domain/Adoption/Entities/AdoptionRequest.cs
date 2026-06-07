using HairyPaws.Domain.Adoption.Enums;
using HairyPaws.Domain.Common.Abstractions;
using HairyPaws.Domain.Identity.Entities;
using HairyPaws.Domain.Pets.Entities;
using HairyPaws.Domain.Visits.Entities;

namespace HairyPaws.Domain.Adoption.Entities;

public sealed class AdoptionRequest : 
{
    private readonly List<Visit> _visits = [];

    private AdoptionRequest()
    {
    }

    private AdoptionRequest(
        Guid petId,
        Guid adopterUserId,
        string contactPhone,
        string? livingConditions,
        bool hasPreviousPets,
        string whyAdopt,
        DateTimeOffset utcNow)
    {
        Id = Guid.NewGuid();
        PetId = petId;
        AdopterUserId = adopterUserId;
        Status = AdoptionRequestStatus.Submitted;
        ContactPhone = NormalizeRequired(contactPhone);
        LivingConditions = NormalizeOptional(livingConditions);
        HasPreviousPets = hasPreviousPets;
        WhyAdopt = NormalizeRequired(whyAdopt);
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public Guid PetId { get; private set; }

    public Pet Pet { get; private set; } = null!;

    public Guid AdopterUserId { get; private set; }

    public User AdopterUser { get; private set; } = null!;

    public AdoptionRequestStatus Status { get; private set; }

    public string ContactPhone { get; private set; } = string.Empty;

    public string? LivingConditions { get; private set; }

    public bool HasPreviousPets { get; private set; }

    public string WhyAdopt { get; private set; } = string.Empty;

    public string? ReviewNotes { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }

    public User? ReviewedByUser { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<Visit> Visits => _visits;

    public static AdoptionRequest Create(
        Guid petId,
        Guid adopterUserId,
        string contactPhone,
        string? livingConditions,
        bool hasPreviousPets,
        string whyAdopt,
        DateTimeOffset utcNow)
    {
        return new AdoptionRequest(
            petId,
            adopterUserId,
            contactPhone,
            livingConditions,
            hasPreviousPets,
            whyAdopt,
            utcNow);
    }

    public bool IsOwnedByAdopter(Guid userId) => AdopterUserId == userId;

    public bool HasActiveStatus() => IsActiveStatus(Status);

    public bool CanStartReview() => Status == AdoptionRequestStatus.Submitted;

    public bool CanApprove() => Status is AdoptionRequestStatus.Submitted or AdoptionRequestStatus.UnderReview;

    public bool CanReject() => Status is AdoptionRequestStatus.Submitted or AdoptionRequestStatus.UnderReview;

    public bool CanCancelByAdopter() => Status is AdoptionRequestStatus.Submitted or AdoptionRequestStatus.UnderReview;

    public bool CanComplete() => Status == AdoptionRequestStatus.Approved;

    public bool CanCreateVisit() => Status == AdoptionRequestStatus.UnderReview;

    public static bool IsActiveStatus(AdoptionRequestStatus status)
        => status is AdoptionRequestStatus.Submitted or AdoptionRequestStatus.UnderReview or AdoptionRequestStatus.Approved;

    public void StartReview(Guid reviewedByUserId, string? reviewNotes, DateTimeOffset utcNow)
    {
        Status = AdoptionRequestStatus.UnderReview;
        ReviewNotes = NormalizeOptional(reviewNotes);
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void Approve(Guid reviewedByUserId, string? reviewNotes, DateTimeOffset utcNow)
    {
        Status = AdoptionRequestStatus.Approved;
        ReviewNotes = NormalizeOptional(reviewNotes);
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void Reject(Guid reviewedByUserId, string? reviewNotes, DateTimeOffset utcNow)
    {
        Status = AdoptionRequestStatus.Rejected;
        ReviewNotes = NormalizeOptional(reviewNotes);
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void AutoReject(Guid? reviewedByUserId, string? reviewNotes, DateTimeOffset utcNow)
    {
        Status = AdoptionRequestStatus.Rejected;
        ReviewNotes = NormalizeOptional(reviewNotes);
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void Cancel(string? reviewNotes, DateTimeOffset utcNow)
    {
        Status = AdoptionRequestStatus.Cancelled;
        ReviewNotes = NormalizeOptional(reviewNotes);
        UpdatedAt = utcNow;
    }

    public void Complete(Guid reviewedByUserId, string? reviewNotes, DateTimeOffset utcNow)
    {
        Status = AdoptionRequestStatus.Completed;
        ReviewNotes = NormalizeOptional(reviewNotes);
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public Visit AddVisit(DateTimeOffset scheduledAt, string? location, string? notes, DateTimeOffset utcNow)
    {
        var visit = Visit.Create(Id, scheduledAt, location, notes, utcNow);
        _visits.Add(visit);
        UpdatedAt = utcNow;
        return visit;
    }

    public void CancelActiveVisits(string? notes, DateTimeOffset utcNow)
    {
        foreach (var visit in _visits.Where(static candidate => candidate.HasActiveStatus()))
        {
            visit.Cancel(notes, utcNow);
        }

        UpdatedAt = utcNow;
    }

    private static string NormalizeRequired(string value) => value.Trim();

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
