using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPayroll.Domain.DTOs.Attendance;
using RPayroll.Domain.Interfaces.Services;

namespace RPayroll.API.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAttendanceDto dto)
    {
        try
        {
            var result = await _attendanceService.CreateAttendanceAsync(dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAttendanceDto dto)
    {
        dto.AttendanceId = id;
        try
        {
            var result = await _attendanceService.UpdateAttendanceAsync(dto);
            return result == null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        var result = await _attendanceService.GetByEmployeeAsync(employeeId);
        return Ok(result);
    }

    [HttpGet("month/{employeeId:int}")]
    public async Task<IActionResult> GetByMonth(int employeeId, [FromQuery] int month, [FromQuery] int year)
    {
        var result = await _attendanceService.GetByMonthAsync(employeeId, month, year);
        return Ok(result);
    }

    [HttpGet("date/{date}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> GetByDate(DateTime date)
    {
        var result = await _attendanceService.GetByDateAsync(date);
        return Ok(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyAttendance()
    {
        var result = await _attendanceService.GetMyAttendanceAsync();
        return Ok(result);
    }
}
