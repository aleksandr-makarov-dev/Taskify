using Microsoft.AspNetCore.Identity;

namespace Taskify.WebApi.Domain.Users;

public sealed class User : IdentityUser<Guid>
{
}