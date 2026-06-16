using PropertySaaS.Domain.Common;
using PropertySaaS.Domain.Enums;

namespace PropertySaaS.Domain.Entities;

public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Province { get; set; } = "ON";
    public string TimeZone { get; set; } = "America/Toronto";
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Trial;
    public string StripeCustomerId { get; set; } = string.Empty;
    public string StripeSubscriptionId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<Property> Properties { get; set; } = new List<Property>();
}
