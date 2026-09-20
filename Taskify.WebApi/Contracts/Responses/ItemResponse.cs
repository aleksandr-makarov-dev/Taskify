using Taskify.WebApi.Domain;
using Taskify.WebApi.Domain.Items;

namespace Taskify.WebApi.Contracts.Responses;

public sealed record ItemResponse
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public Priority Priority { get; init; }
    public DateTime? DueDateOnUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? LastModifiedAtUtc { get; init; }
    public bool IsComplete { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public bool IsExpired { get; init; }
    public DateTime? ExpiredAtUtc { get; init; }
}