using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Taskify.WebApi.Contracts.Requests;
using Taskify.WebApi.Infrastructure.Filters;
using Taskify.WebApi.Mapping;
using Taskify.WebApi.Persistence;

namespace Taskify.WebApi.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/items")]
public class ItemsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpPost]
    [Validate<CreateItemRequest>]
    public async Task<IActionResult> Post([FromBody] CreateItemRequest request, CancellationToken cancellationToken)
    {
        var item = request.ToItem();

        dbContext.Items.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(item.ToItemResponse());
    }
}