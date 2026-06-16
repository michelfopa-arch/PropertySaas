using PropertySaaS.Domain.Common;

namespace PropertySaaS.Domain.Entities;

public class AppUser : TenantScopedEntity
{
    public string ClerkUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Owner";
    public bool IsActive { get; set; } = true;
    public Organization? Organization { get; set; }
}
