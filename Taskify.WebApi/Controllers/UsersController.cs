using System.Security.Cryptography;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Taskify.WebApi.Contracts.Requests;
using Taskify.WebApi.Domain.Users;
using Taskify.WebApi.Infrastructure.Exceptions;
using Taskify.WebApi.Infrastructure.Filters;
using Taskify.WebApi.Persistence;
using Taskify.WebApi.Services;
using UnauthorizedAccessException = Taskify.WebApi.Infrastructure.Exceptions.UnauthorizedAccessException;

namespace Taskify.WebApi.Controllers;

[AllowAnonymous]
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/users")]
public class UsersController(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    ILogger<UsersController> logger,
    IJsonWebTokenService jsonWebTokenService,
    TimeProvider timeProvider,
    ApplicationDbContext dbContext) : ControllerBase
{
    [HttpPost("register")]
    [Validate(typeof(RegisterUserRequest))]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);

        if (existingUser is not null)
        {
            throw new BadRequestException("User with this email already exists.");
        }

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = false
        };

        var createUserResult = await userManager.CreateAsync(user, request.Password);

        if (!createUserResult.Succeeded)
        {
            var error = createUserResult.Errors.FirstOrDefault();
            throw new BadRequestException(error?.Description ?? "Failed to create user.");
        }

        await userManager.AddToRoleAsync(user, RoleNames.User);

        // TODO: send email confirmation letter
        var emailConfirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);

        logger.LogInformation("Email confirmation token: {EmailConfirmationToken}", emailConfirmationToken);

        return Ok();
    }

    [HttpPost("login/password")]
    [Validate(typeof(LoginUserRequest))]
    public async Task<IActionResult> LoginUser([FromBody] LoginUserRequest request, CancellationToken cancellationToken)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);

        if (existingUser is null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        var checkPasswordResult =
            await signInManager.CheckPasswordSignInAsync(existingUser, request.Password, lockoutOnFailure: true);

        if (!checkPasswordResult.Succeeded)
        {
            if (checkPasswordResult.IsLockedOut)
            {
                logger.LogWarning("Login attempt for locked out user {UserId}", existingUser.Id);

                throw new UnauthorizedAccessException("Account is locked.");
            }

            if (checkPasswordResult.IsNotAllowed)
            {
                logger.LogWarning("Login attempt for not allowed user {UserId}", existingUser.Id);

                throw new UnauthorizedAccessException("User is not allowed to sign in.");
            }

            logger.LogWarning("Invalid password for user {UserId}", existingUser.Id);

            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var userRoles = await userManager.GetRolesAsync(existingUser);

        var refreshTokenAsBytes = RandomNumberGenerator.GetBytes(64);
        var refreshTokenAsString = Convert.ToBase64String(refreshTokenAsBytes);

        var refreshToken = new RefreshToken
        {
            UserId = existingUser.Id,
            TokenHash = Convert.ToHexString(SHA256.HashData(refreshTokenAsBytes)),
            GroupId = Guid.NewGuid(),
            ExpiresAtUtc = timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsRevoked = false
        };

        dbContext.RefreshTokens.Add(refreshToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = jsonWebTokenService.CreateToken(existingUser, userRoles);

        return Ok(new { AccessToken = accessToken, RefreshToken = refreshTokenAsString });
    }

    [HttpPost("verify-email")]
    [Validate(typeof(VerifyEmailRequest))]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);

        if (existingUser is null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        if (await userManager.IsEmailConfirmedAsync(existingUser))
        {
            return Ok();
        }

        var confirmEmailResult = await userManager.ConfirmEmailAsync(existingUser, request.Token);

        if (!confirmEmailResult.Succeeded)
        {
            var error = confirmEmailResult.Errors.FirstOrDefault();
            throw new UnauthorizedAccessException(error?.Description ?? "Failed to confirm email.");
        }

        return Ok();
    }
}