namespace Taskify.WebApi.Domain.Items;

public sealed class Item : Entity, IAuditable, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Priority Priority { get; set; }
    public DateTime? DueDateOnUtc { get; set; }

    public DateTime CreatedAtUtc { get; init; }
    public DateTime? LastModifiedAtUtc { get; set; }

    public bool IsComplete { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public bool IsExpired { get; set; }
    public DateTime? ExpiredAtUtc { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}