using Microsoft.VisualBasic;
using Taskify.WebApi.Domain;

namespace Taskify.WebApi.Contracts;

public record ItemResponse(
    Guid Id,
    string Name,
    string? Description,
    Priority Priority,
    DateTime? DueDateAtUtc,
    DateTime CreatedAtUtc,
    DateTime? LastModifiedAtUtc
);