using PropertySaaS.Domain.Common;
using PropertySaaS.Domain.Enums;

namespace PropertySaaS.Domain.Entities;

public class MaintenanceRequest : TenantScopedEntity
{
    public Guid PropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MaintenancePriority Priority { get; set; }
    public string Status { get; set; } = "Open";
    public string VendorName { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public DateOnly RequestedDate { get; set; }
}
