using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPayroll.API.DTOs.Auth;
using RPayroll.API.Mappers;
using RPayroll.Infrastructure.Interfaces.Common;
using RPayroll.Infrastructure.Interfaces.Services;
using RPayroll.Repositories.Services;

namespace RPayroll.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserContext _currentUser;

    public AuthController(IAuthService authService, ICurrentUserContext currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var user = await _authService.LoginAsync(request.Username, request.Password);
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(user.ToLoginResponseDto());
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync(_currentUser.UserId);
        return Ok();
    }
}
