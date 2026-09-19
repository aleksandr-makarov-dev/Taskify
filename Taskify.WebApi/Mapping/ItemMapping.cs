using Taskify.WebApi.Contracts.Requests;
using Taskify.WebApi.Contracts.Responses;
using Taskify.WebApi.Domain;

namespace Taskify.WebApi.Mapping;

public static class ItemMapping
{
    public static ItemResponse ToItemResponse(this Item source)
    {
        return new ItemResponse
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Priority = source.Priority,
            DueDateOnUtc = source.DueDateOnUtc,
            CreatedAtUtc = source.CreatedAtUtc,
            LastModifiedAtUtc = source.LastModifiedAtUtc
        };
    }

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
}