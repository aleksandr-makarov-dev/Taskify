using System.Net;

namespace Taskify.WebApi.Infrastructure.Exceptions;

public sealed class NotFoundException(string identifier, object key)
    : ApplicationException($"{identifier} with id {key} not found", HttpStatusCode.NotFound);