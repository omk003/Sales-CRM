using System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    public static string GetUserRole(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.Role)?.Value;
        return claim;
    }
}
