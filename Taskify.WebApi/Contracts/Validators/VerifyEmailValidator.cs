using FluentValidation;
using Taskify.WebApi.Contracts.Requests;

namespace Taskify.WebApi.Contracts.Validators;

public sealed class VerifyEmailValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Token)
            .NotEmpty();
    }
}