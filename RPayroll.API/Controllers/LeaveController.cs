using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPayroll.API.DTOs.Leave;
using RPayroll.API.Mappers;
using RPayroll.Infrastructure.Interfaces.Services;

namespace RPayroll.API.Controllers;

[ApiController]
[Route("api/leave")]
[Authorize]
public class LeaveController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeaveController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    [HttpPost]
    public async Task<IActionResult> Apply([FromBody] LeaveRequestDto dto)
    {
        var result = await _leaveService.ApplyLeaveAsync(dto.ToEntity());
        return Ok(result.ToDto());
    }

    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _leaveService.ApproveLeaveAsync(id);
        return result == null ? NotFound() : Ok(result.ToDto());
    }

    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> Reject(int id)
    {
        var result = await _leaveService.RejectLeaveAsync(id);
        return result == null ? NotFound() : Ok(result.ToDto());
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        var leaves = await _leaveService.GetLeavesByEmployeeAsync(employeeId);
        return Ok(leaves.Select(x => x.ToDto()));
    }

    [HttpGet]
    public async Task<IActionResult> GetForCurrentUser()
    {
        var leaves = await _leaveService.GetLeavesForCurrentUserAsync();
        return Ok(leaves.Select(x => x.ToDto()));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyLeaves()
    {
        var leaves = await _leaveService.GetMyLeavesAsync();
        return Ok(leaves.Select(x => x.ToDto()));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> SoftDelete(int id)
    {
        var result = await _leaveService.SoftDeleteLeaveAsync(id);
        return result ? Ok() : NotFound();
    }
}
