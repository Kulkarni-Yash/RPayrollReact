using RPayroll.Infrastructure.Repositories.Interfaces;

namespace RPayroll.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    public UnitOfWork(
        IEmployeeRepository employees,
        IUserRepository users,
        IRoleRepository roles,
        ILeaveRepository leaves,
        IAttendanceRepository attendances,
        IPayrollRepository payrolls,
        IMenuItemRepository menuItems,
        IFormLayoutRepository formLayouts)
    {
        Employees = employees;
        Users = users;
        Roles = roles;
        Leaves = leaves;
        Attendances = attendances;
        Payrolls = payrolls;
        MenuItems = menuItems;
        FormLayouts = formLayouts;
    }

    public IEmployeeRepository Employees { get; }
    public IUserRepository Users { get; }
    public IRoleRepository Roles { get; }
    public ILeaveRepository Leaves { get; }
    public IAttendanceRepository Attendances { get; }
    public IPayrollRepository Payrolls { get; }
    public IMenuItemRepository MenuItems { get; }
    public IFormLayoutRepository FormLayouts { get; }

    public Task SaveChangesAsync()
    {
        return Task.CompletedTask;
    }

    public Task BeginTransactionAsync()
    {
        return Task.CompletedTask;
    }

    public Task CommitAsync()
    {
        return Task.CompletedTask;
    }

    public Task RollbackAsync()
    {
        return Task.CompletedTask;
    }
}
