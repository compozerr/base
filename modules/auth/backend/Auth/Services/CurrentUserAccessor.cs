using System.Net.Mail;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Auth.Services;

public interface ICurrentUserAccessor
{
    UserId? CurrentUserId { get; }
    MailAddress? CurrentUserEmail { get; }
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

    public MailAddress? CurrentUserEmail
    {
        get
        {
            var emailClaim = httpContextAccessor.HttpContext
                                                ?.User
                                                ?.FindFirst(ClaimTypes.Email)
                                                ?.Value;

            if (emailClaim is null)
                return null;

            try
            {
                return new MailAddress(emailClaim);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}