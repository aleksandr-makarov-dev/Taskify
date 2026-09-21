using FluentValidation;
using Taskify.WebApi.Contracts.Requests;

namespace Taskify.WebApi.Contracts.Validators;

public sealed class LoginUserByPasswordValidator : AbstractValidator<RegisterUserRequest>
{
    public LoginUserByPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);
    }
}