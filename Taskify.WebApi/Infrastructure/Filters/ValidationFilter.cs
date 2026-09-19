using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Taskify.WebApi.Infrastructure.Filters;

public sealed class ValidationFilter<T>(IEnumerable<IValidator<T>> validators) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var argument = context.ActionArguments.Values.OfType<T>().FirstOrDefault();

        if (argument is null)
        {
            await next();
            return;
        }

        var validationResults =
            await Task.WhenAll(validators.Select(validator =>
                validator.ValidateAsync(argument, context.HttpContext.RequestAborted)));

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        await next();
    }
}