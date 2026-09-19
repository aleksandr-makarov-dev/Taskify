using Taskify.WebApi.Domain;

namespace Taskify.WebApi.Contracts.Responses;

public sealed record ItemResponse
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public Priority Priority { get; init; }
    public DateTime? DueDateOnUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? LastModifiedAtUtc { get; init; }
}