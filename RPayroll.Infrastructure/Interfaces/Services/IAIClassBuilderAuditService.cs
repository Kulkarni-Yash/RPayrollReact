using RPayroll.Infrastructure.Interfaces.Services.Models;

namespace RPayroll.Infrastructure.Interfaces.Services;

public interface IAIClassBuilderAuditService
{
    Task<AIClassBuilderAuditLogContract> GetLogAsync(CancellationToken cancellationToken = default);
    Task<AIClassBuilderAuditEntryContract> AppendAsync(AIClassBuilderAuditRequestContract request, CancellationToken cancellationToken = default);
}
