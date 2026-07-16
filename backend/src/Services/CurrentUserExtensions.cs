using System.Security.Claims;

namespace RetailSystem.Api.Services;

public static class CurrentUserExtensions
{
    public static long GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("missing user id claim");
        return long.Parse(value);
    }

    public static string GetRoleName(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}
