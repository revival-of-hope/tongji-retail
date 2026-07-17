using System.Security.Claims;

namespace RetailSystem.Backend.Services;

/// <summary>
/// 从 JWT 建立的 ClaimsPrincipal 中读取当前用户信息。
/// </summary>
public static class CurrentUserExtensions
{
    public static long GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(value, out var userId) || userId <= 0)
        {
            throw new UnauthorizedAccessException("JWT 中缺少有效的用户标识。");
        }

        return userId;
    }

    public static string GetRoleName(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}
