using FluentValidation;
using Taskify.WebApi.Contracts.Requests;

namespace Taskify.WebApi.Contracts.Validators;

public sealed class UpdateItemValidator : AbstractValidator<CreateItemRequest>
{
    public UpdateItemValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Description)
            .MaximumLength(5000);

        RuleFor(x => x.Priority)
            .IsInEnum();

        RuleFor(x => x.DueDateOnUtc)
            .Must(x => x is null || x > timeProvider.GetUtcNow().UtcDateTime)
            .WithMessage("Due date must be in the future.");
    }
}