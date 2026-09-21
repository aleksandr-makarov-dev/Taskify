namespace Taskify.WebApi.Contracts.Requests;

public sealed record LoginUserByEmailRequest
{
    public required string Email { get; init; }
};