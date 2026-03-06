using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPayroll.API.Models;
using RPayroll.Domain.Interfaces.Services;

namespace RPayroll.API.Controllers;

[ApiController]
[Route("api/payroll")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _payrollService;

    public PayrollController(IPayrollService payrollService)
    {
        _payrollService = payrollService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GeneratePayrollRequest request)
    {
        var payroll = await _payrollService.GeneratePayrollAsync(
            request.EmployeeId,
            request.PeriodStart,
            request.PeriodEnd);
        return Ok(payroll);
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        var payrolls = await _payrollService.GetPayrollsByEmployeeAsync(employeeId);
        return Ok(payrolls);
    }

    [HttpGet]
    public async Task<IActionResult> GetForCurrentUser()
    {
        var payrolls = await _payrollService.GetPayrollsForCurrentUserAsync();
        return Ok(payrolls);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _payrollService.DeletePayrollAsync(id);
        return result ? Ok() : NotFound();
    }
}
