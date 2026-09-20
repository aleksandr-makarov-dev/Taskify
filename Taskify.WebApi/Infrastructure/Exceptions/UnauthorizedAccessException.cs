using System.Net;

namespace Taskify.WebApi.Infrastructure.Exceptions;

public class UnauthorizedAccessException(string? message) : ApplicationException(message, HttpStatusCode.Unauthorized);