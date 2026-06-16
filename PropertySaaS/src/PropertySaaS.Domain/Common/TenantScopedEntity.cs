namespace PropertySaaS.Domain.Common;

public abstract class TenantScopedEntity : BaseEntity
{
    public Guid OrganizationId { get; set; }
}
