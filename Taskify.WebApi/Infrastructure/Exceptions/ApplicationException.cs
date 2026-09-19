using System.Net;

namespace Taskify.WebApi.Infrastructure.Exceptions;

public class ApplicationException : Exception
{
    public HttpStatusCode StatusCode { get; }

    protected ApplicationException(string? message = null,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError) : base(message)
    {
        StatusCode = statusCode;
    }

    protected ApplicationException(string? message, Exception? innerException,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError) : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}