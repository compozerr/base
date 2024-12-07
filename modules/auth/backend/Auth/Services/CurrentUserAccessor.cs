using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Auth.Services;

public interface ICurrentUserAccessor
{
    UserId? CurrentUserId { get; }
}

public class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public UserId? CurrentUserId
    {
        get
        {
            return ToUserId(httpContextAccessor.HttpContext
                                               ?.User
                                               ?.FindFirst(ClaimTypes.NameIdentifier)
                                               ?.Value);

            static UserId? ToUserId(string? subject)
            {
                if (subject is null)
                    return null;

                return UserId.Parse(subject);
            }
        }
    }
}