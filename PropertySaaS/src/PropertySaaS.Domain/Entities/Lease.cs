using PropertySaaS.Domain.Common;
using PropertySaaS.Domain.Enums;

namespace PropertySaaS.Domain.Entities;

public class Lease : TenantScopedEntity
{
    public Guid UnitId { get; set; }
    public Guid TenantId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public LeaseStatus Status { get; set; }
    public bool StandardOntarioLeaseSigned { get; set; }
    public bool N1IncreaseNoticeScheduled { get; set; }
    public Unit? Unit { get; set; }
    public Tenant? Tenant { get; set; }
}
