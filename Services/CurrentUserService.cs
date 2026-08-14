using System.Security.Claims;

namespace ClinicaOdontologica.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? GetCurrentUnitId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return null;

        var unitIdClaim = user.FindFirst("UnitId");
        if (unitIdClaim != null && int.TryParse(unitIdClaim.Value, out var unitId))
        {
            return unitId;
        }

        return null;
    }

    public string GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    public bool IsAdmin()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.IsInRole("Admin") ?? false;
    }
}
