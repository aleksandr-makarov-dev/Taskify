using FluentValidation;
using Taskify.WebApi.Contracts.Requests;

namespace Taskify.WebApi.Contracts.Validators;

public sealed class LoginUserByEmailValidator : AbstractValidator<LoginUserByEmailRequest>
{
    public LoginUserByEmailValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}