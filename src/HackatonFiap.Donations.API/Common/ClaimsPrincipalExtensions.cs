using System.Security.Claims;

namespace HackatonFiap.Donations.API.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub")
                  ?? user.FindFirstValue("nameid");
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    public static string GetEmail(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email") ?? string.Empty;

    public static string GetName(this ClaimsPrincipal user)
        => user.FindFirstValue("name")
           ?? user.FindFirstValue(ClaimTypes.Name)
           ?? user.FindFirstValue("unique_name")
           ?? string.Empty;
}
