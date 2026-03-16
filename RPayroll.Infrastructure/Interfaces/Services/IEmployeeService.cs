using RPayroll.Domain.Entities;

namespace RPayroll.Infrastructure.Interfaces.Services;

public interface IEmployeeService
{
    Task<Employee> CreateEmployeeAsync(Employee employee, bool createLogin = false, string? username = null, string? password = null, string? roleName = null);
    Task<Employee?> GetEmployeeByIdAsync(int id);
    Task<IEnumerable<Employee>> GetAllEmployeesAsync();
    Task<IEnumerable<Employee>> GetEmployeesForCurrentUserAsync();
    Task<Employee?> UpdateEmployeeAsync(Employee employee);
    Task<bool> SoftDeleteEmployeeAsync(int id);
    Task<EmployeeContactPerson> AddContactPersonAsync(int employeeId, EmployeeContactPerson contact);
    Task<EmployeeContactPerson?> UpdateContactPersonAsync(int employeeId, EmployeeContactPerson contact);
    Task<bool> RemoveContactPersonAsync(int employeeId, int contactPersonId);
    Task<IEnumerable<Employee>> SearchEmployeesAsync(string query);
    Task<Employee?> GetMyProfileAsync();
    Task<Employee?> UpdateMyProfileAsync(Employee profile);
}
