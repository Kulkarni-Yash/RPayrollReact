using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPayroll.API.DTOs.User;
using RPayroll.API.Mappers;
using RPayroll.Infrastructure.Interfaces.Services;

namespace RPayroll.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] UserDto dto)
    {
        var user = await _userService.CreateUserAsync(dto.ToEntity());
        return Ok(user.ToDto());
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users.Select(x => x.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        return user == null ? NotFound() : Ok(user.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UserDto dto)
    {
        dto.Id = id;
        var user = await _userService.UpdateUserAsync(dto.ToEntity());
        return user == null ? NotFound() : Ok(user.ToDto());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Disable(int id)
    {
        var result = await _userService.DisableUserAsync(id);
        return result ? Ok() : NotFound();
    }
}
