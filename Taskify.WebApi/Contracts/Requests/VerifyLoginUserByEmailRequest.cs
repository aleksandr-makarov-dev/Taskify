namespace Taskify.WebApi.Contracts.Requests;

public record VerifyLoginUserByEmailRequest
{
    public required string Email { get; init; }
    public required string Token { get; init; }
}