namespace Taskify.WebApi.Contracts.Requests;

public sealed record LoginUserRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
};