using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPayroll.Domain.DTOs.Employee;
using RPayroll.Domain.Interfaces.Services;

namespace RPayroll.API.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeeController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
    {
        var employee = await _employeeService.CreateEmployeeAsync(dto);
        return Ok(employee);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllForCurrentUser()
    {
        var employees = await _employeeService.GetEmployeesForCurrentUserAsync();
        return Ok(employees);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var employee = await _employeeService.GetEmployeeByIdAsync(id);
        if (employee == null)
        {
            return NotFound();
        }

        return Ok(employee);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeDto dto)
    {
        dto.Id = id;
        var employee = await _employeeService.UpdateEmployeeAsync(dto);
        if (employee == null)
        {
            return NotFound();
        }

        return Ok(employee);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> SoftDelete(int id)
    {
        var result = await _employeeService.SoftDeleteEmployeeAsync(id);
        return result ? Ok() : NotFound();
    }

    [HttpPost("{id:int}/contacts")]
    public async Task<IActionResult> AddContact(int id, [FromBody] EmployeeContactPersonDto dto)
    {
        var contact = await _employeeService.AddContactPersonAsync(id, dto);
        return Ok(contact);
    }

    [HttpPut("{id:int}/contacts/{contactId:int}")]
    public async Task<IActionResult> UpdateContact(int id, int contactId, [FromBody] EmployeeContactPersonDto dto)
    {
        dto.Id = contactId;
        var contact = await _employeeService.UpdateContactPersonAsync(id, dto);
        if (contact == null)
        {
            return NotFound();
        }
        return Ok(contact);
    }

    [HttpDelete("{id:int}/contacts/{contactId:int}")]
    public async Task<IActionResult> RemoveContact(int id, int contactId)
    {
        var result = await _employeeService.RemoveContactPersonAsync(id, contactId);
        return result ? Ok() : NotFound();
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var results = await _employeeService.SearchEmployeesAsync(query ?? string.Empty);
        return Ok(results);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var profile = await _employeeService.GetMyProfileAsync();
        if (profile == null)
        {
            return NotFound();
        }
        return Ok(profile);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateEmployeeProfileDto dto)
    {
        var profile = await _employeeService.UpdateMyProfileAsync(dto);
        if (profile == null)
        {
            return NotFound();
        }
        return Ok(profile);
    }
}
