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
    public enum ListingStatus { Draft, ReadyToPublish, Published, Archived }
    public enum LeadStatus { New, Contacted, Qualified, ShowingScheduled, Applied, Won, Lost }
    public enum PaymentStatus { Draft, Sent, Paid, PartiallyPaid, Overdue, Cancelled }
    public enum MediaAssetCategory { PropertyPhoto, UnitPhoto, MaintenanceEvidence, LeaseDocument, Notice, ListingAttachment, MaintenanceBeforePhoto, MaintenanceAfterPhoto, MaintenanceEvidenceDocument }
    public enum AISuggestionType { WorkQueue, ListingDescription, TenantMessage, ImportMapping, ComplianceRecommendation }
    public enum ConversationChannel { Email, SMS, Portal, Phone }
}

namespace PropertySaaS.Domain.Entities
{
    using PropertySaaS.Domain.Common;
    using PropertySaaS.Domain.Enums;

    public class Organization : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string CountryCode { get; set; } = "CA";
        public string Province { get; set; } = "ON";
        public string PreferredLanguage { get; set; } = "en-CA";
        public string TimeZone { get; set; } = "America/Toronto";
        public bool IsDemo { get; set; }
        public string DemoTemplate { get; set; } = string.Empty;
        public DateTime? DemoExpiresUtc { get; set; }
        public DateTime? DemoResetAtUtc { get; set; }
        public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Trial;
        public string StripeCustomerId { get; set; } = string.Empty;
        public string StripeSubscriptionId { get; set; } = string.Empty;
        public DateTime? TrialEndsUtc { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
        public ICollection<OrganizationMembership> Memberships { get; set; } = new List<OrganizationMembership>();
        public ICollection<OrganizationInvitation> Invitations { get; set; } = new List<OrganizationInvitation>();
        public ICollection<Property> Properties { get; set; } = new List<Property>();
    }

    public class AppUser : BaseEntity
    {
        public Guid? OrganizationId { get; set; }
        public string ClerkUserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Owner";
        public string SystemRole { get; set; } = "User";
        public string PreferredLanguage { get; set; } = "en-CA";
        public bool IsActive { get; set; } = true;
        public Organization? Organization { get; set; }
        public ICollection<OrganizationMembership> Memberships { get; set; } = new List<OrganizationMembership>();
    }

