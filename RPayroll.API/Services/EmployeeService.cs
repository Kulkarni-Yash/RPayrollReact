using RPayroll.Domain.DTOs.Employee;
using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Domain.Interfaces.Services;
using RPayroll.Infrastructure.UnitOfWork;

namespace RPayroll.API.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public EmployeeService(IUnitOfWork unitOfWork, ICurrentUserContext currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto dto)
    {
        EnsureAuthenticated();
        EnsureCanManageEmployees(dto.ManagerId);
        ValidateBankDetails(dto.BankName, dto.BankAccountNumber, dto.IFSCCode);
        ValidateLeaveAllocations(dto.TotalSickLeaves, dto.TotalCasualLeaves, dto.TotalGovernmentLeaves, dto.TotalUnpaidLeaves);
        ValidateContacts(dto.ContactPersons);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var employee = new Employee
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = dto.Phone,
                DateOfBirth = dto.DateOfBirth,
                Department = dto.Department,
                Position = dto.Position,
                BasicSalary = dto.BasicSalary,
                ManagerId = dto.ManagerId,
                DateOfJoining = dto.DateOfJoining,
                Address = dto.Address,
                BankName = dto.BankName,
                BankAccountNumber = dto.BankAccountNumber,
                IFSCCode = dto.IFSCCode,
                TotalSickLeaves = dto.TotalSickLeaves,
                TotalCasualLeaves = dto.TotalCasualLeaves,
                TotalGovernmentLeaves = dto.TotalGovernmentLeaves,
                TotalUnpaidLeaves = dto.TotalUnpaidLeaves,
                UsedSickLeaves = 0,
                UsedCasualLeaves = 0,
                UsedGovernmentLeaves = 0,
                UsedUnpaidLeaves = 0,
                CreatedDate = DateTime.UtcNow,
                Status = StatusCode.Accepted
            };

            if (dto.ContactPersons.Count > 0)
            {
                foreach (var contact in dto.ContactPersons)
                {
                    var contactEntity = new EmployeeContactPerson
                    {
                        Name = contact.Name,
                        Relationship = contact.Relationship,
                        Phone = contact.Phone,
                        Email = contact.Email,
                        IsPrimary = contact.IsPrimary,
                        CreatedDate = DateTime.UtcNow,
                        Status = StatusCode.Accepted
                    };
                    employee.ContactPersons.Add(contactEntity);
                }
                EnforceSinglePrimary(employee);
            }

            await _unitOfWork.Employees.AddAsync(employee);

            if (dto.CreateLogin && !string.IsNullOrWhiteSpace(dto.Username))
            {
                var roleName = string.IsNullOrWhiteSpace(dto.RoleName) ? "Employee" : dto.RoleName;
                var role = await _unitOfWork.Roles.GetByNameAsync(roleName, includeInactive: true)
                           ?? new Role
                           {
                               Name = roleName,
                               HierarchyLevel = 4,
                               CreatedDate = DateTime.UtcNow,
                               Status = StatusCode.Accepted
                           };

                var user = new User
                {
                    Username = dto.Username,
                    Password = dto.Password ?? string.Empty,
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

            return MapEmployee(employee);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
    {
        EnsureAuthenticated();
        var allowed = await GetEmployeesForCurrentUserAsync();
        return allowed.FirstOrDefault(e => e.Id == id);
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync()
    {
        EnsureAuthenticated();
        var employees = await _unitOfWork.Employees.GetAllAsync(includeInactive: true);
        return employees.Select(MapEmployee).ToList();
    }

    public async Task<IEnumerable<EmployeeDto>> GetEmployeesForCurrentUserAsync()
    {
        EnsureAuthenticated();
        var employees = await _unitOfWork.Employees.GetAllAsync(includeInactive: false);
        var filtered = ApplyVisibilityFilter(employees);
        return filtered.Select(MapEmployee).ToList();
    }

    public async Task<EmployeeDto?> UpdateEmployeeAsync(UpdateEmployeeDto dto)
    {
        EnsureAuthenticated();
        ValidateBankDetails(dto.BankName, dto.BankAccountNumber, dto.IFSCCode);
        ValidateLeaveAllocations(dto.TotalSickLeaves, dto.TotalCasualLeaves, dto.TotalGovernmentLeaves, dto.TotalUnpaidLeaves);

        var employee = await _unitOfWork.Employees.GetByIdAsync(dto.Id, includeInactive: true);
        if (employee == null)
        {
            return null;
        }

        EnsureCanModifyEmployee(employee);

        employee.FirstName = dto.FirstName;
        employee.LastName = dto.LastName;
        employee.Email = dto.Email;
        employee.Phone = dto.Phone;
        employee.DateOfBirth = dto.DateOfBirth;
        employee.Department = dto.Department;
        employee.Position = dto.Position;
        employee.BasicSalary = dto.BasicSalary;
        employee.ManagerId = dto.ManagerId;
        employee.DateOfJoining = dto.DateOfJoining;
        employee.Address = dto.Address;
        employee.BankName = dto.BankName;
        employee.BankAccountNumber = dto.BankAccountNumber;
        employee.IFSCCode = dto.IFSCCode;
        employee.TotalSickLeaves = dto.TotalSickLeaves;
        employee.TotalCasualLeaves = dto.TotalCasualLeaves;
        employee.TotalGovernmentLeaves = dto.TotalGovernmentLeaves;
        employee.TotalUnpaidLeaves = dto.TotalUnpaidLeaves;
        employee.UpdatedDate = DateTime.UtcNow;
        employee.Status = employee.Status == StatusCode.Rejected ? StatusCode.Accepted : employee.Status;

        await _unitOfWork.Employees.UpdateAsync(employee);
        await _unitOfWork.SaveChangesAsync();

        return MapEmployee(employee);
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

    public async Task<EmployeeContactPersonDto> AddContactPersonAsync(int employeeId, EmployeeContactPersonDto dto)
    {
        EnsureAuthenticated();
        ValidateContact(dto);
        var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, includeInactive: true);
        if (employee == null)
        {
            throw new InvalidOperationException("Employee not found.");
        }

        EnsureCanModifyEmployee(employee);

        var contact = new EmployeeContactPerson
        {
            EmployeeId = employeeId,
            Name = dto.Name,
            Relationship = dto.Relationship,
            Phone = dto.Phone,
            Email = dto.Email,
            IsPrimary = dto.IsPrimary,
            CreatedDate = DateTime.UtcNow,
            Status = StatusCode.Accepted
        };

        employee.ContactPersons.Add(contact);
        if (dto.IsPrimary)
        {
            EnsureSinglePrimary(employee, contact.Id);
        }

        await _unitOfWork.Employees.UpdateAsync(employee);
        await _unitOfWork.SaveChangesAsync();

        return MapContact(employeeId, contact);
    }

    public async Task<EmployeeContactPersonDto?> UpdateContactPersonAsync(int employeeId, EmployeeContactPersonDto dto)
    {
        EnsureAuthenticated();
        ValidateContact(dto);
        var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId, includeInactive: true);
        if (employee == null)
        {
            return null;
        }

        EnsureCanModifyEmployee(employee);

        var contact = employee.ContactPersons.FirstOrDefault(c => c.Id == dto.Id);
        if (contact == null)
        {
            return null;
        }

        contact.Name = dto.Name;
        contact.Relationship = dto.Relationship;
        contact.Phone = dto.Phone;
        contact.Email = dto.Email;
        contact.IsPrimary = dto.IsPrimary;
        contact.UpdatedDate = DateTime.UtcNow;

        if (dto.IsPrimary)
        {
            EnsureSinglePrimary(employee, contact.Id);
        }

        await _unitOfWork.Employees.UpdateAsync(employee);
        await _unitOfWork.SaveChangesAsync();

        return MapContact(employeeId, contact);
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

    public async Task<IEnumerable<EmployeeSearchResultDto>> SearchEmployeesAsync(string query)
    {
        var employees = await GetEmployeesForCurrentUserAsync();
        var filtered = employees.Where(e =>
            string.IsNullOrWhiteSpace(query) ||
            ($"{e.FirstName} {e.LastName}").Contains(query, StringComparison.OrdinalIgnoreCase));

        return filtered.Select(e => new EmployeeSearchResultDto
        {
            Id = e.Id,
            FullName = $"{e.FirstName} {e.LastName}".Trim()
        }).ToList();
    }

    public async Task<EmployeeDto?> GetMyProfileAsync()
    {
        EnsureAuthenticated();
        if (!_currentUser.EmployeeId.HasValue)
        {
            return null;
        }

        var employee = await _unitOfWork.Employees.GetByIdAsync(_currentUser.EmployeeId.Value, includeInactive: true);
        return employee == null ? null : MapEmployee(employee);
    }

    public async Task<EmployeeDto?> UpdateMyProfileAsync(UpdateEmployeeProfileDto dto)
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

        if (employee.Id != _currentUser.EmployeeId.Value)
        {
            throw new UnauthorizedAccessException("Cannot edit other employee profile.");
        }

        ValidateBankDetails(dto.BankName, dto.BankAccountNumber, dto.IFSCCode);
        employee.Email = dto.Email;
        employee.Phone = dto.Phone;
        employee.Address = dto.Address;
        employee.BankName = dto.BankName;
        employee.BankAccountNumber = dto.BankAccountNumber;
        employee.IFSCCode = dto.IFSCCode;
        employee.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.Employees.UpdateAsync(employee);
        await _unitOfWork.SaveChangesAsync();

        return MapEmployee(employee);
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

    private bool IsEmployeeAdmin(Employee employee)
    {
        return string.Equals(employee.User?.Role?.Name, "Admin", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsAdmin() => string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase);

    private bool IsHr() => string.Equals(_currentUser.Role, "HR", StringComparison.OrdinalIgnoreCase);

    private bool IsManager() => string.Equals(_currentUser.Role, "Manager", StringComparison.OrdinalIgnoreCase);

    private bool IsAdminOrHr() => IsAdmin() || IsHr();

    private bool IsManagerOf(Employee employee)
    {
        return _currentUser.EmployeeId.HasValue && employee.ManagerId == _currentUser.EmployeeId.Value;
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }
    }

    private void EnsureCanModifyEmployee(Employee employee)
    {
        if (IsAdminOrHr())
        {
            return;
        }

        if (IsManagerOf(employee))
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

    private static EmployeeDto MapEmployee(Employee employee)
    {
        return new EmployeeDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Phone = employee.Phone,
            DateOfBirth = employee.DateOfBirth,
            Department = employee.Department,
            Position = employee.Position,
            BasicSalary = employee.BasicSalary,
            ManagerId = employee.ManagerId,
            DateOfJoining = employee.DateOfJoining,
            Address = employee.Address,
            BankName = employee.BankName,
            BankAccountNumber = employee.BankAccountNumber,
            IFSCCode = employee.IFSCCode,
            TotalSickLeaves = employee.TotalSickLeaves,
            UsedSickLeaves = employee.UsedSickLeaves,
            TotalCasualLeaves = employee.TotalCasualLeaves,
            UsedCasualLeaves = employee.UsedCasualLeaves,
            TotalGovernmentLeaves = employee.TotalGovernmentLeaves,
            UsedGovernmentLeaves = employee.UsedGovernmentLeaves,
            TotalUnpaidLeaves = employee.TotalUnpaidLeaves,
            UsedUnpaidLeaves = employee.UsedUnpaidLeaves,
            ContactPersons = employee.ContactPersons
                .Where(c => c.Status != StatusCode.Rejected)
                .Select(c => new EmployeeContactPersonDto
                {
                    Id = c.Id,
                    EmployeeId = employee.Id,
                    Name = c.Name,
                    Relationship = c.Relationship,
                    Phone = c.Phone,
                    Email = c.Email,
                    IsPrimary = c.IsPrimary
                }).ToList()
        };
    }

    private static EmployeeContactPersonDto MapContact(int employeeId, EmployeeContactPerson contact)
    {
        return new EmployeeContactPersonDto
        {
            Id = contact.Id,
            EmployeeId = employeeId,
            Name = contact.Name,
            Relationship = contact.Relationship,
            Phone = contact.Phone,
            Email = contact.Email,
            IsPrimary = contact.IsPrimary
        };
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
            if (contact != primary)
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

        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            throw new InvalidOperationException("Bank account number is required.");
        }

        var ifscValue = ifsc.Trim().ToUpperInvariant();
        var ifscPattern = @"^[A-Z]{4}0[A-Z0-9]{6}$";
        if (!System.Text.RegularExpressions.Regex.IsMatch(ifscValue, ifscPattern))
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

    private static void ValidateContacts(IEnumerable<EmployeeContactPersonDto> contacts)
    {
        foreach (var contact in contacts)
        {
            ValidateContact(contact);
        }
    }

    private static void ValidateContact(EmployeeContactPersonDto contact)
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
