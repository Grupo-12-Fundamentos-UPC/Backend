namespace HairyPaws.Contracts.Users.Requests;

public sealed record UpdateUserStatusRequest
{
    public string Status { get; init; } = string.Empty;
}
