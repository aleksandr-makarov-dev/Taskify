using Taskify.WebApi.Contracts.Requests;
using Taskify.WebApi.Contracts.Responses;
using Taskify.WebApi.Domain.Items;

namespace Taskify.WebApi.Mappings;

public static class ItemMapping
{
    public static Item ToItem(this CreateItemRequest source)
    {
        return new Item
        {
            Name = source.Name,
            Description = source.Description,
            Priority = source.Priority,
            DueDateOnUtc = source.DueDateOnUtc,
        };
    }

    public static ItemResponse ToItemResponse(this Item source)
    {
        return new ItemResponse
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

    public static ItemDetailsResponse ToItemDetailsResponse(this Item source)
    {
        return new ItemDetailsResponse
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
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
}