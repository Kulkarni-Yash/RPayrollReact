using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace RPayroll.API.Services;

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public int UserId => int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public string Role => Principal?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public int HierarchyLevel => int.TryParse(Principal?.FindFirstValue("HierarchyLevel"), out var level) ? level : int.MaxValue;

    public int? EmployeeId => int.TryParse(Principal?.FindFirstValue("EmployeeId"), out var id) ? id : null;
}
