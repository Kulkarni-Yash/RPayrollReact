using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.UnitOfWork;

namespace RPayroll.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("attendance-stats")]
    public async Task<IActionResult> GetAttendanceStats()
    {
        var today = DateTime.UtcNow.Date;
        var items = await _unitOfWork.Attendances.GetByDateAsync(today);
        var present = items.Count(a => a.Status == AttendanceStatus.Present);
        var absent = items.Count(a => a.Status == AttendanceStatus.Absent);
        var marked = items.Count;
        var late = items.Count(a => a.CheckInTime.HasValue && a.CheckInTime.Value > new TimeSpan(9, 30, 0));

        return Ok(new
        {
            present,
            absent,
            marked,
            late
        });
    }
}
