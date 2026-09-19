using Taskify.WebApi.Domain;

namespace Taskify.WebApi.Contracts;

public record CreateItemRequest(string Name, string? Description, Priority Priority, DateTime? DueDateAtUtc);