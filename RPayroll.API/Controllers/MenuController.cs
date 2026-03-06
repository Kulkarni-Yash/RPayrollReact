using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPayroll.Domain.DTOs.Menu;

namespace RPayroll.API.Controllers;

[ApiController]
[Route("api/menu")]
public class MenuController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult GetMenu()
    {
        var menu = new List<MenuItemDto>
        {
            new() { Key = "dashboard", Label = "Dashboard", Order = 1 },
            new() { Key = "employee", Label = "Employee", Order = 2 }
        };

        return Ok(menu);
    }
}
