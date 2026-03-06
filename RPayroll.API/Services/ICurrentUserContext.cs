namespace RPayroll.API.Services;

public interface ICurrentUserContext
{
    int UserId { get; }
    string Role { get; }
    int HierarchyLevel { get; }
    int? EmployeeId { get; }
    bool IsAuthenticated { get; }
}
