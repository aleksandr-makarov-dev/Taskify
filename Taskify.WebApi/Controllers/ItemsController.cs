using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Taskify.WebApi.Contracts;
using Taskify.WebApi.Infrastructure.Filters;

namespace Taskify.WebApi.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/items")]
public class ItemsController : ControllerBase
{
    [HttpPost]
    [Validate<CreateItemRequest>]
    public IActionResult Post([FromBody] CreateItemRequest item, CancellationToken cancellationToken)
    {
        var response = new ItemResponse(
            Guid.NewGuid(),
            item.Name,
            item.Description,
            item.Priority,
            item.DueDateAtUtc,
            DateTime.UtcNow,
            null
        );

        return Ok(response);
    }
}