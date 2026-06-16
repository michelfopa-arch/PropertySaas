using PropertySaaS.Domain.Common;

namespace PropertySaaS.Domain.Entities;

public class DocumentTemplate : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Province { get; set; } = "ON";
    public string Description { get; set; } = string.Empty;
}
