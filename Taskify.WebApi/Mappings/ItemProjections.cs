using System.Linq.Expressions;
using Taskify.WebApi.Contracts.Responses;
using Taskify.WebApi.Domain;

namespace Taskify.WebApi.Mappings;

public static class ItemProjections
{
    public static Expression<Func<Item, ItemResponse>> ToItemResponse = source =>
        new ItemResponse
        {
            Id = source.Id,
            Name = source.Name,
            Priority = source.Priority,
            DueDateOnUtc = source.DueDateOnUtc,
            CreatedAtUtc = source.CreatedAtUtc,
            LastModifiedAtUtc = source.LastModifiedAtUtc,
            IsComplete = source.IsComplete,
            CompletedAtUtc = source.CompletedAtUtc,
            IsExpired = source.IsExpired,
            ExpiredAtUtc = source.ExpiredAtUtc,
        };
}