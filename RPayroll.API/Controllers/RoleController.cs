using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPayroll.API.DTOs.Role;
using RPayroll.API.Mappers;
using RPayroll.Infrastructure.Interfaces.Services;

namespace RPayroll.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] RoleDto dto)
    {
        var role = await _roleService.CreateRoleAsync(dto.ToEntity());
        return Ok(role.ToDto());
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _roleService.GetAllRolesAsync();
        return Ok(roles.Select(x => x.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var role = await _roleService.GetRoleByIdAsync(id);
        return role == null ? NotFound() : Ok(role.ToDto());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] RoleDto dto)
    {
        dto.Id = id;
        var role = await _roleService.UpdateRoleAsync(dto.ToEntity());
        return role == null ? NotFound() : Ok(role.ToDto());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _roleService.DeleteRoleAsync(id);
        return result ? Ok() : NotFound();
    }
}
