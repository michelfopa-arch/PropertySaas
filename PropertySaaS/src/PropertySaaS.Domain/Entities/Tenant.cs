using PropertySaaS.Domain.Common;

namespace PropertySaaS.Domain.Entities;

public class Tenant : TenantScopedEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int CreditScore { get; set; }
    public bool ScreeningCompleted { get; set; }
    public string ScreeningProvider { get; set; } = string.Empty;
    public ICollection<Lease> Leases { get; set; } = new List<Lease>();
}
