using PropertySaaS.Domain.Common;

namespace PropertySaaS.Domain.Entities;

public class Unit : TenantScopedEntity
{
    public Guid PropertyId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public decimal MonthlyRent { get; set; }
    public bool IsOccupied { get; set; }
    public Property? Property { get; set; }
    public ICollection<Lease> Leases { get; set; } = new List<Lease>();
}
