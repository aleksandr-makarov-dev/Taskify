using System.Net;

namespace Taskify.WebApi.Infrastructure.Exceptions;

public sealed class BadRequestException(string? message) : ApplicationException(message, HttpStatusCode.BadRequest);