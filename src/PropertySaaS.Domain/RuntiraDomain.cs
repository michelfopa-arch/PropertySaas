namespace PropertySaaS.Domain.Common
{
    public abstract class RuntiraTenantEntity : BaseEntity
    {
        public Guid TenantId { get; set; }
    }
}

namespace PropertySaaS.Domain.Entities
{
    using PropertySaaS.Domain.Common;

    public sealed class RuntiraOrganization : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public string DefaultLocale { get; set; } = "en";
        public string CountryCode { get; set; } = "CA";
        public string RegionCode { get; set; } = "ON";
        public string TimeZone { get; set; } = "America/Toronto";
        public string LegalProfileJson { get; set; } = "{}";
        public string AdditionalSettingsJson { get; set; } = "{}";
        public bool IsActive { get; set; } = true;
        public ICollection<RuntiraMembership> Memberships { get; set; } = new List<RuntiraMembership>();
        public ICollection<RuntiraAsset> Assets { get; set; } = new List<RuntiraAsset>();
    }

    public sealed class RuntiraUser : BaseEntity
    {
        public string ClerkUserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PreferredLanguage { get; set; } = "en";
        public bool IsSuperAdmin { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<RuntiraMembership> Memberships { get; set; } = new List<RuntiraMembership>();
    }

    public sealed class RuntiraMembership : RuntiraTenantEntity
    {
        public Guid UserId { get; set; }
        public string Role { get; set; } = "Manager";
        public string Status { get; set; } = "Active";
        public DateTime? LastSelectedUtc { get; set; }
        public RuntiraOrganization? Tenant { get; set; }
        public RuntiraUser? User { get; set; }
    }

    public sealed class RuntiraAsset : RuntiraTenantEntity
    {
        public string Name { get; set; } = string.Empty;
        public string AssetType { get; set; } = "Property";
        public string AddressLine1 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string RegionCode { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public int UnitCount { get; set; }
        public string LegalProfileJson { get; set; } = "{}";
        public string AdditionalDataJson { get; set; } = "{}";
        public string WorkflowSummaryJson { get; set; } = "{}";
        public RuntiraOrganization? Tenant { get; set; }
    }

    public sealed class RuntiraConversation : RuntiraTenantEntity
    {
        public string Channel { get; set; } = "Chat";
        public string Subject { get; set; } = string.Empty;
        public string Locale { get; set; } = "en";
        public string Status { get; set; } = "Open";
        public string Intent { get; set; } = string.Empty;
        public string JurisdictionCode { get; set; } = string.Empty;
        public DateTime? LastMessageUtc { get; set; }
        public string SummaryJson { get; set; } = "{}";
        public ICollection<RuntiraMessage> Messages { get; set; } = new List<RuntiraMessage>();
    }

    public sealed class RuntiraMessage : RuntiraTenantEntity
    {
        public Guid ConversationId { get; set; }
        public string Direction { get; set; } = "Incoming";
        public string AuthorType { get; set; } = "User";
        public string Content { get; set; } = string.Empty;
        public string StructuredPayloadJson { get; set; } = "{}";
        public bool RequiresAction { get; set; }
        public string CreatedByEmail { get; set; } = string.Empty;
        public RuntiraConversation? Conversation { get; set; }
    }

    public sealed class RuntiraWorkflowTemplate : RuntiraTenantEntity
    {
        public string Name { get; set; } = string.Empty;
        public string TriggerType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PromptTemplate { get; set; } = string.Empty;
        public string RequiredQuestionsJson { get; set; } = "[]";
        public string ValidationSchemaJson { get; set; } = "{}";
        public bool IsActive { get; set; } = true;
    }

    public sealed class RuntiraBlobArchive : RuntiraTenantEntity
    {
        public string BlobPath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string MetadataJson { get; set; } = "{}";
        public long SizeBytes { get; set; }
        public string SourceSystem { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
    }

    public sealed class RuntiraJurisdictionProfile : RuntiraTenantEntity
    {
        public string CountryCode { get; set; } = "CA";
        public string RegionCode { get; set; } = "ON";
        public string SupportedLanguagesJson { get; set; } = "[\"en\"]";
        public string RequiredQuestionsJson { get; set; } = "[]";
        public string ValidationRulesJson { get; set; } = "{}";
        public string InvoiceRulesJson { get; set; } = "{}";
        public string AssetRulesJson { get; set; } = "{}";
        public string MaintenanceRulesJson { get; set; } = "{}";
    }

    public sealed class RuntiraQuotaPolicy : RuntiraTenantEntity
    {
        public int MaxAssets { get; set; } = 100;
        public int MaxDocuments { get; set; } = 1000;
        public int MaxMonthlyAiRequests { get; set; } = 5000;
        public int MaxBlobStorageMb { get; set; } = 2048;
        public int MaxActiveWorkflows { get; set; } = 50;
        public bool EnforceHardLimit { get; set; } = true;
    }
}
