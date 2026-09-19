using Taskify.WebApi.Domain;

namespace Taskify.WebApi.Contracts.Requests;

public record UpdateItemRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public Priority Priority { get; init; }
    public DateTime? DueDateOnUtc { get; init; }
}