using FluentValidation;
using Taskify.WebApi.Contracts.Requests;

namespace Taskify.WebApi.Contracts.Validators;

public sealed class VerifyLoginUserByEmailValidator : AbstractValidator<VerifyLoginUserByEmailRequest>
{
    public VerifyLoginUserByEmailValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Token)
            .NotEmpty();
    }
}