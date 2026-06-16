using PropertySaaS.Domain.Common;

namespace PropertySaaS.Domain.Entities;

public class AuditLog : TenantScopedEntity
{
    public string EntityName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
