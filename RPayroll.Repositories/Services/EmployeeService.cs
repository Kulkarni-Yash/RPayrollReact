using System.Text.RegularExpressions;
using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.Interfaces.Common;
using RPayroll.Infrastructure.Interfaces.Services;
using RPayroll.Infrastructure.UnitOfWork;

namespace RPayroll.Repositories.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public EmployeeService(IUnitOfWork unitOfWork, ICurrentUserContext currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Employee> CreateEmployeeAsync(Employee employee, bool createLogin = false, string? username = null, string? password = null, string? roleName = null)
    {
        EnsureAuthenticated();
        EnsureCanManageEmployees(employee.ManagerId);
        ValidateBankDetails(employee.BankName, employee.BankAccountNumber, employee.IFSCCode);
        ValidateLeaveAllocations(employee.TotalSickLeaves, employee.TotalCasualLeaves, employee.TotalGovernmentLeaves, employee.TotalUnpaidLeaves);
        ValidateContacts(employee.ContactPersons);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            employee.CreatedDate = DateTime.UtcNow;
            employee.Status = StatusCode.Accepted;
            employee.UsedSickLeaves = 0;
            employee.UsedCasualLeaves = 0;
            employee.UsedGovernmentLeaves = 0;
            employee.UsedUnpaidLeaves = 0;

            foreach (var contact in employee.ContactPersons)
            {
                contact.CreatedDate = DateTime.UtcNow;
                contact.Status = StatusCode.Accepted;
            }
            EnforceSinglePrimary(employee);

            await _unitOfWork.Employees.AddAsync(employee);

            if (createLogin && !string.IsNullOrWhiteSpace(username))
            {
                var resolvedRoleName = string.IsNullOrWhiteSpace(roleName) ? "Employee" : roleName;
                var role = await _unitOfWork.Roles.GetByNameAsync(resolvedRoleName, includeInactive: true)
                    ?? new Role
                    {
                        Name = resolvedRoleName,
                        HierarchyLevel = 4,
                        CreatedDate = DateTime.UtcNow,
                        Status = StatusCode.Accepted
                    };

                var user = new User
                {
                    Username = username,
                    Password = password ?? string.Empty,
                    Role = role,
                    RoleId = role.Id,
                    EmployeeId = employee.Id,
                    Employee = employee,
                    CreatedDate = DateTime.UtcNow,
                    Status = StatusCode.Accepted
                };

                await _unitOfWork.Users.AddAsync(user);
                employee.UserId = user.Id;
                employee.User = user;
                await _unitOfWork.Employees.UpdateAsync(employee);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
            return employee;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<Employee?> GetEmployeeByIdAsync(int id)
    {
        EnsureAuthenticated();
        var allowed = await GetEmployeesForCurrentUserAsync();
        return allowed.FirstOrDefault(e => e.Id == id);
    }

    public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
    {
        EnsureAuthenticated();
        return await _unitOfWork.Employees.GetAllAsync(includeInactive: true);
    }

    public async Task<IEnumerable<Employee>> GetEmployeesForCurrentUserAsync()
    {
        EnsureAuthenticated();
        var employees = await _unitOfWork.Employees.GetAllAsync(includeInactive: false);
        return ApplyVisibilityFilter(employees).ToList();
    }

    public async Task<Employee?> UpdateEmployeeAsync(Employee employee)
    {
        EnsureAuthenticated();
        ValidateBankDetails(employee.BankName, employee.BankAccountNumber, employee.IFSCCode);
        ValidateLeaveAllocations(employee.TotalSickLeaves, employee.TotalCasualLeaves, employee.TotalGovernmentLeaves, employee.TotalUnpaidLeaves);

        var existingEmployee = await _unitOfWork.Employees.GetByIdAsync(employee.Id, includeInactive: true);
        if (existingEmployee == null)
        {
            return null;
        }

        EnsureCanModifyEmployee(existingEmployee);

        existingEmployee.FirstName = employee.FirstName;
        existingEmployee.LastName = employee.LastName;
        existingEmployee.Email = employee.Email;
        existingEmployee.Phone = employee.Phone;
        existingEmployee.DateOfBirth = employee.DateOfBirth;
        existingEmployee.Department = employee.Department;
        existingEmployee.Position = employee.Position;
        existingEmployee.BasicSalary = employee.BasicSalary;
        existingEmployee.ManagerId = employee.ManagerId;
        existingEmployee.DateOfJoining = employee.DateOfJoining;
        existingEmployee.Address = employee.Address;
        existingEmployee.BankName = employee.BankName;
        existingEmployee.BankAccountNumber = employee.BankAccountNumber;
        existingEmployee.IFSCCode = employee.IFSCCode;
        existingEmployee.TotalSickLeaves = employee.TotalSickLeaves;
        existingEmployee.TotalCasualLeaves = employee.TotalCasualLeaves;
        existingEmployee.TotalGovernmentLeaves = employee.TotalGovernmentLeaves;
        existingEmployee.TotalUnpaidLeaves = employee.TotalUnpaidLeaves;
        existingEmployee.UpdatedDate = DateTime.UtcNow;
        existingEmployee.Status = existingEmployee.Status == StatusCode.Rejected ? StatusCode.Accepted : existingEmployee.Status;

        await _unitOfWork.Employees.UpdateAsync(existingEmployee);
        await _unitOfWork.SaveChangesAsync();
        return existingEmployee;
    }

    public async Task<bool> SoftDeleteEmployeeAsync(int id)
    {
        EnsureAuthenticated();
        var employee = await _unitOfWork.Employees.GetByIdAsync(id, includeInactive: true);
        if (employee == null)
        {
            return false;
        }

        if (!IsAdminOrHr() && !IsManagerOf(employee))
        {
            throw new UnauthorizedAccessException("Not allowed to delete employee.");
        }

        employee.Status = StatusCode.Rejected;
        employee.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.Employees.UpdateAsync(employee);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<EmployeeContactPerson> AddContactPersonAsync(int employeeId, EmployeeContactPerson contact)
    {
        EnsureAuthenticated();
        ValidateContact(contact);
        var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, includeInactive: true)
            ?? throw new InvalidOperationException("Employee not found.");

        EnsureCanModifyEmployee(employee);

        contact.EmployeeId = employeeId;
        contact.CreatedDate = DateTime.UtcNow;
        contact.Status = StatusCode.Accepted;
        employee.ContactPersons.Add(contact);
        if (contact.IsPrimary)
        {
            EnsureSinglePrimary(employee, contact.Id);
        }

        await _unitOfWork.Employees.UpdateAsync(employee);
        await _unitOfWork.SaveChangesAsync();
        return contact;
    }

    public async Task<EmployeeContactPerson?> UpdateContactPersonAsync(int employeeId, EmployeeContactPerson contact)
    {
        EnsureAuthenticated();
        ValidateContact(contact);
        var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, includeInactive: true);
        if (employee == null)
        {
            return null;
        }

        EnsureCanModifyEmployee(employee);

        var existingContact = employee.ContactPersons.FirstOrDefault(c => c.Id == contact.Id);
        if (existingContact == null)
        {
            return null;
        }

        existingContact.Name = contact.Name;
        existingContact.Relationship = contact.Relationship;
        existingContact.Phone = contact.Phone;
        existingContact.Email = contact.Email;
        existingContact.IsPrimary = contact.IsPrimary;
        existingContact.UpdatedDate = DateTime.UtcNow;

        if (contact.IsPrimary)
        {
            EnsureSinglePrimary(employee, contact.Id);
        }

        await _unitOfWork.Employees.UpdateAsync(employee);
        await _unitOfWork.SaveChangesAsync();
        return existingContact;
    }

    public async Task<bool> RemoveContactPersonAsync(int employeeId, int contactPersonId)
    {
        EnsureAuthenticated();
        var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, includeInactive: true);
        if (employee == null)
        {
            return false;
        }

        EnsureCanModifyEmployee(employee);

        var contact = employee.ContactPersons.FirstOrDefault(c => c.Id == contactPersonId);
        if (contact == null)
        {
            return false;
        }

        contact.Status = StatusCode.Rejected;
        contact.IsPrimary = false;
        contact.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.Employees.UpdateAsync(employee);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Employee>> SearchEmployeesAsync(string query)
    {
        var employees = await GetEmployeesForCurrentUserAsync();
        return employees.Where(e =>
            string.IsNullOrWhiteSpace(query) ||
            ($"{e.FirstName} {e.LastName}").Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<Employee?> GetMyProfileAsync()
    {
        EnsureAuthenticated();
        if (!_currentUser.EmployeeId.HasValue)
        {
            return null;
        }

        return await _unitOfWork.Employees.GetByIdAsync(_currentUser.EmployeeId.Value, includeInactive: true);
    }

    public async Task<Employee?> UpdateMyProfileAsync(Employee profile)
    {
        EnsureAuthenticated();
        if (!_currentUser.EmployeeId.HasValue)
        {
            throw new UnauthorizedAccessException("Only employees can update profile.");
        }

        var employee = await _unitOfWork.Employees.GetByIdAsync(_currentUser.EmployeeId.Value, includeInactive: true);
        if (employee == null)
        {
            return null;
        }

        ValidateBankDetails(profile.BankName, profile.BankAccountNumber, profile.IFSCCode);
        employee.Email = profile.Email;
        employee.Phone = profile.Phone;
        employee.Address = profile.Address;
        employee.BankName = profile.BankName;
        employee.BankAccountNumber = profile.BankAccountNumber;
        employee.IFSCCode = profile.IFSCCode;
        employee.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.Employees.UpdateAsync(employee);
        await _unitOfWork.SaveChangesAsync();
        return employee;
    }

    private IEnumerable<Employee> ApplyVisibilityFilter(IEnumerable<Employee> employees)
    {
        if (IsAdmin())
        {
            return employees;
        }

        if (IsHr())
        {
            return employees.Where(e => !IsEmployeeAdmin(e));
        }

        if (IsManager())
        {
            var managerEmployeeId = _currentUser.EmployeeId ?? 0;
            return employees.Where(e => e.ManagerId == managerEmployeeId);
        }

        if (_currentUser.EmployeeId.HasValue)
        {
            return employees.Where(e => e.Id == _currentUser.EmployeeId.Value);
        }

        return Enumerable.Empty<Employee>();
    }

    private static bool IsEmployeeAdmin(Employee employee) =>
        string.Equals(employee.User?.Role?.Name, "Admin", StringComparison.OrdinalIgnoreCase);

    private bool IsAdmin() => string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase);
    private bool IsHr() => string.Equals(_currentUser.Role, "HR", StringComparison.OrdinalIgnoreCase);
    private bool IsManager() => string.Equals(_currentUser.Role, "Manager", StringComparison.OrdinalIgnoreCase);
    private bool IsAdminOrHr() => IsAdmin() || IsHr();
    private bool IsManagerOf(Employee employee) => _currentUser.EmployeeId.HasValue && employee.ManagerId == _currentUser.EmployeeId.Value;

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }
    }

    private void EnsureCanModifyEmployee(Employee employee)
    {
        if (IsAdminOrHr() || IsManagerOf(employee))
        {
            return;
        }

        if (_currentUser.EmployeeId.HasValue && employee.Id == _currentUser.EmployeeId.Value)
        {
            return;
        }

        throw new UnauthorizedAccessException("Not allowed to modify employee.");
    }

    private void EnsureCanManageEmployees(int? managerId)
    {
        if (IsAdminOrHr())
        {
            return;
        }

        if (IsManager())
        {
            if (managerId.HasValue && _currentUser.EmployeeId.HasValue && managerId.Value != _currentUser.EmployeeId.Value)
            {
                throw new UnauthorizedAccessException("Managers can only create subordinates.");
            }
            return;
        }

        throw new UnauthorizedAccessException("Not allowed to create employee.");
    }

    private static void EnforceSinglePrimary(Employee employee)
    {
        var primary = employee.ContactPersons.FirstOrDefault(c => c.IsPrimary);
        if (primary == null)
        {
            return;
        }

        foreach (var contact in employee.ContactPersons)
        {
            if (!ReferenceEquals(contact, primary))
            {
                contact.IsPrimary = false;
            }
        }
    }

    private static void EnsureSinglePrimary(Employee employee, int primaryContactId)
    {
        foreach (var contact in employee.ContactPersons)
        {
            if (contact.Id != primaryContactId)
            {
                contact.IsPrimary = false;
            }
        }
    }

    private static void ValidateBankDetails(string? bankName, string? accountNumber, string? ifsc)
    {
        if (string.IsNullOrWhiteSpace(bankName) && string.IsNullOrWhiteSpace(accountNumber) && string.IsNullOrWhiteSpace(ifsc))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(bankName) || string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(ifsc))
        {
            throw new InvalidOperationException("Bank details are required.");
        }

        var ifscValue = ifsc.Trim().ToUpperInvariant();
        if (!Regex.IsMatch(ifscValue, @"^[A-Z]{4}0[A-Z0-9]{6}$"))
        {
            throw new InvalidOperationException("IFSC code format is invalid.");
        }
    }

    private static void ValidateLeaveAllocations(int sick, int casual, int government, int unpaid)
    {
        if (sick < 0 || casual < 0 || government < 0 || unpaid < 0)
        {
            throw new InvalidOperationException("Leave allocations must be positive.");
        }
    }

    private static void ValidateContacts(IEnumerable<EmployeeContactPerson> contacts)
    {
        foreach (var contact in contacts)
        {
            ValidateContact(contact);
        }
    }

    private static void ValidateContact(EmployeeContactPerson contact)
    {
        if (string.IsNullOrWhiteSpace(contact.Name))
        {
            throw new InvalidOperationException("Contact name is required.");
        }

        if (string.IsNullOrWhiteSpace(contact.Phone))
        {
            throw new InvalidOperationException("Contact phone is required.");
        }
    }
}
