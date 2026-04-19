using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Users.Commands.UpdateMyProfile;
using HairyPaws.Application.Users.Queries.GetMyProfile;
using HairyPaws.Contracts.Users.Requests;
using HairyPaws.Contracts.Users.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairyPaws.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
[Route("api/v1/users")]
public sealed class UsersController : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileResponse>> Me(
        [FromServices] IQueryHandler<GetMyProfileQuery, UserProfileResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new GetMyProfileQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileResponse>> UpdateMe(
        [FromBody] UpdateMyProfileRequest request,
        [FromServices] ICommandHandler<UpdateMyProfileCommand, UserProfileResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new UpdateMyProfileCommand(
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.IdentityDocument,
                request.Address,
                request.ProfileImagePath),
            cancellationToken);

        return Ok(response);
    }
}
