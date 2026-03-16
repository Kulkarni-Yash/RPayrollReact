using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPayroll.API.DTOs.Attendance;
using RPayroll.API.Mappers;
using RPayroll.Infrastructure.Interfaces.Services;

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
    public async Task<IActionResult> Create([FromBody] AttendanceDto dto)
    {
        var result = await _attendanceService.CreateAttendanceAsync(dto.ToEntity());
        return Ok(result.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AttendanceDto dto)
    {
        dto.AttendanceId = id;
        var result = await _attendanceService.UpdateAttendanceAsync(dto.ToEntity());
        return result == null ? NotFound() : Ok(result.ToDto());
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        var result = await _attendanceService.GetByEmployeeAsync(employeeId);
        return Ok(result.Select(x => x.ToDto()));
    }

    [HttpGet("month/{employeeId:int}")]
    public async Task<IActionResult> GetByMonth(int employeeId, [FromQuery] int month, [FromQuery] int year)
    {
        var result = await _attendanceService.GetByMonthAsync(employeeId, month, year);
        return Ok(result.Select(x => x.ToDto()));
    }

    [HttpGet("date/{date}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> GetByDate(DateTime date)
    {
        var result = await _attendanceService.GetByDateAsync(date);
        return Ok(result.Select(x => x.ToDto()));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyAttendance()
    {
        var result = await _attendanceService.GetMyAttendanceAsync();
        return Ok(result.Select(x => x.ToDto()));
    }
}
