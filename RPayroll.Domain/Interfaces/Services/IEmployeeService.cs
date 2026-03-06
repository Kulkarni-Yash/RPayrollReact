using RPayroll.Domain.DTOs.Employee;

namespace RPayroll.Domain.Interfaces.Services;

public interface IEmployeeService
{
    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto dto);
    Task<EmployeeDto?> GetEmployeeByIdAsync(int id);
    Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync();
    Task<IEnumerable<EmployeeDto>> GetEmployeesForCurrentUserAsync();
    Task<EmployeeDto?> UpdateEmployeeAsync(UpdateEmployeeDto dto);
    Task<bool> SoftDeleteEmployeeAsync(int id);
    Task<EmployeeContactPersonDto> AddContactPersonAsync(int employeeId, EmployeeContactPersonDto dto);
    Task<EmployeeContactPersonDto?> UpdateContactPersonAsync(int employeeId, EmployeeContactPersonDto dto);
    Task<bool> RemoveContactPersonAsync(int employeeId, int contactPersonId);
    Task<IEnumerable<EmployeeSearchResultDto>> SearchEmployeesAsync(string query);
    Task<EmployeeDto?> GetMyProfileAsync();
    Task<EmployeeDto?> UpdateMyProfileAsync(UpdateEmployeeProfileDto dto);
}
