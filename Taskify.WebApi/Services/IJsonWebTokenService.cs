using Taskify.WebApi.Domain.Users;

namespace Taskify.WebApi.Services;

public interface IJsonWebTokenService
{
    public string CreateToken(User user, IList<string> roles);
}