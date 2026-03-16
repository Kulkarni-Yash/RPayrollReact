using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPayroll.API.DTOs.Menu;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.Interfaces.Services;

namespace RPayroll.API.Controllers;

[ApiController]
[Route("api/menu")]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMenu()
    {
        var menu = await _menuService.GetVisibleMenuAsync();
        return Ok(menu.Select(MapMenu));
    }

    [HttpGet("manage")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllMenuItems()
    {
        var items = await _menuService.GetAllMenuItemsAsync();
        return Ok(items.Select(MapMenu));
    }

    [HttpPut("manage/{key}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(string key, [FromBody] UpdateMenuStatusDto dto)
    {
        var item = await _menuService.SetActiveAsync(key, dto.IsActive);
        return item == null ? NotFound() : Ok(MapMenu(item));
    }

    private static MenuItemDto MapMenu(RPayroll.Domain.Entities.AppMenuItem item)
    {
        return new MenuItemDto
        {
            Key = item.Key,
            Label = item.Label,
            Order = item.Order,
            IsActive = item.Status == RPayroll.Domain.Enums.StatusCode.Accepted,
            IsSystem = item.IsSystem,
            FormLayoutKey = item.FormLayoutKey,
            VisibilityScope = (int)item.VisibilityScope,
            MaxHierarchyLevelAllowed = item.MaxHierarchyLevelAllowed,
            OwnerUserId = item.OwnerUserId
        };
    }
}
