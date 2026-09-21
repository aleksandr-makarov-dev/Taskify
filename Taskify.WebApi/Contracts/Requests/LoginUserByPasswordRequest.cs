namespace Taskify.WebApi.Contracts.Requests;

public sealed record LoginUserByPasswordRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
};