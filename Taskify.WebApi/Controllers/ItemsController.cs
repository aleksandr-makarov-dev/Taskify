using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Taskify.WebApi.Contracts.Requests;
using Taskify.WebApi.Domain.Items;
using Taskify.WebApi.Domain.Users;
using Taskify.WebApi.Infrastructure.Exceptions;
using Taskify.WebApi.Infrastructure.Filters;
using Taskify.WebApi.Mappings;
using Taskify.WebApi.Persistence;

namespace Taskify.WebApi.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/items")]
public class ItemsController(ApplicationDbContext dbContext, TimeProvider timeProvider) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetItems(CancellationToken cancellationToken)
    {
        var items = await dbContext.Items
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(ItemProjections.ToItemResponse)
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetItem(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item is null)
        {
            throw new NotFoundException(nameof(Item), id);
        }

        return Ok(item.ToItemDetailsResponse());
    }

    [HttpPost]
    [Validate(typeof(CreateItemRequest))]
    public async Task<IActionResult> CreateItem([FromBody] CreateItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = request.ToItem();

        dbContext.Items.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(item.ToItemDetailsResponse());
    }

    [HttpPut("{id:guid}")]
    [Validate(typeof(UpdateItemRequest))]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.Items.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item is null)
        {
            throw new NotFoundException(nameof(Item), id);
        }

        item.Name = request.Name;
        item.Description = request.Description;
        item.Priority = request.Priority;
        item.DueDateOnUtc = request.DueDateOnUtc;

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/complete")]
    public async Task<IActionResult> CompleteItem(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item is null)
        {
            throw new NotFoundException(nameof(Item), id);
        }

        item.IsComplete = true;
        item.CompletedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item is null)
        {
            throw new NotFoundException(nameof(Item), id);
        }

        dbContext.Items.Remove(item);

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> RestoreItem(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted, cancellationToken);

        if (item is null)
        {
            throw new NotFoundException(nameof(Item), id);
        }

        item.IsDeleted = false;
        item.DeletedAtUtc = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("trash")]
    public async Task<IActionResult> GetTrashItems(CancellationToken cancellationToken)
    {
        var items = await dbContext.Items
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .Where(x => x.IsDeleted)
            .OrderByDescending(x => x.DeletedAtUtc)
            .Select(ItemProjections.ToItemResponse)
            .ToListAsync(cancellationToken);

        return Ok(items);
    }
}