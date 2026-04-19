using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Identity.Commands.AdminResetPassword;
using HairyPaws.Application.Identity.Commands.ChangePassword;
using HairyPaws.Application.Identity.Commands.Login;
using HairyPaws.Application.Identity.Commands.RefreshToken;
using HairyPaws.Application.Identity.Commands.Register;
using HairyPaws.Application.Identity.Queries.GetCurrentAuthUser;
using HairyPaws.Contracts.Identity.Requests;
using HairyPaws.Contracts.Identity.Responses;
using HairyPaws.Contracts.Users.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairyPaws.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserSummaryResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<UserSummaryResponse>> Register(
        [FromBody] RegisterRequest request,
        [FromServices] ICommandHandler<RegisterCommand, UserSummaryResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new RegisterCommand(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.Role,
                request.PhoneNumber,
                request.IdentityDocument,
                request.Address),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        [FromServices] ICommandHandler<LoginCommand, AuthResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new LoginCommand(request.Email, request.Password), cancellationToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshTokenRequest request,
        [FromServices] ICommandHandler<RefreshTokenCommand, AuthResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new RefreshTokenCommand(request.RefreshToken), cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummaryResponse>> Me(
        [FromServices] IQueryHandler<GetCurrentAuthUserQuery, UserSummaryResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new GetCurrentAuthUserQuery(), cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        [FromServices] ICommandHandler<ChangePasswordCommand> handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [HttpPost("admin-reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AdminResetPassword(
        [FromBody] AdminResetPasswordRequest request,
        [FromServices] ICommandHandler<AdminResetPasswordCommand> handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(new AdminResetPasswordCommand(request.UserId, request.NewPassword), cancellationToken);
        return NoContent();
    }
}
