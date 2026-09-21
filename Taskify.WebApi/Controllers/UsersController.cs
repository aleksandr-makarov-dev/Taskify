using System.Security.Cryptography;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Taskify.WebApi.Contracts.Requests;
using Taskify.WebApi.Domain.Users;
using Taskify.WebApi.Infrastructure.Exceptions;
using Taskify.WebApi.Infrastructure.Filters;
using Taskify.WebApi.Infrastructure.Options;
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
    ApplicationDbContext dbContext,
    IOptions<RefreshTokenOptions> refreshTokenOptions) : ControllerBase
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
    [Validate(typeof(LoginUserByPasswordRequest))]
    public async Task<IActionResult> LoginUserByPassword([FromBody] LoginUserByPasswordRequest byPasswordRequest,
        CancellationToken cancellationToken)
    {
        var existingUser = await userManager.FindByEmailAsync(byPasswordRequest.Email);

        if (existingUser is null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        if (!await userManager.HasPasswordAsync(existingUser))
        {
            throw new UnauthorizedAccessException("Use other sign in method.");
        }

        var checkPasswordResult =
            await signInManager.CheckPasswordSignInAsync(existingUser, byPasswordRequest.Password,
                lockoutOnFailure: true);

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
            ExpiresAtUtc = timeProvider.GetUtcNow().UtcDateTime.Add(refreshTokenOptions.Value.Expiration),
            IsRevoked = false
        };

        dbContext.RefreshTokens.Add(refreshToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = jsonWebTokenService.CreateToken(existingUser, userRoles);

        Response.Cookies.Append("refresh_token", refreshTokenAsString, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = refreshToken.ExpiresAtUtc,
        });

        return Ok(new { AccessToken = accessToken });
    }

    [HttpPost("login/email")]
    [Validate(typeof(LoginUserByEmailRequest))]
    public async Task<IActionResult> LoginUserByEmail([FromBody] LoginUserByEmailRequest request,
        CancellationToken cancellationToken)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);

        if (existingUser is null)
        {
            // if user does not exist, create new user
            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = false
            };

            var createUserResult = await userManager.CreateAsync(user);

            if (!createUserResult.Succeeded)
            {
                var error = createUserResult.Errors.FirstOrDefault();
                throw new BadRequestException(error?.Description ?? "Failed to create user.");
            }

            await userManager.AddToRoleAsync(user, RoleNames.User);

            existingUser = user;
        }

        var emailConfirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(existingUser);

        logger.LogInformation("Email confirmation token: {EmailConfirmationToken}", emailConfirmationToken);

        return Ok();
    }

    [HttpPost("login/email/verify")]
    [Validate(typeof(VerifyLoginUserByEmailRequest))]
    public async Task<IActionResult> VerifyLoginUserByEmail(
        [FromBody] VerifyLoginUserByEmailRequest request,
        CancellationToken cancellationToken)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);

        if (existingUser is null)
        {
            throw new UnauthorizedAccessException("The email address or verification link is invalid.");
        }

        var isValid = await userManager.VerifyUserTokenAsync(
            existingUser,
            TokenOptions.DefaultProvider,
            UserManager<User>.ConfirmEmailTokenPurpose,
            request.Token);

        if (!isValid)
        {
            throw new UnauthorizedAccessException("The verification link is invalid or has expired.");
        }

        if (!existingUser.EmailConfirmed)
        {
            existingUser.EmailConfirmed = true;

            var updateResult = await userManager.UpdateAsync(existingUser);

            if (!updateResult.Succeeded)
            {
                var error = updateResult.Errors.FirstOrDefault();

                throw new InvalidOperationException(error?.Description ?? "Failed to confirm the email address.");
            }
        }

        var userRoles = await userManager.GetRolesAsync(existingUser);

        var refreshTokenAsBytes = RandomNumberGenerator.GetBytes(64);
        var refreshTokenAsString = Convert.ToBase64String(refreshTokenAsBytes);

        var refreshToken = new RefreshToken
        {
            UserId = existingUser.Id,
            TokenHash = Convert.ToHexString(SHA256.HashData(refreshTokenAsBytes)),
            GroupId = Guid.NewGuid(),
            ExpiresAtUtc = timeProvider.GetUtcNow().UtcDateTime
                .Add(refreshTokenOptions.Value.Expiration),
            IsRevoked = false
        };

        dbContext.RefreshTokens.Add(refreshToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = jsonWebTokenService.CreateToken(existingUser, userRoles);

        Response.Cookies.Append("refresh_token", refreshTokenAsString, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = refreshToken.ExpiresAtUtc,
        });

        return Ok(new { AccessToken = accessToken });
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

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue("refresh_token", out var refreshTokenAsString) ||
            string.IsNullOrEmpty(refreshTokenAsString))
        {
            logger.LogWarning("Refresh token cookie is missing.");

            throw new UnauthorizedAccessException("Refresh token cookie not found.");
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var refreshTokenAsBytes = Convert.FromBase64String(refreshTokenAsString);
        var tokenHash = Convert.ToHexString(SHA256.HashData(refreshTokenAsBytes));

        var refreshToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            logger.LogWarning("Refresh token was not found.");

            throw new UnauthorizedAccessException("Refresh token not found.");
        }

        if (refreshToken.IsRevoked)
        {
            logger.LogWarning(
                "Refresh token reuse detected for user {UserId} and token group {GroupId}. Revoking token group.",
                refreshToken.UserId,
                refreshToken.GroupId);

            await dbContext.RefreshTokens
                .Where(x => x.GroupId == refreshToken.GroupId && !x.IsRevoked)
                .ExecuteUpdateAsync(setters =>
                {
                    setters.SetProperty(x => x.IsRevoked, true);
                    setters.SetProperty(x => x.RevokedAtUtc, utcNow);
                    setters.SetProperty(x => x.RevokeReason, "token_reuse_detected");
                    setters.SetProperty(x => x.LastModifiedAtUtc, utcNow);
                }, cancellationToken);

            throw new UnauthorizedAccessException("Token is revoked.");
        }

        if (refreshToken.ExpiresAtUtc <= utcNow)
        {
            logger.LogWarning(
                "Expired refresh token used by user {UserId}. Token expired at {ExpiresAtUtc}.",
                refreshToken.UserId,
                refreshToken.ExpiresAtUtc);

            throw new UnauthorizedAccessException("Refresh token is expired.");
        }

        var user = await userManager.FindByIdAsync(refreshToken.UserId.ToString());

        if (user is null)
        {
            logger.LogWarning("User {UserId} associated with refresh token was not found.", refreshToken.UserId);

            throw new UnauthorizedAccessException("User not found.");
        }

        var roles = await userManager.GetRolesAsync(user);

        var newAccessToken = jsonWebTokenService.CreateToken(user, roles);

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAtUtc = utcNow;
        refreshToken.RevokeReason = "token_refresh";

        var newRefreshTokenAsBytes = RandomNumberGenerator.GetBytes(64);
        var newRefreshTokenAsString = Convert.ToBase64String(newRefreshTokenAsBytes);

        var newRefreshToken = new RefreshToken
        {
            UserId = refreshToken.UserId,
            TokenHash = Convert.ToHexString(SHA256.HashData(newRefreshTokenAsBytes)),
            GroupId = refreshToken.GroupId,
            ExpiresAtUtc = utcNow.Add(refreshTokenOptions.Value.Expiration),
            IsRevoked = false
        };

        dbContext.RefreshTokens.Add(newRefreshToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        Response.Cookies.Append("refresh_token", newRefreshTokenAsString, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = newRefreshToken.ExpiresAtUtc,
        });

        logger.LogInformation(
            "Refresh token successfully rotated for user {UserId} and token group {GroupId}.",
            refreshToken.UserId,
            refreshToken.GroupId);

        return Ok(new { AccessToken = newAccessToken });
    }
}