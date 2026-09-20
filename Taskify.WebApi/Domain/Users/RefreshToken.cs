namespace Taskify.WebApi.Domain.Users;

public sealed class RefreshToken : Entity, IAuditable
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid GroupId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokeReason { get; set; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? LastModifiedAtUtc { get; set; }
}