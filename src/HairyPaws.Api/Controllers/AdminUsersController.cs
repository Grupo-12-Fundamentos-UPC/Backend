using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Users.Commands.UpdateUserStatus;
using HairyPaws.Application.Users.Commands.UpdateUserVerification;
using HairyPaws.Application.Users.Queries.GetUsers;
using HairyPaws.Contracts.Common.Requests;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Users.Requests;
using HairyPaws.Contracts.Users.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairyPaws.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
[Route("api/v1/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UserSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<UserSummaryResponse>>> GetUsers(
        [FromQuery] GetUsersQueryParameters queryParameters,
        [FromServices] IQueryHandler<GetUsersQuery, PagedResponse<UserSummaryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new GetUsersQuery(
                queryParameters.PageNumber,
                queryParameters.PageSize,
                queryParameters.Role,
                queryParameters.Status,
                queryParameters.VerificationStatus,
                queryParameters.Search),
            cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(UserSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummaryResponse>> UpdateStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
        [FromServices] ICommandHandler<UpdateUserStatusCommand, UserSummaryResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new UpdateUserStatusCommand(id, request.Status), cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{id:guid}/verify")]
    [ProducesResponseType(typeof(UserSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummaryResponse>> UpdateVerification(
        Guid id,
        [FromBody] UpdateUserVerificationRequest request,
        [FromServices] ICommandHandler<UpdateUserVerificationCommand, UserSummaryResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new UpdateUserVerificationCommand(id, request.VerificationStatus), cancellationToken);
        return Ok(response);
    }
}
