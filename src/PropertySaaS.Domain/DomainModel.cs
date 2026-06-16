using PropertySaaS.Domain.Enums;

namespace PropertySaaS.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedUtc { get; set; }
    }

    public abstract class TenantScopedEntity : BaseEntity
    {
        public Guid OrganizationId { get; set; }
    }
}

namespace PropertySaaS.Domain.Enums
{
    public enum SubscriptionTier { Trial, Starter, Growth, Pro }
    public enum LeaseStatus { Draft, Active, EndingSoon, Expired, Terminated }
    public enum MaintenancePriority { Low, Medium, High, Emergency }
}

namespace PropertySaaS.Domain.Entities
{
    using PropertySaaS.Domain.Common;
    using PropertySaaS.Domain.Enums;

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

    public class AppUser : TenantScopedEntity
    {
        public string ClerkUserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Owner";
        public bool IsActive { get; set; } = true;
        public Organization? Organization { get; set; }
    }

    public class Property : TenantScopedEntity
    {
        public string Name { get; set; } = string.Empty;
        public string PropertyType { get; set; } = "Multi-family";
        public string AddressLine1 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = "ON";
        public string PostalCode { get; set; } = string.Empty;
        public int YearBuilt { get; set; }
        public decimal MonthlyRevenueTarget { get; set; }
        public string AmenitySummary { get; set; } = string.Empty;
        public string NeighborhoodNotes { get; set; } = string.Empty;
        public string LeasingNotes { get; set; } = string.Empty;
        public string OperationalNotes { get; set; } = string.Empty;
        public ICollection<Unit> Units { get; set; } = new List<Unit>();
    }

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

    public class AuditLog : TenantScopedEntity
    {
        public string EntityName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    public class ComplianceReminder : TenantScopedEntity
    {
        public string Title { get; set; } = string.Empty;
        public string NoticeType { get; set; } = string.Empty;
        public DateOnly DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public string Reference { get; set; } = string.Empty;
    }

    public class DocumentTemplate : TenantScopedEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Province { get; set; } = "ON";
        public string Description { get; set; } = string.Empty;
    }
}
