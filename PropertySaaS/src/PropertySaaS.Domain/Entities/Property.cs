using PropertySaaS.Domain.Common;

namespace PropertySaaS.Domain.Entities;

public class Property : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = "ON";
    public string PostalCode { get; set; } = string.Empty;
    public int YearBuilt { get; set; }
    public decimal MonthlyRevenueTarget { get; set; }
    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}
