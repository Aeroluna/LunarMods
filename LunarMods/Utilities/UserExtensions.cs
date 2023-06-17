using System.Security.Claims;
using LunarMods.Models;

namespace LunarMods.Utilities;

public static class UserExtensions
{
    public static bool CanModerate(this ClaimsPrincipal user, Mod mod)
    {
        bool result = user.IsInRole("moderator");

        // ReSharper disable once InvertIf
        if (!result)
        {
            result = user.CanUpload(mod);
        }

        return result;
    }

    public static bool CanUpload(this ClaimsPrincipal user, Mod mod)
    {
        string? userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return (user.IsInRole("uploader") && userId != null && mod.Author == ulong.Parse(userId));
    }
}
