using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RPayroll.API.DTOs.Employee;
using RPayroll.API.Mappers;
using RPayroll.Infrastructure.Interfaces.Services;

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
    public async Task<IActionResult> Create([FromBody] EmployeeDto dto)
    {
        var employee = await _employeeService.CreateEmployeeAsync(
            dto.ToEntity(),
            dto.CreateLogin,
            dto.Username,
            dto.Password,
            dto.RoleName);
        return Ok(employee.ToDto());
    }

    [HttpGet]
    public async Task<IActionResult> GetAllForCurrentUser()
    {
        var employees = await _employeeService.GetEmployeesForCurrentUserAsync();
        return Ok(employees.Select(x => x.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var employee = await _employeeService.GetEmployeeByIdAsync(id);
        return employee == null ? NotFound() : Ok(employee.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeDto dto)
    {
        dto.Id = id;
        var employee = await _employeeService.UpdateEmployeeAsync(dto.ToEntity());
        return employee == null ? NotFound() : Ok(employee.ToDto());
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
        var contact = await _employeeService.AddContactPersonAsync(id, dto.ToEntity());
        return Ok(contact.ToDto(id));
    }

    [HttpPut("{id:int}/contacts/{contactId:int}")]
    public async Task<IActionResult> UpdateContact(int id, int contactId, [FromBody] EmployeeContactPersonDto dto)
    {
        dto.Id = contactId;
        var contact = await _employeeService.UpdateContactPersonAsync(id, dto.ToEntity());
        return contact == null ? NotFound() : Ok(contact.ToDto(id));
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
        return Ok(results.Select(x => x.ToSearchDto()));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var profile = await _employeeService.GetMyProfileAsync();
        return profile == null ? NotFound() : Ok(profile.ToDto());
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] EmployeeDto dto)
    {
        var profile = await _employeeService.UpdateMyProfileAsync(dto.ToEntity());
        return profile == null ? NotFound() : Ok(profile.ToDto());
    }
}
