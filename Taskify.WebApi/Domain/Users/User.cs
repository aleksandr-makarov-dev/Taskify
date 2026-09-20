using System.Collections;
using Microsoft.AspNetCore.Identity;

namespace Taskify.WebApi.Domain.Users;

public sealed class User : IdentityUser<Guid>
{
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}