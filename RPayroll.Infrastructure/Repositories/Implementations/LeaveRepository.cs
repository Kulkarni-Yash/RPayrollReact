using RPayroll.Domain.Entities;
using RPayroll.Domain.Enums;
using RPayroll.Infrastructure.Repositories.Interfaces;

namespace RPayroll.Infrastructure.Repositories.Implementations;

public class LeaveRepository : ILeaveRepository
{
    public Task<LeaveRequest?> GetByIdAsync(int id, bool includeInactive = false)
    {
        var leave = InMemoryDataStore.LeaveRequests.FirstOrDefault(l =>
            l.Id == id && (includeInactive || l.Status != StatusCode.Rejected));
        return Task.FromResult(leave);
    }

    public Task<List<LeaveRequest>> GetAllAsync(bool includeInactive = false)
    {
        var query = InMemoryDataStore.LeaveRequests.AsEnumerable();
        if (!includeInactive)
        {
            query = query.Where(l => l.Status != StatusCode.Rejected);
        }
        return Task.FromResult(query.ToList());
    }

    public Task<List<LeaveRequest>> GetByEmployeeAsync(int employeeId, bool includeInactive = false)
    {
        var query = InMemoryDataStore.LeaveRequests.Where(l => l.EmployeeId == employeeId);
        if (!includeInactive)
        {
            query = query.Where(l => l.Status != StatusCode.Rejected);
        }
        var leaves = query.ToList();
        return Task.FromResult(leaves);
    }

    public Task AddAsync(LeaveRequest leaveRequest)
    {
        leaveRequest.Id = InMemoryDataStore.NextLeaveId();
        InMemoryDataStore.LeaveRequests.Add(leaveRequest);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(LeaveRequest leaveRequest)
    {
        var index = InMemoryDataStore.LeaveRequests.FindIndex(l => l.Id == leaveRequest.Id);
        if (index >= 0)
        {
            InMemoryDataStore.LeaveRequests[index] = leaveRequest;
        }
        return Task.CompletedTask;
    }
}
