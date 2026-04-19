using HairyPaws.Domain.Adoption.Entities;
using HairyPaws.Domain.Visits.Entities;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Adoption.Common;

internal static class AdoptionQueryExtensions
{
    public static IQueryable<AdoptionRequest> IncludeForList(this IQueryable<AdoptionRequest> query)
    {
        return query
            .Include(adoptionRequest => adoptionRequest.Pet)
                .ThenInclude(pet => pet.Photos)
            .Include(adoptionRequest => adoptionRequest.AdopterUser);
    }

    public static IQueryable<AdoptionRequest> IncludeForDetail(this IQueryable<AdoptionRequest> query)
    {
        return query
            .IncludeForList()
            .Include(adoptionRequest => adoptionRequest.ReviewedByUser)
            .Include(adoptionRequest => adoptionRequest.Visits);
    }

    public static IQueryable<Visit> IncludeForDetail(this IQueryable<Visit> query)
    {
        return query
            .Include(visit => visit.AdoptionRequest)
                .ThenInclude(adoptionRequest => adoptionRequest.Pet)
                    .ThenInclude(pet => pet.Photos)
            .Include(visit => visit.AdoptionRequest)
                .ThenInclude(adoptionRequest => adoptionRequest.AdopterUser);
    }
}
