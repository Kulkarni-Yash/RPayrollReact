using RPayroll.Infrastructure.Repositories.Interfaces;

namespace RPayroll.Infrastructure.UnitOfWork;

public interface IUnitOfWork
{
    IEmployeeRepository Employees { get; }
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    ILeaveRepository Leaves { get; }
    IAttendanceRepository Attendances { get; }
    IPayrollRepository Payrolls { get; }
    IMenuItemRepository MenuItems { get; }
    IFormLayoutRepository FormLayouts { get; }

    Task SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
