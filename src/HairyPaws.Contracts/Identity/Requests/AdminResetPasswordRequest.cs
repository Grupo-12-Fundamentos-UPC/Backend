namespace HairyPaws.Contracts.Identity.Requests;

public sealed record AdminResetPasswordRequest
{
    public Guid UserId { get; init; }

    public string NewPassword { get; init; } = string.Empty;
}
