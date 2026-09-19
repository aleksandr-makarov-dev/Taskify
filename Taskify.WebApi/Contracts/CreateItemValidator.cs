using FluentValidation;

namespace Taskify.WebApi.Contracts;

internal sealed class CreateItemValidator : AbstractValidator<CreateItemRequest>
{
    public CreateItemValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Description)
            .MaximumLength(5000);

        RuleFor(x => x.Priority)
            .IsInEnum();

        RuleFor(x => x.DueDateAtUtc)
            .Must(x => x is null || x > DateTime.UtcNow)
            .WithMessage("Due date must be in the future.");
    }
}