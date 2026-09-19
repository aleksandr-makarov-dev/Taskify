using Taskify.WebApi.Domain;

namespace Taskify.WebApi.Contracts.Requests;

public sealed record CreateItemRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public Priority Priority { get; init; }
    public DateTime? DueDateOnUtc { get; init; }
}