    public class OrganizationMembership : BaseEntity
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public string Role { get; set; } = "Viewer";
        public string Status { get; set; } = "Active";
        public Organization? Organization { get; set; }
        public AppUser? User { get; set; }
    }

    public class OrganizationInvitation : BaseEntity
    {
        public Guid OrganizationId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Viewer";
        public string Token { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string InvitedBy { get; set; } = string.Empty;
        public DateTime ExpiresUtc { get; set; } = DateTime.UtcNow.AddDays(7);
        public DateTime? AcceptedUtc { get; set; }
        public DateTime? RevokedUtc { get; set; }
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
        public ICollection<MediaAsset> MediaAssets { get; set; } = new List<MediaAsset>();
        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
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
        public ICollection<MediaAsset> MediaAssets { get; set; } = new List<MediaAsset>();
        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
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
        public ICollection<TenantConversation> Conversations { get; set; } = new List<TenantConversation>();
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
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }

    public class MaintenanceRequest : TenantScopedEntity
    {
        public Guid PropertyId { get; set; }
        public Guid? UnitId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public MaintenancePriority Priority { get; set; }
        public string Status { get; set; } = "Open";
        public string DispatchStatus { get; set; } = "Unassigned";
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
        public string Province { get; set; } = "ON";
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

    public class Vendor : TenantScopedEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Trade { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ServiceArea { get; set; } = string.Empty;
        public bool IsPreferred { get; set; }
        public bool IsActive { get; set; } = true;
        public string DispatchStatus { get; set; } = "Available";
        public string PreferredForPriority { get; set; } = string.Empty;
        public int TypicalResponseHours { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class Listing : TenantScopedEntity
    {
        public Guid PropertyId { get; set; }
        public Guid? UnitId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal AskingRent { get; set; }
        public ListingStatus Status { get; set; } = ListingStatus.Draft;
        public string PublishTargets { get; set; } = string.Empty;
        public DateTime? PublishedUtc { get; set; }
        public Property? Property { get; set; }
        public Unit? Unit { get; set; }
        public ICollection<Lead> Leads { get; set; } = new List<Lead>();
        public ICollection<MediaAsset> MediaAssets { get; set; } = new List<MediaAsset>();
    }

    public class Lead : TenantScopedEntity
    {
        public Guid ListingId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public LeadStatus Status { get; set; } = LeadStatus.New;
        public string Notes { get; set; } = string.Empty;
        public Listing? Listing { get; set; }
        public ICollection<Showing> Showings { get; set; } = new List<Showing>();
    }

    public class Showing : TenantScopedEntity
    {
        public Guid ListingId { get; set; }
        public Guid? LeadId { get; set; }
        public DateTime ScheduledUtc { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public Listing? Listing { get; set; }
        public Lead? Lead { get; set; }
    }

    public class Invoice : TenantScopedEntity
    {
        public Guid LeaseId { get; set; }
        public string Number { get; set; } = string.Empty;
        public DateOnly DueDate { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Draft;
        public string Notes { get; set; } = string.Empty;
        public Lease? Lease { get; set; }
        public ICollection<PaymentEntry> Payments { get; set; } = new List<PaymentEntry>();
    }

    public class PaymentEntry : TenantScopedEntity
    {
        public Guid InvoiceId { get; set; }
        public DateOnly ReceivedDate { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public Invoice? Invoice { get; set; }
    }

    public class MediaAsset : TenantScopedEntity
    {
        public Guid? PropertyId { get; set; }
        public Guid? UnitId { get; set; }
        public Guid? ListingId { get; set; }
        public Guid? MaintenanceRequestId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string BlobPath { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsPrimary { get; set; }
        public MediaAssetCategory Category { get; set; } = MediaAssetCategory.PropertyPhoto;
        public Property? Property { get; set; }
        public Unit? Unit { get; set; }
        public Listing? Listing { get; set; }
        public MaintenanceRequest? MaintenanceRequest { get; set; }
    }

    public class AISuggestionLog : TenantScopedEntity
    {
        public AISuggestionType SuggestionType { get; set; } = AISuggestionType.WorkQueue;
        public string SourceEntityName { get; set; } = string.Empty;
        public Guid? SourceEntityId { get; set; }
        public string PromptSummary { get; set; } = string.Empty;
        public string SuggestedContent { get; set; } = string.Empty;
        public bool ReviewedByHuman { get; set; }
        public string ReviewOutcome { get; set; } = string.Empty;
    }

    public class TenantConversation : TenantScopedEntity
    {
        public Guid TenantId { get; set; }
        public Guid? LeaseId { get; set; }
        public Guid? MaintenanceRequestId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public ConversationChannel Channel { get; set; } = ConversationChannel.Email;
        public string Status { get; set; } = "Draft";
        public DateTime? LastContactUtc { get; set; }
        public Tenant? Tenant { get; set; }
        public Lease? Lease { get; set; }
        public MaintenanceRequest? MaintenanceRequest { get; set; }
        public ICollection<TenantMessage> Messages { get; set; } = new List<TenantMessage>();
    }

    public class TenantMessage : TenantScopedEntity
    {
        public Guid TenantConversationId { get; set; }
        public bool IsIncoming { get; set; }
        public string Body { get; set; } = string.Empty;
        public string SentBy { get; set; } = string.Empty;
        public DateTime SentUtc { get; set; }
        public bool IsAISuggested { get; set; }
        public string DeliveryMethod { get; set; } = string.Empty;
        public DateTime? DeliveredUtc { get; set; }
        public string DeliveryProof { get; set; } = string.Empty;
        public TenantConversation? Conversation { get; set; }
    }
}
