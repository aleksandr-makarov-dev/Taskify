namespace Taskify.WebApi.Contracts.Requests;

public sealed record VerifyEmailRequest
{
    public required string Email { get; init; }
    public required string Token { get; init; }
};