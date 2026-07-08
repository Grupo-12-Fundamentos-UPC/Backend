using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Users.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Users.Queries.GetUsers;

public sealed record GetUsersQuery(
    int PageNumber,
    int PageSize,
    string? Role,
    string? Status,
    string? VerificationStatus,
    string? Search);

public sealed class GetUsersQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetUsersQuery, PagedResponse<UserSummaryResponse>>
{
    public async Task<PagedResponse<UserSummaryResponse>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        var users = dbContext.Users
            .AsNoTracking()
            .Where(user => user.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var role = ContractEnumMapper.ToUserRole(query.Role);
            users = users.Where(user => user.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = ContractEnumMapper.ToUserStatus(query.Status);
            users = users.Where(user => user.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.VerificationStatus))
        {
            var verificationStatus = ContractEnumMapper.ToVerificationStatus(query.VerificationStatus);
            users = users.Where(user => user.VerificationStatus == verificationStatus);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            users = users.Where(user =>
                user.Email.Contains(search) ||
                user.FirstName.ToLower().Contains(search) ||
                user.LastName.ToLower().Contains(search) ||
                (user.FirstName + " " + user.LastName).ToLower().Contains(search));
        }

        var totalCount = await users.LongCountAsync(cancellationToken);
        var items = await users
            .OrderByDescending(user => user.CreatedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return new PagedResponse<UserSummaryResponse>(
            items.Select(static user => user.ToSummaryResponse()).ToArray(),
            query.PageNumber,
            query.PageSize,
            totalCount,
            totalPages);
    }
}
