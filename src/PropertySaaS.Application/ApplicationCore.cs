using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using PropertySaaS.Domain.Entities;
using PropertySaaS.Domain.Enums;

namespace PropertySaaS.Application.Abstractions
{
    public interface IApplicationDbContext
    {
        DbSet<Organization> Organizations { get; }
        DbSet<AppUser> Users { get; }
        DbSet<OrganizationMembership> OrganizationMemberships { get; }
        DbSet<OrganizationInvitation> OrganizationInvitations { get; }
        DbSet<Property> Properties { get; }
        DbSet<Unit> Units { get; }
        DbSet<Tenant> Tenants { get; }
        DbSet<Lease> Leases { get; }
        DbSet<MaintenanceRequest> MaintenanceRequests { get; }
        DbSet<AuditLog> AuditLogs { get; }
        DbSet<ComplianceReminder> ComplianceReminders { get; }
        DbSet<DocumentTemplate> DocumentTemplates { get; }
        DbSet<Vendor> Vendors { get; }
        DbSet<Listing> Listings { get; }
        DbSet<Lead> Leads { get; }
        DbSet<Showing> Showings { get; }
        DbSet<Invoice> Invoices { get; }
        DbSet<PaymentEntry> PaymentEntries { get; }
        DbSet<MediaAsset> MediaAssets { get; }
        DbSet<AISuggestionLog> AISuggestionLogs { get; }
        DbSet<TenantConversation> TenantConversations { get; }
        DbSet<TenantMessage> TenantMessages { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

namespace PropertySaaS.Application.Common
{
    public class JurisdictionProfile
    {
        public string CountryCode { get; init; } = "CA";
        public string ProvinceCode { get; init; } = "ON";
        public string ProvinceDisplayName { get; init; } = "Ontario";
        public string DefaultLanguage { get; init; } = "en-CA";
        public IReadOnlyList<string> SupportedLanguages { get; init; } = new[] { "en-CA" };
        public IReadOnlyList<string> NoticeTypes { get; init; } = Array.Empty<string>();
        public string LeasePackageLabel { get; init; } = "Lease package";
        public IReadOnlyDictionary<string, string> DocumentTemplates { get; init; } = new Dictionary<string, string>();
        public IReadOnlyList<string> ComplianceChecklist { get; init; } = Array.Empty<string>();
    }

    public static class JurisdictionCatalog
    {
        private static readonly Dictionary<string, JurisdictionProfile> Profiles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ON"] = new JurisdictionProfile
            {
                ProvinceCode = "ON",
                ProvinceDisplayName = "Ontario",
                DefaultLanguage = "en-CA",
                SupportedLanguages = new[] { "en-CA", "fr-CA" },
                NoticeTypes = new[] { "N1", "N4", "SOL" },
                LeasePackageLabel = "Ontario Standard Lease Package",
                DocumentTemplates = new Dictionary<string, string>
                {
                    ["lease-package"] = "Ontario Standard Lease Package",
                    ["non-payment-notice"] = "N4 Non-payment Notice",
                    ["rent-increase-notice"] = "N1 Rent Increase Notice"
                },
                ComplianceChecklist = new[]
                {
                    "Review 90-day rent increase workflows",
                    "Track the latest Ontario lease form version",
                    "Retain audit trail for notices and service dates"
                }
            },
            ["QC"] = new JurisdictionProfile
            {
                ProvinceCode = "QC",
                ProvinceDisplayName = "Québec",
                DefaultLanguage = "fr-CA",
                SupportedLanguages = new[] { "fr-CA", "en-CA" },
                NoticeTypes = new[] { "TAL", "RentReview", "LeaseRenewal" },
                LeasePackageLabel = "Québec Residential Lease Package",
                DocumentTemplates = new Dictionary<string, string>
                {
                    ["lease-package"] = "Québec Residential Lease Package",
                    ["non-payment-notice"] = "Notice to pay rent or begin TAL file",
                    ["rent-increase-notice"] = "Lease renewal and rent adjustment notice"
                },
                ComplianceChecklist = new[]
                {
                    "Track Tribunal administratif du logement timelines",
                    "Prepare lease wording and notices in the appropriate language",
                    "Retain delivery proof and resident communication history"
                }
            },
            ["AB"] = new JurisdictionProfile
            {
                ProvinceCode = "AB",
                ProvinceDisplayName = "Alberta",
                DefaultLanguage = "en-CA",
                SupportedLanguages = new[] { "en-CA", "fr-CA" },
                NoticeTypes = new[] { "RentIncrease", "Termination", "Inspection" },
                LeasePackageLabel = "Alberta Residential Tenancy Package",
                DocumentTemplates = new Dictionary<string, string>
                {
                    ["lease-package"] = "Alberta Residential Tenancy Package",
                    ["non-payment-notice"] = "Alberta non-payment notice workflow",
                    ["rent-increase-notice"] = "Alberta rent increase notice"
                },
                ComplianceChecklist = new[]
                {
                    "Track notice windows for rent changes and terminations",
                    "Review inspection and entry documentation requirements",
                    "Keep service evidence and lease package history organized"
                }
            }
        };

        public static JurisdictionProfile GetProfile(string? province)
            => Profiles.TryGetValue(province ?? string.Empty, out var profile)
                ? profile
                : Profiles["ON"];

        public static IReadOnlyList<string> SupportedCultureNames => Profiles.Values
            .SelectMany(x => x.SupportedLanguages)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    public class CurrentOrganization
    {
        public Guid UserId { get; set; }
        public Guid OrganizationId { get; set; }
        public int AccessibleOrganizationCount { get; set; }
        public bool HasSuperAdminOrganizationSelection { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public bool IsDemo { get; set; }
        public DateTime? DemoExpiresUtc { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Owner";
        public string SystemRole { get; set; } = "User";
        public string Province { get; set; } = "ON";
        public string CountryCode { get; set; } = "CA";
        public string PreferredLanguage { get; set; } = "en-CA";
        public bool SubscriptionIsActive { get; set; } = true;
        public bool TrialExpired { get; set; }
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserEmail);
        public bool HasOrganizationAccess => OrganizationId != Guid.Empty;
        public bool CanAccessWorkspace => HasOrganizationAccess && (SubscriptionIsActive || !TrialExpired);
        public bool RequiresOrganizationSelection => !HasOrganizationAccess && AccessibleOrganizationCount > 1;
        public bool CanManageData => Role is "Owner" or "Manager";
        public bool IsSuperAdmin => string.Equals(SystemRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        public JurisdictionProfile Jurisdiction => JurisdictionCatalog.GetProfile(Province);
    }

    public sealed class MemberSummaryDto
    {
        public Guid MembershipId { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public sealed class OrganizationAccessOptionDto
    {
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public bool IsDemo { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public sealed class InvitationSummaryDto
    {
        public Guid InvitationId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ExpiresUtc { get; set; }
        public string Token { get; set; } = string.Empty;
        public bool IsExpired => string.Equals(Status, "Expired", StringComparison.OrdinalIgnoreCase) || ExpiresUtc < DateTime.UtcNow;
    }

    public sealed class PendingWorkspaceInvitationDto
    {
        public string OrganizationName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime ExpiresUtc { get; set; }
        public string Token { get; set; } = string.Empty;
    }

    public sealed class OrganizationInviteResult
    {
        public Guid InvitationId { get; set; }
        public string InvitationUrl { get; set; } = string.Empty;
        public DateTime ExpiresUtc { get; set; }
    }

    public sealed class SubscriptionEntitlementsDto
    {
        public string PlanName { get; set; } = string.Empty;
        public int MaxUnits { get; set; }
        public int MaxUsers { get; set; }
        public bool IncludesCompliance { get; set; }
        public bool IncludesAuditLogs { get; set; }
        public bool IncludesPrioritySupport { get; set; }
        public bool IncludesAdvancedExports { get; set; }
        public string TrialBanner { get; set; } = string.Empty;
        public DateTime? TrialEndsUtc { get; set; }
        public int? TrialDaysRemaining { get; set; }
    }

    public sealed class SupportSessionDto
    {
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime ExpiresUtc { get; set; }
    }

    public sealed class VendorSummaryDto
    {
        public Guid VendorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Trade { get; set; } = string.Empty;
        public string ServiceArea { get; set; } = string.Empty;
        public bool IsPreferred { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string DispatchStatus { get; set; } = string.Empty;
        public string PreferredForPriority { get; set; } = string.Empty;
        public int TypicalResponseHours { get; set; }
        public int LinkedOpenRequests { get; set; }
    }

    public sealed class VendorRecommendationDto
    {
        public Guid MaintenanceRequestId { get; set; }
        public Guid? VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string DispatchStatus { get; set; } = string.Empty;
        public string RecommendationReason { get; set; } = string.Empty;
        public int TypicalResponseHours { get; set; }
    }

    public sealed class ListingSummaryDto
    {
        public Guid ListingId { get; set; }
        public Guid PropertyId { get; set; }
        public Guid? UnitId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string UnitLabel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal AskingRent { get; set; }
        public string PublishTargets { get; set; } = string.Empty;
        public string PublishChannelsLabel { get; set; } = string.Empty;
        public int PublishReadinessScore { get; set; }
        public string PublishReadinessSummary { get; set; } = string.Empty;
        public int MediaAssetCount { get; set; }
        public string ShortCopy { get; set; } = string.Empty;
        public string LongCopy { get; set; } = string.Empty;
        public DateTime? PublishedUtc { get; set; }
    }

    public sealed class InvoiceSummaryDto
    {
        public Guid InvoiceId { get; set; }
        public string Number { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string UnitLabel { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateOnly DueDate { get; set; }
    }

    public sealed class LeadSummaryDto
    {
        public Guid LeadId { get; set; }
        public Guid ListingId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string ListingTitle { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public sealed class ShowingSummaryDto
    {
        public Guid ShowingId { get; set; }
        public Guid ListingId { get; set; }
        public Guid? LeadId { get; set; }
        public string ListingTitle { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public DateTime ScheduledUtc { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public sealed class PaymentEntrySummaryDto
    {
        public Guid PaymentEntryId { get; set; }
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly ReceivedDate { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }

    public sealed class MediaAssetSummaryDto
    {
        public Guid MediaAssetId { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? UnitId { get; set; }
        public Guid? ListingId { get; set; }
        public Guid? MaintenanceRequestId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string BlobPath { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public DateTime CreatedUtc { get; set; }
        public bool IsPrimary { get; set; }
        public string ScopeLabel { get; set; } = string.Empty;
    }

    public sealed class MaintenanceEvidenceSummaryDto
    {
        public Guid MaintenanceRequestId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string UnitLabel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string DispatchStatus { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public DateOnly RequestedDate { get; set; }
        public int EvidenceCount { get; set; }
        public string DossierSignature { get; set; } = string.Empty;
        public string EvidencePackSummary { get; set; } = string.Empty;
    }

    public sealed class AISuggestionSummaryDto
    {
        public Guid SuggestionId { get; set; }
        public string SuggestionType { get; set; } = string.Empty;
        public string SourceEntityName { get; set; } = string.Empty;
        public string PromptSummary { get; set; } = string.Empty;
        public string SuggestedContent { get; set; } = string.Empty;
        public bool ReviewedByHuman { get; set; }
    }

    public sealed class TenantConversationSummaryDto
    {
        public Guid TenantConversationId { get; set; }
        public Guid TenantId { get; set; }
        public Guid? LeaseId { get; set; }
        public Guid? MaintenanceRequestId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string UnitLabel { get; set; } = string.Empty;
        public string LastMessagePreview { get; set; } = string.Empty;
        public DateTime? LastContactUtc { get; set; }
        public int MessageCount { get; set; }
    }

    public sealed class TenantMessageSummaryDto
    {
        public Guid TenantMessageId { get; set; }
        public Guid TenantConversationId { get; set; }
        public bool IsIncoming { get; set; }
        public string Body { get; set; } = string.Empty;
        public string SentBy { get; set; } = string.Empty;
        public DateTime SentUtc { get; set; }
        public bool IsAISuggested { get; set; }
        public string DeliveryMethod { get; set; } = string.Empty;
        public DateTime? DeliveredUtc { get; set; }
        public string DeliveryProof { get; set; } = string.Empty;
    }

    public sealed class TenantMessageTemplateDto
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string BodyTemplate { get; set; } = string.Empty;
    }

    public sealed class MaintenanceCommunicationSummaryDto
    {
        public Guid TenantMessageId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public bool IsIncoming { get; set; }
        public string Body { get; set; } = string.Empty;
        public string SentBy { get; set; } = string.Empty;
        public DateTime SentUtc { get; set; }
        public string DeliveryMethod { get; set; } = string.Empty;
        public DateTime? DeliveredUtc { get; set; }
        public string DeliveryProof { get; set; } = string.Empty;
    }

    public sealed class SuperAdminOrganizationDto
    {
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public bool IsDemo { get; set; }
        public string SubscriptionTier { get; set; } = string.Empty;
        public int Units { get; set; }
        public int Users { get; set; }
    }

    public class DashboardSummaryDto
    {
        public int Properties { get; set; }
        public int Units { get; set; }
        public int OccupiedUnits { get; set; }
        public int VacantUnits => Math.Max(Units - OccupiedUnits, 0);
        public int Tenants { get; set; }
        public int ActiveLeases { get; set; }
        public int OpenMaintenance { get; set; }
        public int DispatchAtRisk { get; set; }
        public int DispatchBlocked { get; set; }
        public int ComplianceDueSoon { get; set; }
        public int JurisdictionNoticesInWorkflow { get; set; }
        public int ExportFeedsReady { get; set; } = 3;
        public decimal MonthlyRentRoll { get; set; }
        public string SubscriptionTier { get; set; } = "Growth";
        public decimal OccupancyRate => Units == 0 ? 0 : Math.Round((decimal)OccupiedUnits / Units * 100, 2);
    }

    public sealed class DispatchSignalDto
    {
        public Guid MaintenanceRequestId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string DispatchStatus { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public MaintenancePriority Priority { get; set; }
        public DateOnly RequestedDate { get; set; }
        public int TypicalResponseHours { get; set; }
        public bool IsAtRisk { get; set; }
        public bool IsBlocked { get; set; }
    }
}

namespace PropertySaaS.Application.Features
{
    using PropertySaaS.Application.Abstractions;
    using PropertySaaS.Application.Common;

    public class SaasDataService
    {
        private readonly IApplicationDbContext _db;
        private readonly CurrentOrganization _current;
        private readonly INotificationService _notifications;
        private const string SupportRole = "SupportViewer";

        public SaasDataService(IApplicationDbContext db, CurrentOrganization current, INotificationService notifications)
        {
            _db = db;
            _current = current;
            _notifications = notifications;
        }

        private void EnsureCanManageData()
        {
            if (!_current.CanManageData)
            {
                throw new InvalidOperationException("Current role cannot modify portfolio data.");
            }
        }

        private async Task<SubscriptionEntitlementsDto> GetEntitlementsInternalAsync(CancellationToken cancellationToken = default)
        {
            var organization = await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == _current.OrganizationId, cancellationToken);
            var tier = organization?.SubscriptionTier ?? SubscriptionTier.Trial;
            var trialEndsUtc = organization?.TrialEndsUtc;
            int? trialDaysRemaining = trialEndsUtc.HasValue
                ? Math.Max(0, (int)Math.Ceiling((trialEndsUtc.Value - DateTime.UtcNow).TotalDays))
                : null;

            return tier switch
            {
                SubscriptionTier.Starter => new SubscriptionEntitlementsDto
                {
                    PlanName = "Starter",
                    MaxUnits = 25,
                    MaxUsers = 2,
                    IncludesCompliance = true,
                    IncludesAuditLogs = false,
                    IncludesPrioritySupport = false,
                    IncludesAdvancedExports = false,
                    TrialBanner = string.Empty,
                    TrialEndsUtc = trialEndsUtc,
                    TrialDaysRemaining = trialDaysRemaining
                },
                SubscriptionTier.Growth => new SubscriptionEntitlementsDto
                {
                    PlanName = "Growth",
                    MaxUnits = 150,
                    MaxUsers = 8,
                    IncludesCompliance = true,
                    IncludesAuditLogs = true,
                    IncludesPrioritySupport = false,
                    IncludesAdvancedExports = true,
                    TrialBanner = string.Empty,
                    TrialEndsUtc = trialEndsUtc,
                    TrialDaysRemaining = trialDaysRemaining
                },
                SubscriptionTier.Pro => new SubscriptionEntitlementsDto
                {
                    PlanName = "Pro",
                    MaxUnits = int.MaxValue,
                    MaxUsers = 25,
                    IncludesCompliance = true,
                    IncludesAuditLogs = true,
                    IncludesPrioritySupport = true,
                    IncludesAdvancedExports = true,
                    TrialBanner = string.Empty,
                    TrialEndsUtc = trialEndsUtc,
                    TrialDaysRemaining = trialDaysRemaining
                },
                _ => new SubscriptionEntitlementsDto
                {
                    PlanName = "Trial",
                    MaxUnits = 10,
                    MaxUsers = 3,
                    IncludesCompliance = true,
                    IncludesAuditLogs = true,
                    IncludesPrioritySupport = false,
                    IncludesAdvancedExports = false,
                    TrialBanner = "14-day trial active",
                    TrialEndsUtc = trialEndsUtc,
                    TrialDaysRemaining = trialDaysRemaining
                }
            };
        }

        private async Task EnsureUserLimitAsync(CancellationToken cancellationToken = default)
        {
            var entitlements = await GetEntitlementsInternalAsync(cancellationToken);
            var activeUsers = await _db.OrganizationMemberships.CountAsync(x => x.OrganizationId == _current.OrganizationId && x.Status == "Active", cancellationToken);
            if (entitlements.MaxUsers != int.MaxValue && activeUsers >= entitlements.MaxUsers)
            {
                throw new InvalidOperationException($"The current {entitlements.PlanName} plan allows up to {entitlements.MaxUsers} active users.");
            }
        }

        private async Task EnsureUnitLimitAsync(int additionalUnits = 1, CancellationToken cancellationToken = default)
        {
            var entitlements = await GetEntitlementsInternalAsync(cancellationToken);
            var unitCount = await _db.Units.CountAsync(x => x.OrganizationId == _current.OrganizationId, cancellationToken);
            if (entitlements.MaxUnits != int.MaxValue && unitCount + additionalUnits > entitlements.MaxUnits)
            {
                throw new InvalidOperationException($"The current {entitlements.PlanName} plan allows up to {entitlements.MaxUnits} units.");
            }
        }

        private async Task EnsureFeatureEnabledAsync(Func<SubscriptionEntitlementsDto, bool> predicate, string featureName, CancellationToken cancellationToken = default)
        {
            var entitlements = await GetEntitlementsInternalAsync(cancellationToken);
            if (!predicate(entitlements))
            {
                throw new InvalidOperationException($"{featureName} is not included in the current {entitlements.PlanName} plan.");
            }
        }

        private void AddAISuggestion(AISuggestionType suggestionType, string sourceEntityName, Guid? sourceEntityId, string promptSummary, string suggestedContent)
        {
            _db.AISuggestionLogs.Add(new AISuggestionLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = _current.OrganizationId,
                SuggestionType = suggestionType,
                SourceEntityName = sourceEntityName,
                SourceEntityId = sourceEntityId,
                PromptSummary = promptSummary,
                SuggestedContent = suggestedContent,
                ReviewedByHuman = false,
                ReviewOutcome = string.Empty,
                CreatedUtc = DateTime.UtcNow
            });
        }

        private async Task<TenantConversation?> EnsureTenantConversationAsync(Guid tenantId, Guid? leaseId, Guid? maintenanceRequestId, string subject, ConversationChannel channel, string firstMessageBody, string sourceEntityName, Guid? sourceEntityId)
        {
            var existingConversation = await _db.TenantConversations
                .FirstOrDefaultAsync(x => x.OrganizationId == _current.OrganizationId
                    && x.TenantId == tenantId
                    && x.LeaseId == leaseId
                    && x.MaintenanceRequestId == maintenanceRequestId
                    && x.Subject == subject);

            if (existingConversation is not null)
            {
                return existingConversation;
            }

            var conversation = new TenantConversation
            {
                Id = Guid.NewGuid(),
                OrganizationId = _current.OrganizationId,
                TenantId = tenantId,
                LeaseId = leaseId,
                MaintenanceRequestId = maintenanceRequestId,
                Subject = subject,
                Channel = channel,
                Status = "Awaiting reply",
                LastContactUtc = DateTime.UtcNow,
                CreatedUtc = DateTime.UtcNow
            };

            _db.TenantConversations.Add(conversation);
            _db.TenantMessages.Add(new TenantMessage
            {
                Id = Guid.NewGuid(),
                OrganizationId = _current.OrganizationId,
                TenantConversationId = conversation.Id,
                IsIncoming = false,
                Body = firstMessageBody,
                SentBy = _current.UserEmail,
                SentUtc = DateTime.UtcNow,
                IsAISuggested = true,
                CreatedUtc = DateTime.UtcNow
            });

            AddAISuggestion(AISuggestionType.TenantMessage, sourceEntityName, sourceEntityId, $"Prepare resident communication for {subject}", firstMessageBody);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(TenantConversation), Action = "AutoCreate", PerformedBy = _current.UserEmail, Details = $"Auto-created tenant conversation {subject}" });
            return conversation;
        }

        private async Task RecalculateInvoiceAsync(Guid invoiceId)
        {
            var invoice = await _db.Invoices.FirstOrDefaultAsync(x => x.Id == invoiceId && x.OrganizationId == _current.OrganizationId);
            if (invoice is null)
            {
                return;
            }

            var totalPaid = await _db.PaymentEntries
                .Where(x => x.InvoiceId == invoiceId && x.OrganizationId == _current.OrganizationId)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            invoice.Balance = Math.Max(invoice.Amount - totalPaid, 0m);
            invoice.Status = invoice.Balance <= 0m
                ? PaymentStatus.Paid
                : totalPaid > 0m
                    ? PaymentStatus.PartiallyPaid
                    : invoice.DueDate < DateOnly.FromDateTime(DateTime.Today)
                        ? PaymentStatus.Overdue
                        : PaymentStatus.Sent;
        }

        private static IReadOnlyList<string> SplitPublishTargets(string publishTargets)
            => string.IsNullOrWhiteSpace(publishTargets)
                ? Array.Empty<string>()
                : publishTargets.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        private static ListingSummaryDto BuildListingSummary(Listing listing)
        {
            var channels = SplitPublishTargets(listing.PublishTargets);
            var mediaCount = listing.MediaAssets?.Count ?? 0;
            var readiness = 0;

            if (!string.IsNullOrWhiteSpace(listing.Title)) readiness += 25;
            if (!string.IsNullOrWhiteSpace(listing.Description)) readiness += 25;
            if (listing.AskingRent > 0m) readiness += 15;
            if (channels.Count > 0) readiness += 20;
            if (mediaCount > 0) readiness += 15;

            var unitLabel = listing.Unit?.UnitNumber ?? string.Empty;
            var propertyName = listing.Property?.Name ?? string.Empty;
            var amenitySummary = listing.Property?.AmenitySummary ?? string.Empty;
            var neighborhoodNotes = listing.Property?.NeighborhoodNotes ?? string.Empty;
            var unitDescriptor = string.IsNullOrWhiteSpace(unitLabel) ? propertyName : $"{propertyName} - Unit {unitLabel}";
            var shortCopy = $"{listing.Title}: {unitDescriptor} at {listing.AskingRent:C} with {TrimSentence(amenitySummary, 80)}";
            var longCopy = $"{listing.Title} is positioned for renters seeking {TrimSentence(neighborhoodNotes, 120)}. {TrimSentence(listing.Description, 180)} {(string.IsNullOrWhiteSpace(amenitySummary) ? string.Empty : $"Highlights include {TrimSentence(amenitySummary, 120)}.")} Published channels: {(channels.Count == 0 ? "internal review only" : string.Join(", ", channels))}.";

            return new ListingSummaryDto
            {
                ListingId = listing.Id,
                PropertyId = listing.PropertyId,
                UnitId = listing.UnitId,
                Title = listing.Title,
                Description = listing.Description,
                PropertyName = propertyName,
                UnitLabel = unitLabel,
                Status = listing.Status.ToString(),
                AskingRent = listing.AskingRent,
                PublishTargets = listing.PublishTargets,
                PublishChannelsLabel = channels.Count == 0 ? "No channels selected" : string.Join(", ", channels),
                PublishReadinessScore = readiness,
                PublishReadinessSummary = $"{readiness}% ready · {channels.Count} channel(s) · {mediaCount} media asset(s)",
                MediaAssetCount = mediaCount,
                ShortCopy = shortCopy,
                LongCopy = longCopy,
                PublishedUtc = listing.PublishedUtc
            };
        }

        private static string TrimSentence(string input, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "a professionally presented rental opportunity";
            }

            var normalized = input.Trim();
            return normalized.Length <= maxLength ? normalized : normalized[..maxLength].TrimEnd() + "...";
        }

        public async Task<ListingSummaryDto?> GetListingPublicationPreviewAsync(Guid listingId)
        {
            var listing = await _db.Listings
                .AsNoTracking()
                .Include(x => x.Property)
                .Include(x => x.Unit)
                .Include(x => x.MediaAssets)
                .FirstOrDefaultAsync(x => x.Id == listingId && x.OrganizationId == _current.OrganizationId);

            return listing is null ? null : BuildListingSummary(listing);
        }

        public async Task<string> BuildListingExportTextAsync(Guid listingId)
        {
            var listing = await GetListingPublicationPreviewAsync(listingId)
                ?? throw new InvalidOperationException("Listing was not found.");

            var builder = new StringBuilder();
            builder.AppendLine(listing.Title);
            builder.AppendLine($"Property: {listing.PropertyName}{(string.IsNullOrWhiteSpace(listing.UnitLabel) ? string.Empty : $" - Unit {listing.UnitLabel}")}");
            builder.AppendLine($"Asking rent: {listing.AskingRent:C}");
            builder.AppendLine($"Status: {listing.Status}");
            builder.AppendLine($"Publish readiness: {listing.PublishReadinessSummary}");
            builder.AppendLine($"Channels: {listing.PublishChannelsLabel}");
            builder.AppendLine();
            builder.AppendLine("Short copy");
            builder.AppendLine(listing.ShortCopy);
            builder.AppendLine();
            builder.AppendLine("Long copy");
            builder.AppendLine(listing.LongCopy);
            return builder.ToString();
        }

        public async Task<DashboardSummaryDto> GetDashboardAsync()
        {
            var id = _current.OrganizationId;
            var units = await _db.Units.Where(x => x.OrganizationId == id).ToListAsync();
            var leases = await _db.Leases.Where(x => x.OrganizationId == id).ToListAsync();
            var organization = await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            var jurisdictionProfile = JurisdictionCatalog.GetProfile(_current.Province);
            var dispatchSignals = await GetDispatchSignalsAsync();
            return new DashboardSummaryDto
            {
                Properties = await _db.Properties.CountAsync(x => x.OrganizationId == id),
                Units = units.Count,
                OccupiedUnits = units.Count(x => x.IsOccupied),
                Tenants = await _db.Tenants.CountAsync(x => x.OrganizationId == id),
                ActiveLeases = leases.Count(x => x.Status == LeaseStatus.Active || x.Status == LeaseStatus.EndingSoon),
                OpenMaintenance = await _db.MaintenanceRequests.CountAsync(x => x.OrganizationId == id && x.Status != "Closed"),
                DispatchAtRisk = dispatchSignals.Count(x => x.IsAtRisk),
                DispatchBlocked = dispatchSignals.Count(x => x.IsBlocked),
                ComplianceDueSoon = await _db.ComplianceReminders.CountAsync(x => x.OrganizationId == id && !x.IsCompleted && x.DueDate <= DateOnly.FromDateTime(DateTime.Today.AddDays(45))),
                JurisdictionNoticesInWorkflow = leases.Count(x => x.N1IncreaseNoticeScheduled) + await _db.ComplianceReminders.CountAsync(x => x.OrganizationId == id && !x.IsCompleted && jurisdictionProfile.NoticeTypes.Contains(x.NoticeType)),
                MonthlyRentRoll = units.Sum(x => x.MonthlyRent),
                SubscriptionTier = organization?.SubscriptionTier.ToString() ?? "Growth"
            };
        }

        public async Task<List<DispatchSignalDto>> GetDispatchSignalsAsync()
        {
            var maintenance = await _db.MaintenanceRequests
                .AsNoTracking()
                .Where(x => x.OrganizationId == _current.OrganizationId && x.Status != "Closed")
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.RequestedDate)
                .ToListAsync();

            var propertyMap = await _db.Properties
                .AsNoTracking()
                .Where(x => x.OrganizationId == _current.OrganizationId)
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var vendorMap = await _db.Vendors
                .AsNoTracking()
                .Where(x => x.OrganizationId == _current.OrganizationId)
                .ToDictionaryAsync(x => x.Name, x => x.TypicalResponseHours);

            return maintenance.Select(item =>
            {
                var hasVendor = !string.IsNullOrWhiteSpace(item.VendorName);
                var responseHours = hasVendor && vendorMap.TryGetValue(item.VendorName, out var hours) ? hours : 0;
                var atRisk = hasVendor && responseHours > 0 && item.RequestedDate <= DateOnly.FromDateTime(DateTime.Today.AddDays(-1)) && !string.Equals(item.DispatchStatus, "Completed", StringComparison.OrdinalIgnoreCase);
                var blocked = !hasVendor || string.Equals(item.DispatchStatus, "Unassigned", StringComparison.OrdinalIgnoreCase);

                return new DispatchSignalDto
                {
                    MaintenanceRequestId = item.Id,
                    Title = item.Title,
                    DispatchStatus = item.DispatchStatus,
                    VendorName = item.VendorName,
                    PropertyName = propertyMap.TryGetValue(item.PropertyId, out var propertyName) ? propertyName : string.Empty,
                    Priority = item.Priority,
                    RequestedDate = item.RequestedDate,
                    TypicalResponseHours = responseHours,
                    IsAtRisk = atRisk,
                    IsBlocked = blocked
                };
            }).ToList();
        }

        public async Task<SubscriptionEntitlementsDto> GetSubscriptionEntitlementsAsync()
            => await GetEntitlementsInternalAsync();

        public Task<List<Property>> GetPropertiesAsync() => _db.Properties.Where(x => x.OrganizationId == _current.OrganizationId).OrderBy(x => x.Name).ToListAsync();
        public Task<List<Unit>> GetUnitsAsync() => _db.Units.Where(x => x.OrganizationId == _current.OrganizationId).OrderBy(x => x.UnitNumber).ToListAsync();
        public Task<List<Tenant>> GetTenantsAsync() => _db.Tenants.Where(x => x.OrganizationId == _current.OrganizationId).OrderBy(x => x.FullName).ToListAsync();
        public Task<List<Lease>> GetLeasesAsync() => _db.Leases.Where(x => x.OrganizationId == _current.OrganizationId).OrderByDescending(x => x.StartDate).ToListAsync();
        public Task<List<MaintenanceRequest>> GetMaintenanceAsync() => _db.MaintenanceRequests.Where(x => x.OrganizationId == _current.OrganizationId).OrderByDescending(x => x.RequestedDate).ThenBy(x => x.DispatchStatus).ToListAsync();
        public Task<List<ComplianceReminder>> GetComplianceAsync() => _db.ComplianceReminders.Where(x => x.OrganizationId == _current.OrganizationId).OrderBy(x => x.DueDate).ToListAsync();
        public Task<List<DocumentTemplate>> GetTemplatesAsync() => _db.DocumentTemplates.Where(x => x.OrganizationId == _current.OrganizationId).OrderBy(x => x.Name).ToListAsync();
        public Task<List<AuditLog>> GetAuditLogsAsync() => _db.AuditLogs.Where(x => x.OrganizationId == _current.OrganizationId).OrderByDescending(x => x.CreatedUtc).ToListAsync();
        public Task<List<VendorSummaryDto>> GetVendorsAsync() => _db.Vendors.Where(x => x.OrganizationId == _current.OrganizationId).OrderByDescending(x => x.IsPreferred).ThenBy(x => x.Name).Select(x => new VendorSummaryDto { VendorId = x.Id, Name = x.Name, Trade = x.Trade, ServiceArea = x.ServiceArea, IsPreferred = x.IsPreferred, Email = x.Email, PhoneNumber = x.PhoneNumber, DispatchStatus = x.DispatchStatus, PreferredForPriority = x.PreferredForPriority, TypicalResponseHours = x.TypicalResponseHours, LinkedOpenRequests = _db.MaintenanceRequests.Count(request => request.OrganizationId == _current.OrganizationId && request.VendorName == x.Name && request.Status != "Closed") }).ToListAsync();
        public async Task<List<ListingSummaryDto>> GetListingsAsync()
            => (await _db.Listings
                    .AsNoTracking()
                    .Where(x => x.OrganizationId == _current.OrganizationId)
                    .Include(x => x.Property)
                    .Include(x => x.Unit)
                    .Include(x => x.MediaAssets)
                    .OrderByDescending(x => x.PublishedUtc)
                    .ThenBy(x => x.Title)
                    .ToListAsync())
                .Select(BuildListingSummary)
                .ToList();
        public Task<List<LeadSummaryDto>> GetLeadsAsync() => _db.Leads.Where(x => x.OrganizationId == _current.OrganizationId).OrderBy(x => x.Status).ThenBy(x => x.FullName).Select(x => new LeadSummaryDto { LeadId = x.Id, ListingId = x.ListingId, FullName = x.FullName, ListingTitle = x.Listing != null ? x.Listing.Title : string.Empty, Source = x.Source, Status = x.Status.ToString(), Email = x.Email }).ToListAsync();
        public Task<List<ShowingSummaryDto>> GetShowingsAsync() => _db.Showings.Where(x => x.OrganizationId == _current.OrganizationId).OrderBy(x => x.ScheduledUtc).Select(x => new ShowingSummaryDto { ShowingId = x.Id, ListingId = x.ListingId, LeadId = x.LeadId, ListingTitle = x.Listing != null ? x.Listing.Title : string.Empty, LeadName = x.Lead != null ? x.Lead.FullName : string.Empty, ScheduledUtc = x.ScheduledUtc, Status = x.Status }).ToListAsync();
        public Task<List<InvoiceSummaryDto>> GetInvoicesAsync() => _db.Invoices.Where(x => x.OrganizationId == _current.OrganizationId).OrderBy(x => x.DueDate).Select(x => new InvoiceSummaryDto { InvoiceId = x.Id, Number = x.Number, TenantName = x.Lease != null && x.Lease.Tenant != null ? x.Lease.Tenant.FullName : string.Empty, UnitLabel = x.Lease != null && x.Lease.Unit != null ? x.Lease.Unit.UnitNumber : string.Empty, Amount = x.Amount, Balance = x.Balance, Status = x.Status.ToString(), DueDate = x.DueDate }).ToListAsync();
        public Task<List<PaymentEntrySummaryDto>> GetPaymentEntriesAsync() => _db.PaymentEntries.Where(x => x.OrganizationId == _current.OrganizationId).OrderByDescending(x => x.ReceivedDate).Select(x => new PaymentEntrySummaryDto { PaymentEntryId = x.Id, InvoiceId = x.InvoiceId, InvoiceNumber = x.Invoice != null ? x.Invoice.Number : string.Empty, ReceivedDate = x.ReceivedDate, Amount = x.Amount, Method = x.Method, Reference = x.Reference }).ToListAsync();
        public Task<List<MediaAssetSummaryDto>> GetMediaAssetsAsync() => _db.MediaAssets.Where(x => x.OrganizationId == _current.OrganizationId).OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder).Select(x => new MediaAssetSummaryDto { MediaAssetId = x.Id, PropertyId = x.PropertyId, UnitId = x.UnitId, ListingId = x.ListingId, MaintenanceRequestId = x.MaintenanceRequestId, FileName = x.FileName, BlobPath = x.BlobPath, Caption = x.Caption, Category = x.Category.ToString(), SortOrder = x.SortOrder, CreatedUtc = x.CreatedUtc, IsPrimary = x.IsPrimary, ScopeLabel = x.MaintenanceRequest != null ? x.MaintenanceRequest.Title : x.Listing != null ? x.Listing.Title : x.Unit != null ? x.Unit.UnitNumber : x.Property != null ? x.Property.Name : string.Empty }).ToListAsync();
        public Task<List<MediaAssetSummaryDto>> GetMaintenanceMediaAssetsAsync() => _db.MediaAssets.Where(x => x.OrganizationId == _current.OrganizationId && x.MaintenanceRequestId != null).OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder).Select(x => new MediaAssetSummaryDto { MediaAssetId = x.Id, PropertyId = x.PropertyId, UnitId = x.UnitId, ListingId = x.ListingId, MaintenanceRequestId = x.MaintenanceRequestId, FileName = x.FileName, BlobPath = x.BlobPath, Caption = x.Caption, Category = x.Category.ToString(), SortOrder = x.SortOrder, CreatedUtc = x.CreatedUtc, IsPrimary = x.IsPrimary, ScopeLabel = x.MaintenanceRequest != null ? x.MaintenanceRequest.Title : string.Empty }).ToListAsync();
        public Task<List<MaintenanceEvidenceSummaryDto>> GetMaintenanceEvidenceAsync() => _db.MaintenanceRequests.Where(x => x.OrganizationId == _current.OrganizationId).OrderByDescending(x => x.RequestedDate).Select(x => new MaintenanceEvidenceSummaryDto { MaintenanceRequestId = x.Id, Title = x.Title, PropertyName = _db.Properties.Where(property => property.Id == x.PropertyId).Select(property => property.Name).FirstOrDefault() ?? string.Empty, UnitLabel = x.UnitId.HasValue ? _db.Units.Where(unit => unit.Id == x.UnitId.Value).Select(unit => unit.UnitNumber).FirstOrDefault() ?? string.Empty : string.Empty, Status = x.Status, DispatchStatus = x.DispatchStatus, VendorName = x.VendorName, RequestedDate = x.RequestedDate, EvidenceCount = _db.MediaAssets.Count(asset => asset.MaintenanceRequestId == x.Id), DossierSignature = $"EVP-{x.RequestedDate:yyyyMMdd}-{x.Id.ToString().Substring(0, 8).ToUpperInvariant()}", EvidencePackSummary = $"Timeline ready for {x.Title}: {_db.MediaAssets.Count(asset => asset.MaintenanceRequestId == x.Id)} item(s), status {x.Status}, dispatch {x.DispatchStatus}{(string.IsNullOrWhiteSpace(x.VendorName) ? string.Empty : $", vendor {x.VendorName}")}." }).ToListAsync();
        public Task<List<AISuggestionSummaryDto>> GetAISuggestionsAsync() => _db.AISuggestionLogs.Where(x => x.OrganizationId == _current.OrganizationId).OrderByDescending(x => x.CreatedUtc).Select(x => new AISuggestionSummaryDto { SuggestionId = x.Id, SuggestionType = x.SuggestionType.ToString(), SourceEntityName = x.SourceEntityName, PromptSummary = x.PromptSummary, SuggestedContent = x.SuggestedContent, ReviewedByHuman = x.ReviewedByHuman }).ToListAsync();
        public Task<List<TenantConversationSummaryDto>> GetTenantConversationsAsync() => _db.TenantConversations.Where(x => x.OrganizationId == _current.OrganizationId).OrderByDescending(x => x.LastContactUtc ?? x.CreatedUtc).Select(x => new TenantConversationSummaryDto { TenantConversationId = x.Id, TenantId = x.TenantId, LeaseId = x.LeaseId, MaintenanceRequestId = x.MaintenanceRequestId, TenantName = x.Tenant != null ? x.Tenant.FullName : string.Empty, Subject = x.Subject, Channel = x.Channel.ToString(), Status = x.Status, UnitLabel = x.Lease != null && x.Lease.Unit != null ? x.Lease.Unit.UnitNumber : string.Empty, LastMessagePreview = _db.TenantMessages.Where(message => message.TenantConversationId == x.Id).OrderByDescending(message => message.SentUtc).Select(message => message.Body).FirstOrDefault() ?? string.Empty, LastContactUtc = x.LastContactUtc, MessageCount = _db.TenantMessages.Count(message => message.TenantConversationId == x.Id) }).ToListAsync();
        public Task<List<TenantMessageSummaryDto>> GetTenantMessagesAsync(Guid conversationId) => _db.TenantMessages.Where(x => x.OrganizationId == _current.OrganizationId && x.TenantConversationId == conversationId).OrderBy(x => x.SentUtc).Select(x => new TenantMessageSummaryDto { TenantMessageId = x.Id, TenantConversationId = x.TenantConversationId, IsIncoming = x.IsIncoming, Body = x.Body, SentBy = x.SentBy, SentUtc = x.SentUtc, IsAISuggested = x.IsAISuggested, DeliveryMethod = x.DeliveryMethod, DeliveredUtc = x.DeliveredUtc, DeliveryProof = x.DeliveryProof }).ToListAsync();
        public Task<List<MaintenanceCommunicationSummaryDto>> GetMaintenanceCommunicationMessagesAsync(Guid maintenanceRequestId) => _db.TenantMessages
            .Where(x => x.OrganizationId == _current.OrganizationId && x.Conversation != null && x.Conversation.MaintenanceRequestId == maintenanceRequestId)
            .OrderBy(x => x.SentUtc)
            .Select(x => new MaintenanceCommunicationSummaryDto
            {
                TenantMessageId = x.Id,
                TenantName = x.Conversation != null && x.Conversation.Tenant != null ? x.Conversation.Tenant.FullName : string.Empty,
                Subject = x.Conversation != null ? x.Conversation.Subject : string.Empty,
                Channel = x.Conversation != null ? x.Conversation.Channel.ToString() : string.Empty,
                IsIncoming = x.IsIncoming,
                Body = x.Body,
                SentBy = x.SentBy,
                SentUtc = x.SentUtc,
                DeliveryMethod = x.DeliveryMethod,
                DeliveredUtc = x.DeliveredUtc,
                DeliveryProof = x.DeliveryProof
            })
            .ToListAsync();
        public Task<List<TenantMessageTemplateDto>> GetTenantMessageTemplatesAsync()
        {
            var templates = new List<TenantMessageTemplateDto>
            {
                new() { Key = "rent-reminder", Title = "Ontario rent reminder", BodyTemplate = "Hello {{tenantName}}, this is a friendly reminder regarding the rent balance for {{unitLabel}}. Please confirm your expected payment date and let us know if you need to discuss the next steps." },
                new() { Key = "n4-prep", Title = "Ontario arrears follow-up before N4", BodyTemplate = "Hello {{tenantName}}, we are following up on the outstanding rent for {{unitLabel}}. Please contact us by {{nextStepDate}} to confirm payment timing before formal Ontario notice steps are reviewed." },
                new() { Key = "maintenance-entry", Title = "Maintenance access coordination", BodyTemplate = "Hello {{tenantName}}, we are coordinating access for '{{subject}}' at {{unitLabel}}. Please confirm your availability and any access instructions so we can finalize the visit window." },
                new() { Key = "notice-delivery", Title = "Notice delivery confirmation", BodyTemplate = "Hello {{tenantName}}, this message confirms delivery of the related notice for {{unitLabel}}. Please reply to confirm receipt and keep this thread for your records." },
                new() { Key = "renewal-checkin", Title = "Lease renewal check-in", BodyTemplate = "Hello {{tenantName}}, we are starting an early renewal check-in for {{unitLabel}}. Please let us know if you would like to discuss your plans, questions or any proposed changes." }
            };

            return Task.FromResult(templates);
        }
        public async Task<Guid?> EnsureMaintenanceConversationAsync(Guid maintenanceRequestId)
        {
            EnsureCanManageData();

            var request = await _db.MaintenanceRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == maintenanceRequestId && x.OrganizationId == _current.OrganizationId);
            if (request is null || !request.UnitId.HasValue)
            {
                return null;
            }

            var lease = await _db.Leases
                .AsNoTracking()
                .Where(x => x.OrganizationId == _current.OrganizationId && x.UnitId == request.UnitId.Value && (x.Status == LeaseStatus.Active || x.Status == LeaseStatus.EndingSoon))
                .OrderByDescending(x => x.StartDate)
                .FirstOrDefaultAsync();
            if (lease is null)
            {
                return null;
            }

            var unitLabel = await _db.Units.Where(x => x.Id == request.UnitId.Value).Select(x => x.UnitNumber).FirstOrDefaultAsync() ?? string.Empty;
            var conversation = await EnsureTenantConversationAsync(lease.TenantId, lease.Id, request.Id, $"Maintenance update - {request.Title}", ConversationChannel.Email, $"Hello, this is an update for maintenance request '{request.Title}'{(string.IsNullOrWhiteSpace(unitLabel) ? string.Empty : $" for unit {unitLabel}")}. We are coordinating the next step and will confirm timing shortly.", nameof(MaintenanceRequest), request.Id);
            await _db.SaveChangesAsync();
            return conversation?.Id;
        }

        public async Task<VendorRecommendationDto?> RecommendVendorForMaintenanceAsync(Guid maintenanceRequestId)
        {
            var request = await _db.MaintenanceRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == maintenanceRequestId && x.OrganizationId == _current.OrganizationId);
            if (request is null)
            {
                return null;
            }

            var preferredPriority = request.Priority.ToString();
            var vendors = await _db.Vendors
                .AsNoTracking()
                .Where(x => x.OrganizationId == _current.OrganizationId && x.IsActive)
                .OrderByDescending(x => x.IsPreferred)
                .ThenBy(x => x.TypicalResponseHours)
                .ThenBy(x => x.Name)
                .ToListAsync();

            var vendor = vendors.FirstOrDefault(x => string.Equals(x.PreferredForPriority, preferredPriority, StringComparison.OrdinalIgnoreCase))
                ?? vendors.FirstOrDefault(x => x.IsPreferred)
                ?? vendors.FirstOrDefault();

            if (vendor is null)
            {
                return null;
            }

            var reason = string.Equals(vendor.PreferredForPriority, preferredPriority, StringComparison.OrdinalIgnoreCase)
                ? $"Preferred for {preferredPriority} priority tickets"
                : vendor.IsPreferred
                    ? "Preferred vendor fallback"
                    : "Best available vendor currently on roster";

            return new VendorRecommendationDto
            {
                MaintenanceRequestId = maintenanceRequestId,
                VendorId = vendor.Id,
                VendorName = vendor.Name,
                DispatchStatus = vendor.DispatchStatus,
                RecommendationReason = reason,
                TypicalResponseHours = vendor.TypicalResponseHours
            };
        }

        public async Task<bool> AssignRecommendedVendorAsync(Guid maintenanceRequestId)
        {
            EnsureCanManageData();

            var request = await _db.MaintenanceRequests.FirstOrDefaultAsync(x => x.Id == maintenanceRequestId && x.OrganizationId == _current.OrganizationId);
            if (request is null)
            {
                return false;
            }

            var recommendation = await RecommendVendorForMaintenanceAsync(maintenanceRequestId);
            if (recommendation is null || string.IsNullOrWhiteSpace(recommendation.VendorName))
            {
                return false;
            }

            request.VendorName = recommendation.VendorName;
            if (string.Equals(request.Status, "Open", StringComparison.OrdinalIgnoreCase))
            {
                request.Status = "Scheduled";
            }
            request.DispatchStatus = "Assigned";

            request.ModifiedUtc = DateTime.UtcNow;

            _db.AuditLogs.Add(new AuditLog
            {
                OrganizationId = _current.OrganizationId,
                EntityName = nameof(MaintenanceRequest),
                Action = "AssignRecommendedVendor",
                PerformedBy = _current.UserEmail,
                Details = $"Assigned recommended vendor {recommendation.VendorName} to maintenance request {request.Title}"
            });

            AddAISuggestion(
                AISuggestionType.TenantMessage,
                nameof(MaintenanceRequest),
                request.Id,
                $"Draft a resident update for maintenance request {request.Title}",
                $"Confirm that {recommendation.VendorName} has been assigned, share the expected response window of {recommendation.TypicalResponseHours} hours, and explain the next scheduling step.");

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateMaintenanceDispatchStatusAsync(Guid maintenanceRequestId, string dispatchStatus)
        {
            EnsureCanManageData();

            var normalizedStatus = dispatchStatus?.Trim() ?? string.Empty;
            var allowedStatuses = new[] { "Unassigned", "Recommended", "Assigned", "Acknowledged", "Completed" };
            if (!allowedStatuses.Contains(normalizedStatus, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            var request = await _db.MaintenanceRequests.FirstOrDefaultAsync(x => x.Id == maintenanceRequestId && x.OrganizationId == _current.OrganizationId);
            if (request is null)
            {
                return false;
            }

            if (string.Equals(normalizedStatus, "Recommended", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(request.DispatchStatus, "Unassigned", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(normalizedStatus, "Acknowledged", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(request.VendorName))
            {
                return false;
            }

            request.DispatchStatus = allowedStatuses.First(x => string.Equals(x, normalizedStatus, StringComparison.OrdinalIgnoreCase));

            if (string.Equals(request.DispatchStatus, "Acknowledged", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(request.Status, "Closed", StringComparison.OrdinalIgnoreCase))
            {
                request.Status = "In Progress";
            }

            if (string.Equals(request.DispatchStatus, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                request.Status = "Closed";
            }

            request.ModifiedUtc = DateTime.UtcNow;

            _db.AuditLogs.Add(new AuditLog
            {
                OrganizationId = _current.OrganizationId,
                EntityName = nameof(MaintenanceRequest),
                Action = "DispatchStatus",
                PerformedBy = _current.UserEmail,
                Details = $"Set dispatch status for maintenance request {request.Title} to {request.DispatchStatus}"
            });

            if (string.Equals(request.DispatchStatus, "Acknowledged", StringComparison.OrdinalIgnoreCase))
            {
                AddAISuggestion(
                    AISuggestionType.TenantMessage,
                    nameof(MaintenanceRequest),
                    request.Id,
                    $"Draft a resident update for maintenance request {request.Title}",
                    $"Confirm that {request.VendorName} acknowledged the work order and share the active visit window or next update timing.");
            }
            else if (string.Equals(request.DispatchStatus, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                AddAISuggestion(
                    AISuggestionType.TenantMessage,
                    nameof(MaintenanceRequest),
                    request.Id,
                    $"Draft a completion update for maintenance request {request.Title}",
                    "Confirm the work is complete, summarize what was done, and ask the resident to report any remaining issues.");
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<Guid?> EnsureInvoiceConversationAsync(Guid invoiceId)
        {
            EnsureCanManageData();

            var invoice = await _db.Invoices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == invoiceId && x.OrganizationId == _current.OrganizationId);
            if (invoice is null)
            {
                return null;
            }

            var lease = await _db.Leases.AsNoTracking().FirstOrDefaultAsync(x => x.Id == invoice.LeaseId && x.OrganizationId == _current.OrganizationId);
            if (lease is null)
            {
                return null;
            }

            var unitLabel = await _db.Units.Where(x => x.Id == lease.UnitId).Select(x => x.UnitNumber).FirstOrDefaultAsync() ?? string.Empty;
            var conversation = await EnsureTenantConversationAsync(lease.TenantId, lease.Id, null, $"Rent follow-up - {invoice.Number}", ConversationChannel.Email, $"Hello, this is a friendly reminder regarding invoice {invoice.Number} for{(string.IsNullOrWhiteSpace(unitLabel) ? " your lease" : $" unit {unitLabel}")}. Please confirm the expected payment date for the outstanding balance.", nameof(Invoice), invoice.Id);
            await _db.SaveChangesAsync();
            return conversation?.Id;
        }

        public async Task AddTenantConversationAsync(TenantConversation conversation)
        {
            EnsureCanManageData();
            conversation.Id = Guid.NewGuid();
            conversation.OrganizationId = _current.OrganizationId;
            conversation.CreatedUtc = DateTime.UtcNow;
            conversation.LastContactUtc ??= DateTime.UtcNow;
            _db.TenantConversations.Add(conversation);

            var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == conversation.TenantId && x.OrganizationId == _current.OrganizationId);
            var tenantName = tenant?.FullName ?? "tenant";
            AddAISuggestion(AISuggestionType.TenantMessage, nameof(TenantConversation), conversation.Id, $"Prepare first outreach for {tenantName}", $"Draft a concise {conversation.Channel} message for {tenantName} about '{conversation.Subject}' with a clear next step and response deadline.");
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(TenantConversation), Action = "Create", PerformedBy = _current.UserEmail, Details = $"Created tenant conversation {conversation.Subject}" });
            await _db.SaveChangesAsync();
        }

        public async Task AddTenantMessageAsync(TenantMessage message)
        {
            EnsureCanManageData();
            var conversation = await _db.TenantConversations.FirstOrDefaultAsync(x => x.Id == message.TenantConversationId && x.OrganizationId == _current.OrganizationId);
            if (conversation is null)
            {
                return;
            }

            message.Id = Guid.NewGuid();
            message.OrganizationId = _current.OrganizationId;
            message.CreatedUtc = DateTime.UtcNow;
            message.SentUtc = message.SentUtc == default ? DateTime.UtcNow : message.SentUtc;
            if (!message.IsIncoming)
            {
                message.DeliveredUtc ??= message.SentUtc;
                message.DeliveryMethod = string.IsNullOrWhiteSpace(message.DeliveryMethod) ? conversation.Channel.ToString() : message.DeliveryMethod;
                message.DeliveryProof = string.IsNullOrWhiteSpace(message.DeliveryProof)
                    ? $"Sent via {message.DeliveryMethod} and logged by {_current.UserEmail} on {message.SentUtc:u}"
                    : message.DeliveryProof;
            }
            _db.TenantMessages.Add(message);

            conversation.LastContactUtc = message.SentUtc;
            if (!message.IsIncoming)
            {
                conversation.Status = "Awaiting reply";
            }

            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(TenantMessage), Action = "Create", PerformedBy = _current.UserEmail, Details = $"Logged tenant message for conversation {conversation.Subject}" });
            await _db.SaveChangesAsync();
        }

        public async Task AddMediaAssetAsync(MediaAsset mediaAsset)
        {
            EnsureCanManageData();
            mediaAsset.Id = Guid.NewGuid();
            mediaAsset.OrganizationId = _current.OrganizationId;
            mediaAsset.CreatedUtc = DateTime.UtcNow;
            _db.MediaAssets.Add(mediaAsset);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(MediaAsset), Action = "Create", PerformedBy = _current.UserEmail, Details = $"Added media asset {mediaAsset.FileName}" });
            await _db.SaveChangesAsync();
        }

        public async Task UpdateMediaAssetAsync(MediaAsset mediaAsset)
        {
            EnsureCanManageData();
            var entity = await _db.MediaAssets.FirstOrDefaultAsync(x => x.Id == mediaAsset.Id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;

            entity.PropertyId = mediaAsset.PropertyId;
            entity.UnitId = mediaAsset.UnitId;
            entity.ListingId = mediaAsset.ListingId;
            entity.MaintenanceRequestId = mediaAsset.MaintenanceRequestId;
            entity.FileName = mediaAsset.FileName;
            entity.BlobPath = mediaAsset.BlobPath;
            entity.Caption = mediaAsset.Caption;
            entity.SortOrder = mediaAsset.SortOrder;
            entity.IsPrimary = mediaAsset.IsPrimary;
            entity.Category = mediaAsset.Category;

            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(MediaAsset), Action = "Update", PerformedBy = _current.UserEmail, Details = $"Updated media asset {mediaAsset.FileName}" });
            await _db.SaveChangesAsync();
        }

        public async Task DeleteMediaAssetAsync(Guid id)
        {
            EnsureCanManageData();
            var entity = await _db.MediaAssets.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;
            _db.MediaAssets.Remove(entity);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(MediaAsset), Action = "Delete", PerformedBy = _current.UserEmail, Details = $"Deleted media asset {entity.FileName}" });
            await _db.SaveChangesAsync();
        }

        public async Task AddLeadAsync(Lead lead)
        {
            EnsureCanManageData();
            lead.Id = Guid.NewGuid();
            lead.OrganizationId = _current.OrganizationId;
            lead.CreatedUtc = DateTime.UtcNow;
            _db.Leads.Add(lead);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Lead), Action = "Create", PerformedBy = _current.UserEmail, Details = $"Created lead {lead.FullName}" });
            AddAISuggestion(AISuggestionType.WorkQueue, nameof(Lead), lead.Id, $"Qualify lead {lead.FullName}", $"Confirm renter timing, budget and next action for lead {lead.FullName} before the showing pipeline cools down.");
            await _db.SaveChangesAsync();
        }

        public async Task UpdateLeadAsync(Lead lead)
        {
            EnsureCanManageData();
            var entity = await _db.Leads.FirstOrDefaultAsync(x => x.Id == lead.Id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;

            entity.ListingId = lead.ListingId;
            entity.FullName = lead.FullName;
            entity.Email = lead.Email;
            entity.PhoneNumber = lead.PhoneNumber;
            entity.Source = lead.Source;
            entity.Status = lead.Status;
            entity.Notes = lead.Notes;

            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Lead), Action = "Update", PerformedBy = _current.UserEmail, Details = $"Updated lead {lead.FullName}" });
            await _db.SaveChangesAsync();
        }

        public async Task DeleteLeadAsync(Guid id)
        {
            EnsureCanManageData();
            var entity = await _db.Leads.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;
            _db.Leads.Remove(entity);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Lead), Action = "Delete", PerformedBy = _current.UserEmail, Details = $"Deleted lead {entity.FullName}" });
            await _db.SaveChangesAsync();
        }

        public async Task AddShowingAsync(Showing showing)
        {
            EnsureCanManageData();
            showing.Id = Guid.NewGuid();
            showing.OrganizationId = _current.OrganizationId;
            showing.CreatedUtc = DateTime.UtcNow;
            _db.Showings.Add(showing);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Showing), Action = "Create", PerformedBy = _current.UserEmail, Details = $"Scheduled showing {showing.Id}" });
            AddAISuggestion(AISuggestionType.TenantMessage, nameof(Showing), showing.Id, "Prepare showing confirmation", "Send a confirmation message with arrival window, access instructions and one follow-up question after the visit.");
            await _db.SaveChangesAsync();
        }

        public async Task UpdateShowingAsync(Showing showing)
        {
            EnsureCanManageData();
            var entity = await _db.Showings.FirstOrDefaultAsync(x => x.Id == showing.Id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;

            entity.ListingId = showing.ListingId;
            entity.LeadId = showing.LeadId;
            entity.ScheduledUtc = showing.ScheduledUtc;
            entity.Status = showing.Status;
            entity.Notes = showing.Notes;

            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Showing), Action = "Update", PerformedBy = _current.UserEmail, Details = $"Updated showing {showing.Id}" });
            await _db.SaveChangesAsync();
        }

        public async Task DeleteShowingAsync(Guid id)
        {
            EnsureCanManageData();
            var entity = await _db.Showings.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;
            _db.Showings.Remove(entity);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Showing), Action = "Delete", PerformedBy = _current.UserEmail, Details = $"Deleted showing {entity.Id}" });
            await _db.SaveChangesAsync();
        }

        public async Task AddPaymentEntryAsync(PaymentEntry paymentEntry)
        {
            EnsureCanManageData();
            paymentEntry.Id = Guid.NewGuid();
            paymentEntry.OrganizationId = _current.OrganizationId;
            paymentEntry.CreatedUtc = DateTime.UtcNow;
            _db.PaymentEntries.Add(paymentEntry);
            await RecalculateInvoiceAsync(paymentEntry.InvoiceId);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(PaymentEntry), Action = "Create", PerformedBy = _current.UserEmail, Details = $"Recorded payment {paymentEntry.Reference}" });
            await _db.SaveChangesAsync();
        }

        public async Task UpdatePaymentEntryAsync(PaymentEntry paymentEntry)
        {
            EnsureCanManageData();
            var entity = await _db.PaymentEntries.FirstOrDefaultAsync(x => x.Id == paymentEntry.Id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;

            var previousInvoiceId = entity.InvoiceId;
            entity.InvoiceId = paymentEntry.InvoiceId;
            entity.ReceivedDate = paymentEntry.ReceivedDate;
            entity.Amount = paymentEntry.Amount;
            entity.Method = paymentEntry.Method;
            entity.Reference = paymentEntry.Reference;
            entity.Notes = paymentEntry.Notes;

            await RecalculateInvoiceAsync(previousInvoiceId);
            if (paymentEntry.InvoiceId != previousInvoiceId)
            {
                await RecalculateInvoiceAsync(paymentEntry.InvoiceId);
            }

            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(PaymentEntry), Action = "Update", PerformedBy = _current.UserEmail, Details = $"Updated payment {paymentEntry.Reference}" });
            await _db.SaveChangesAsync();
        }

        public async Task DeletePaymentEntryAsync(Guid id)
        {
            EnsureCanManageData();
            var entity = await _db.PaymentEntries.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;
            var invoiceId = entity.InvoiceId;
            _db.PaymentEntries.Remove(entity);
            await RecalculateInvoiceAsync(invoiceId);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(PaymentEntry), Action = "Delete", PerformedBy = _current.UserEmail, Details = $"Deleted payment {entity.Reference}" });
            await _db.SaveChangesAsync();
        }

        public async Task AddPropertyAsync(Property property)
        {
            EnsureCanManageData();
            property.Id = Guid.NewGuid();
            property.OrganizationId = _current.OrganizationId;
            property.CreatedUtc = DateTime.UtcNow;
            _db.Properties.Add(property);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Property), Action = "Create", PerformedBy = _current.UserEmail, Details = $"Created property {property.Name}" });
            await _db.SaveChangesAsync();
        }

        public async Task UpdatePropertyAsync(Property property)
        {
            EnsureCanManageData();
            var entity = await _db.Properties.FirstOrDefaultAsync(x => x.Id == property.Id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;

            entity.Name = property.Name;
            entity.PropertyType = property.PropertyType;
            entity.AddressLine1 = property.AddressLine1;
            entity.City = property.City;
            entity.Province = property.Province;
            entity.PostalCode = property.PostalCode;
            entity.YearBuilt = property.YearBuilt;
            entity.MonthlyRevenueTarget = property.MonthlyRevenueTarget;
            entity.AmenitySummary = property.AmenitySummary;
            entity.NeighborhoodNotes = property.NeighborhoodNotes;
            entity.LeasingNotes = property.LeasingNotes;
            entity.OperationalNotes = property.OperationalNotes;

            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Property), Action = "Update", PerformedBy = _current.UserEmail, Details = $"Updated property playbook for {property.Name}" });
            await _db.SaveChangesAsync();
        }

        public async Task DeletePropertyAsync(Guid id)
        {
            EnsureCanManageData();
            var entity = await _db.Properties.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;
            _db.Properties.Remove(entity);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Property), Action = "Delete", PerformedBy = _current.UserEmail, Details = $"Deleted property {entity.Name}" });
            await _db.SaveChangesAsync();
        }

        public async Task AddTenantAsync(Tenant tenant)
        {
            EnsureCanManageData();
            tenant.Id = Guid.NewGuid();
            tenant.OrganizationId = _current.OrganizationId;
            tenant.CreatedUtc = DateTime.UtcNow;
            _db.Tenants.Add(tenant);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Tenant), Action = "Create", PerformedBy = _current.UserEmail, Details = $"Created tenant {tenant.FullName}" });
            await _db.SaveChangesAsync();
        }

        public async Task DeleteTenantAsync(Guid id)
        {
            EnsureCanManageData();
            var entity = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;
            _db.Tenants.Remove(entity);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Tenant), Action = "Delete", PerformedBy = _current.UserEmail, Details = $"Deleted tenant {entity.FullName}" });
            await _db.SaveChangesAsync();
        }

        public async Task UpdateTenantAsync(Tenant tenant)
        {
            EnsureCanManageData();
            var entity = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == tenant.Id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;

            entity.FullName = tenant.FullName;
            entity.Email = tenant.Email;
            entity.PhoneNumber = tenant.PhoneNumber;
            entity.CreditScore = tenant.CreditScore;
            entity.ScreeningCompleted = tenant.ScreeningCompleted;
            entity.ScreeningProvider = tenant.ScreeningProvider;

            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Tenant), Action = "Update", PerformedBy = _current.UserEmail, Details = $"Updated tenant {tenant.FullName}" });
            await _db.SaveChangesAsync();
        }

        public async Task<AppUser?> GetCurrentUserProfileAsync()
            => _current.UserId == Guid.Empty
                ? null
                : await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == _current.UserId);

        public async Task<List<MemberSummaryDto>> GetMembersAsync()
            => await _db.OrganizationMemberships
                .AsNoTracking()
                .Where(x => x.OrganizationId == _current.OrganizationId)
                .OrderBy(x => x.User!.FullName)
                .Select(x => new MemberSummaryDto
                {
                    MembershipId = x.Id,
                    UserId = x.UserId,
                    FullName = x.User != null && !string.IsNullOrWhiteSpace(x.User.FullName) ? x.User.FullName : x.User!.Email,
                    Email = x.User!.Email,
                    Role = x.Role,
                    Status = x.Status
                })
                .ToListAsync();

        public async Task<Organization?> CreateOrganizationAsync(string name, string province, bool loadDemoData = false, CancellationToken cancellationToken = default)
        {
            if (!_current.IsAuthenticated || _current.UserId == Guid.Empty)
            {
                throw new InvalidOperationException("You must be authenticated to create an organization.");
            }

            if (loadDemoData)
            {
                var existingDemoOrganization = await _db.OrganizationMemberships
                    .AsNoTracking()
                    .Where(x => x.UserId == _current.UserId && x.Status == "Active" && x.Role == "Owner")
                    .Select(x => x.Organization)
                    .FirstOrDefaultAsync(x => x != null && x.IsDemo && (!x.DemoExpiresUtc.HasValue || x.DemoExpiresUtc.Value >= DateTime.UtcNow), cancellationToken);

                if (existingDemoOrganization is not null)
                {
                    return existingDemoOrganization;
                }
            }

            var normalizedName = string.IsNullOrWhiteSpace(name)
                ? throw new InvalidOperationException("Organization name is required.")
                : name.Trim();

            var slug = BuildSlug(normalizedName);
            var existingSlugCount = await _db.Organizations.CountAsync(x => x.Slug.StartsWith(slug), cancellationToken);
            if (existingSlugCount > 0)
            {
                slug = $"{slug}-{existingSlugCount + 1}";
            }

            var selectedProvince = string.IsNullOrWhiteSpace(province) ? "ON" : province.Trim().ToUpperInvariant();
            var profile = JurisdictionCatalog.GetProfile(selectedProvince);

            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = normalizedName,
                Slug = slug,
                CountryCode = profile.CountryCode,
                Province = profile.ProvinceCode,
                PreferredLanguage = profile.DefaultLanguage,
                TimeZone = "America/Toronto",
                IsDemo = loadDemoData,
                DemoTemplate = loadDemoData ? profile.ProvinceCode : string.Empty,
                DemoExpiresUtc = loadDemoData ? DateTime.UtcNow.AddDays(14) : null,
                SubscriptionTier = SubscriptionTier.Trial,
                TrialEndsUtc = DateTime.UtcNow.AddDays(14),
                IsActive = true,
                CreatedUtc = DateTime.UtcNow
            };

            _db.Organizations.Add(organization);

            var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == _current.UserId, cancellationToken);
            if (user is null)
            {
                throw new InvalidOperationException("Current user profile was not found.");
            }

            user.OrganizationId = organization.Id;
            user.Role = "Owner";
            user.PreferredLanguage = profile.DefaultLanguage;

            _db.OrganizationMemberships.Add(new OrganizationMembership
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                UserId = user.Id,
                Role = "Owner",
                Status = "Active",
                CreatedUtc = DateTime.UtcNow
            });

            _db.AuditLogs.Add(new AuditLog
            {
                OrganizationId = organization.Id,
                EntityName = nameof(Organization),
                Action = loadDemoData ? "CreateDemo" : "Create",
                PerformedBy = _current.UserEmail,
                Details = loadDemoData
                    ? $"Created demo organization {organization.Name} from onboarding"
                    : $"Created organization {organization.Name} from onboarding"
            });

            if (loadDemoData)
            {
                SeedDemoWorkspace(organization, profile);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return organization;
        }

        public async Task<List<OrganizationAccessOptionDto>> GetAccessibleOrganizationsAsync()
            => await _db.OrganizationMemberships
                .AsNoTracking()
                .Where(x => x.UserId == _current.UserId && x.Status == "Active")
                .OrderBy(x => x.Organization!.Name)
                .Select(x => new OrganizationAccessOptionDto
                {
                    OrganizationId = x.OrganizationId,
                    OrganizationName = x.Organization!.Name,
                    IsDemo = x.Organization!.IsDemo,
                    Role = x.Role,
                    Status = x.Status
                })
                .ToListAsync();

        public async Task<List<InvitationSummaryDto>> GetInvitationsAsync()
        {
            await ExpirePendingInvitationsAsync(cancellationToken: default);

            return await _db.OrganizationInvitations
                .AsNoTracking()
                .Where(x => x.OrganizationId == _current.OrganizationId)
                .OrderByDescending(x => x.CreatedUtc)
                .Select(x => new InvitationSummaryDto
                {
                    InvitationId = x.Id,
                    Email = x.Email,
                    Role = x.Role,
                    Status = x.Status,
                    ExpiresUtc = x.ExpiresUtc,
                    Token = x.Token
                })
                .ToListAsync();
        }

        public async Task<List<PendingWorkspaceInvitationDto>> GetPendingWorkspaceInvitationsAsync(CancellationToken cancellationToken = default)
        {
            await ExpirePendingInvitationsAsync(cancellationToken);

            if (!_current.IsAuthenticated || string.IsNullOrWhiteSpace(_current.UserEmail))
            {
                return new List<PendingWorkspaceInvitationDto>();
            }

            var normalizedEmail = _current.UserEmail.Trim().ToLowerInvariant();

            return await _db.OrganizationInvitations
                .AsNoTracking()
                .Where(x => x.Email == normalizedEmail && x.Status == "Pending" && x.ExpiresUtc >= DateTime.UtcNow)
                .OrderBy(x => x.Organization!.Name)
                .Select(x => new PendingWorkspaceInvitationDto
                {
                    OrganizationName = x.Organization!.Name,
                    Role = x.Role,
                    ExpiresUtc = x.ExpiresUtc,
                    Token = x.Token
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<OrganizationInviteResult> InviteUserAsync(string email, string role, string invitationBaseUrl, CancellationToken cancellationToken = default)
        {
            EnsureCanManageData();
            await EnsureUserLimitAsync(cancellationToken);
            await ExpirePendingInvitationsAsync(cancellationToken);

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var normalizedRole = NormalizeInviteRole(role);
            var existingMembership = await _db.OrganizationMemberships
                .AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.OrganizationId == _current.OrganizationId && x.User != null && x.User.Email == normalizedEmail && x.Status == "Active", cancellationToken);

            if (existingMembership is not null)
            {
                throw new InvalidOperationException("This user already belongs to the organization.");
            }

            var existingInvite = await _db.OrganizationInvitations
                .FirstOrDefaultAsync(x => x.OrganizationId == _current.OrganizationId && x.Email == normalizedEmail && x.Status == "Pending", cancellationToken);

            var token = Guid.NewGuid().ToString("N");
            var invite = existingInvite ?? new OrganizationInvitation
            {
                Id = Guid.NewGuid(),
                OrganizationId = _current.OrganizationId,
                Email = normalizedEmail,
                InvitedBy = _current.UserEmail,
                CreatedUtc = DateTime.UtcNow
            };

            invite.Role = normalizedRole;
            invite.Token = token;
            invite.Status = "Pending";
            invite.ExpiresUtc = DateTime.UtcNow.AddDays(7);
            invite.AcceptedUtc = null;
            invite.RevokedUtc = null;

            if (existingInvite is null)
            {
                _db.OrganizationInvitations.Add(invite);
            }

            _db.AuditLogs.Add(new AuditLog
            {
                OrganizationId = _current.OrganizationId,
                EntityName = nameof(OrganizationInvitation),
                Action = existingInvite is null ? "Create" : "Refresh",
                PerformedBy = _current.UserEmail,
                Details = $"Invitation for {normalizedEmail} with role {normalizedRole}"
            });

            await _db.SaveChangesAsync(cancellationToken);

            var invitationUrl = BuildInvitationUrl(invitationBaseUrl, token);
            await _notifications.SendOrganizationInvitationAsync(normalizedEmail, _current.OrganizationName, _current.UserEmail, normalizedRole, invitationUrl, invite.ExpiresUtc, cancellationToken);

            return new OrganizationInviteResult
            {
                InvitationId = invite.Id,
                InvitationUrl = invitationUrl,
                ExpiresUtc = invite.ExpiresUtc
            };
        }

        public async Task<OrganizationInviteResult> ResendInvitationAsync(Guid invitationId, string invitationBaseUrl, CancellationToken cancellationToken = default)
        {
            EnsureCanManageData();

            var invitation = await _db.OrganizationInvitations.FirstOrDefaultAsync(x => x.Id == invitationId && x.OrganizationId == _current.OrganizationId, cancellationToken);
            if (invitation is null)
            {
                throw new InvalidOperationException("Invitation was not found.");
            }

            invitation.Token = Guid.NewGuid().ToString("N");
            invitation.Status = "Pending";
            invitation.ExpiresUtc = DateTime.UtcNow.AddDays(7);
            invitation.AcceptedUtc = null;
            invitation.RevokedUtc = null;

            _db.AuditLogs.Add(new AuditLog
            {
                OrganizationId = _current.OrganizationId,
                EntityName = nameof(OrganizationInvitation),
                Action = "Resend",
                PerformedBy = _current.UserEmail,
                Details = $"Resent invitation for {invitation.Email} with role {invitation.Role}"
            });

            await _db.SaveChangesAsync(cancellationToken);

            var invitationUrl = BuildInvitationUrl(invitationBaseUrl, invitation.Token);
            await _notifications.SendOrganizationInvitationAsync(invitation.Email, _current.OrganizationName, _current.UserEmail, invitation.Role, invitationUrl, invitation.ExpiresUtc, cancellationToken);

            return new OrganizationInviteResult
            {
                InvitationId = invitation.Id,
                InvitationUrl = invitationUrl,
                ExpiresUtc = invitation.ExpiresUtc
            };
        }

        public async Task<Guid> AcceptInvitationAsync(string token, string email, string clerkUserId, string fullName, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var invitation = await _db.OrganizationInvitations
                .FirstOrDefaultAsync(x => x.Token == token && x.Email == normalizedEmail && x.Status == "Pending", cancellationToken);

            if (invitation is null)
            {
                throw new InvalidOperationException("Invitation is invalid or expired.");
            }

            if (invitation.ExpiresUtc < DateTime.UtcNow)
            {
                invitation.Status = "Expired";
                await _db.SaveChangesAsync(cancellationToken);
                throw new InvalidOperationException("Invitation is invalid or expired.");
            }

            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
            if (user is null)
            {
                user = new AppUser
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = invitation.OrganizationId,
                    ClerkUserId = clerkUserId,
                    Email = normalizedEmail,
                    FullName = fullName,
                    Role = invitation.Role,
                    PreferredLanguage = "en-CA",
                    IsActive = true,
                    CreatedUtc = DateTime.UtcNow
                };
                _db.Users.Add(user);
            }
            else
            {
                user.ClerkUserId = string.IsNullOrWhiteSpace(user.ClerkUserId) ? clerkUserId : user.ClerkUserId;
                user.FullName = string.IsNullOrWhiteSpace(user.FullName) ? fullName : user.FullName;
            }

            var membership = await _db.OrganizationMemberships
                .FirstOrDefaultAsync(x => x.OrganizationId == invitation.OrganizationId && x.UserId == user.Id, cancellationToken);

            if (membership is null)
            {
                membership = new OrganizationMembership
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = invitation.OrganizationId,
                    UserId = user.Id,
                    Role = invitation.Role,
                    Status = "Active",
                    CreatedUtc = DateTime.UtcNow
                };
                _db.OrganizationMemberships.Add(membership);
            }
            else
            {
                membership.Role = invitation.Role;
                membership.Status = "Active";
            }

            user.OrganizationId = invitation.OrganizationId;
            user.Role = invitation.Role;
            invitation.Status = "Accepted";
            invitation.AcceptedUtc = DateTime.UtcNow;

            _db.AuditLogs.Add(new AuditLog
            {
                OrganizationId = invitation.OrganizationId,
                EntityName = nameof(OrganizationInvitation),
                Action = "Accept",
                PerformedBy = normalizedEmail,
                Details = $"Accepted invitation for {normalizedEmail}"
            });

            await _db.SaveChangesAsync(cancellationToken);
            return invitation.OrganizationId;
        }

        public async Task RevokeInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default)
        {
            EnsureCanManageData();
            var invitation = await _db.OrganizationInvitations.FirstOrDefaultAsync(x => x.Id == invitationId && x.OrganizationId == _current.OrganizationId, cancellationToken);
            if (invitation is null)
            {
                return;
            }

            invitation.Status = "Revoked";
            invitation.RevokedUtc = DateTime.UtcNow;

            _db.AuditLogs.Add(new AuditLog
            {
                OrganizationId = _current.OrganizationId,
                EntityName = nameof(OrganizationInvitation),
                Action = "Revoke",
                PerformedBy = _current.UserEmail,
                Details = $"Revoked invitation for {invitation.Email}"
            });

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task ExpirePendingInvitationsAsync(CancellationToken cancellationToken = default)
        {
            var expiredInvitations = await _db.OrganizationInvitations
                .Where(x => x.Status == "Pending" && x.ExpiresUtc < DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            if (expiredInvitations.Count == 0)
            {
                return;
            }

            foreach (var invitation in expiredInvitations)
            {
                invitation.Status = "Expired";
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateCurrentUserPreferredLanguageAsync(string preferredLanguage)
        {
            var entity = await _db.Users.FirstOrDefaultAsync(x => x.Id == _current.UserId);
            if (entity is null) return;

            var normalizedLanguage = string.IsNullOrWhiteSpace(preferredLanguage)
                ? _current.Jurisdiction.DefaultLanguage
                : preferredLanguage.Trim();

            if (!_current.Jurisdiction.SupportedLanguages.Contains(normalizedLanguage, StringComparer.OrdinalIgnoreCase))
            {
                normalizedLanguage = _current.Jurisdiction.DefaultLanguage;
            }

            entity.PreferredLanguage = normalizedLanguage;
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(AppUser), Action = "Update", PerformedBy = _current.UserEmail, Details = $"Updated preferred language to {normalizedLanguage}" });
            await _db.SaveChangesAsync();
        }

        public async Task ResetCurrentDemoWorkspaceAsync(CancellationToken cancellationToken = default)
        {
            EnsureCanManageData();

            var organization = await _db.Organizations.FirstOrDefaultAsync(x => x.Id == _current.OrganizationId, cancellationToken);
            if (organization is null)
            {
                throw new InvalidOperationException("Current organization was not found.");
            }

            if (!organization.IsDemo)
            {
                throw new InvalidOperationException("Only demo workspaces can be reset.");
            }

            await ClearOrganizationPortfolioAsync(organization.Id, cancellationToken);

            var profile = JurisdictionCatalog.GetProfile(string.IsNullOrWhiteSpace(organization.DemoTemplate) ? organization.Province : organization.DemoTemplate);
            organization.DemoResetAtUtc = DateTime.UtcNow;
            organization.DemoExpiresUtc ??= DateTime.UtcNow.AddDays(14);
            SeedDemoWorkspace(organization, profile);

            _db.AuditLogs.Add(new AuditLog
            {
                OrganizationId = organization.Id,
                EntityName = nameof(Organization),
                Action = "ResetDemo",
                PerformedBy = _current.UserEmail,
                Details = $"Reset demo workspace {organization.Name}"
            });

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task AddUnitAsync(Unit unit)
        {
            EnsureCanManageData();
            await EnsureUnitLimitAsync();
            unit.Id = Guid.NewGuid();
            unit.OrganizationId = _current.OrganizationId;
            unit.CreatedUtc = DateTime.UtcNow;
            _db.Units.Add(unit);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Unit), Action = "Create", PerformedBy = _current.UserEmail, Details = $"Created unit {unit.UnitNumber}" });
            await _db.SaveChangesAsync();
        }

        public async Task DeleteUnitAsync(Guid id)
        {
            EnsureCanManageData();
            var entity = await _db.Units.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;
            _db.Units.Remove(entity);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Unit), Action = "Delete", PerformedBy = _current.UserEmail, Details = $"Deleted unit {entity.UnitNumber}" });
            await _db.SaveChangesAsync();
        }

        public async Task UpdateUnitAsync(Unit unit)
        {
            EnsureCanManageData();
            var entity = await _db.Units.FirstOrDefaultAsync(x => x.Id == unit.Id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;

            entity.PropertyId = unit.PropertyId;
            entity.UnitNumber = unit.UnitNumber;
            entity.Bedrooms = unit.Bedrooms;
            entity.Bathrooms = unit.Bathrooms;
            entity.MonthlyRent = unit.MonthlyRent;
            entity.IsOccupied = unit.IsOccupied;

            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Unit), Action = "Update", PerformedBy = _current.UserEmail, Details = $"Updated unit {unit.UnitNumber}" });
            await _db.SaveChangesAsync();
        }

        public async Task AddLeaseAsync(Lease lease)
        {
            EnsureCanManageData();
            lease.Id = Guid.NewGuid();
            lease.OrganizationId = _current.OrganizationId;
            lease.CreatedUtc = DateTime.UtcNow;
            _db.Leases.Add(lease);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Lease), Action = "Create", PerformedBy = _current.UserEmail, Details = $"Created lease for unit {lease.UnitId}" });
            await _db.SaveChangesAsync();
        }

        public async Task DeleteLeaseAsync(Guid id)
        {
            EnsureCanManageData();
            var entity = await _db.Leases.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;
            _db.Leases.Remove(entity);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Lease), Action = "Delete", PerformedBy = _current.UserEmail, Details = $"Deleted lease {entity.Id}" });
            await _db.SaveChangesAsync();
        }

        public async Task UpdateLeaseAsync(Lease lease)
        {
            EnsureCanManageData();
            var entity = await _db.Leases.FirstOrDefaultAsync(x => x.Id == lease.Id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;

            entity.UnitId = lease.UnitId;
            entity.TenantId = lease.TenantId;
            entity.StartDate = lease.StartDate;
            entity.EndDate = lease.EndDate;
            entity.MonthlyRent = lease.MonthlyRent;
            entity.Status = lease.Status;
            entity.StandardOntarioLeaseSigned = lease.StandardOntarioLeaseSigned;
            entity.N1IncreaseNoticeScheduled = lease.N1IncreaseNoticeScheduled;

            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Lease), Action = "Update", PerformedBy = _current.UserEmail, Details = $"Updated lease {lease.Id}" });
            await _db.SaveChangesAsync();
        }
        public async Task AddMaintenanceAsync(MaintenanceRequest request)
        {
            EnsureCanManageData();
            request.Id = Guid.NewGuid();
            request.OrganizationId = _current.OrganizationId;
            request.CreatedUtc = DateTime.UtcNow;
            if (request.RequestedDate == default) request.RequestedDate = DateOnly.FromDateTime(DateTime.Today);
            request.DispatchStatus = string.IsNullOrWhiteSpace(request.VendorName) ? "Unassigned" : "Assigned";
            _db.MaintenanceRequests.Add(request);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(MaintenanceRequest), Action = "Create", PerformedBy = _current.UserEmail, Details = $"Created maintenance request {request.Title}" });
            await _db.SaveChangesAsync();

            var propertyName = await _db.Properties
                .Where(x => x.Id == request.PropertyId && x.OrganizationId == _current.OrganizationId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync() ?? "Unknown property";

            var unitNumber = request.UnitId is null
                ? null
                : await _db.Units
                    .Where(x => x.Id == request.UnitId.Value && x.OrganizationId == _current.OrganizationId)
                    .Select(x => x.UnitNumber)
                    .FirstOrDefaultAsync();

            var promptSummary = $"Review maintenance follow-up for {request.Title}";
            var suggestedContent = string.IsNullOrWhiteSpace(request.VendorName)
                ? $"Assign a preferred vendor for {propertyName}{(string.IsNullOrWhiteSpace(unitNumber) ? string.Empty : $" unit {unitNumber}")} and prepare a tenant update on timing."
                : $"Confirm dispatch with {request.VendorName} for {propertyName}{(string.IsNullOrWhiteSpace(unitNumber) ? string.Empty : $" unit {unitNumber}")} and send the next resident update.";

            AddAISuggestion(AISuggestionType.WorkQueue, nameof(MaintenanceRequest), request.Id, promptSummary, suggestedContent);
            await _db.SaveChangesAsync();

            await _notifications.SendMaintenanceRequestCreatedAsync(request, propertyName, unitNumber);
        }

        public async Task DeleteMaintenanceAsync(Guid id)
        {
            EnsureCanManageData();
            var entity = await _db.MaintenanceRequests.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;
            _db.MaintenanceRequests.Remove(entity);
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(MaintenanceRequest), Action = "Delete", PerformedBy = _current.UserEmail, Details = $"Deleted maintenance request {entity.Title}" });
            await _db.SaveChangesAsync();
        }

        public async Task UpdateMaintenanceAsync(MaintenanceRequest request)
        {
            EnsureCanManageData();
            var entity = await _db.MaintenanceRequests.FirstOrDefaultAsync(x => x.Id == request.Id && x.OrganizationId == _current.OrganizationId);
            if (entity is null) return;

            entity.PropertyId = request.PropertyId;
            entity.UnitId = request.UnitId;
            entity.Title = request.Title;
            entity.Description = request.Description;
            entity.Priority = request.Priority;
            entity.Status = request.Status;
            entity.DispatchStatus = string.IsNullOrWhiteSpace(request.VendorName) ? request.DispatchStatus : (string.IsNullOrWhiteSpace(request.DispatchStatus) || string.Equals(request.DispatchStatus, "Unassigned", StringComparison.OrdinalIgnoreCase) ? "Assigned" : request.DispatchStatus);
            entity.VendorName = request.VendorName;
            entity.EstimatedCost = request.EstimatedCost;
            entity.RequestedDate = request.RequestedDate;

            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(MaintenanceRequest), Action = "Update", PerformedBy = _current.UserEmail, Details = $"Updated maintenance request {request.Title}" });

            if (!string.Equals(request.Status, "Closed", StringComparison.OrdinalIgnoreCase))
            {
                AddAISuggestion(
                    AISuggestionType.TenantMessage,
                    nameof(MaintenanceRequest),
                    request.Id,
                    $"Draft a resident update for maintenance request {request.Title}",
                    string.IsNullOrWhiteSpace(request.VendorName)
                        ? "Explain that the issue is being triaged and confirm the next expected update window."
                        : $"Confirm that {request.VendorName} is scheduled and share the next access or timing details with the resident.");
            }

            await _db.SaveChangesAsync();
        }

        public async Task ImportSamplePortfolioAsync()
        {
            if (!_current.CanManageData) return;
            if (await _db.Properties.AnyAsync(x => x.OrganizationId == _current.OrganizationId)) return;
            await EnsureUnitLimitAsync();

            var property = new Property { OrganizationId = _current.OrganizationId, Name = "Lakeshore Residences", PropertyType = "Waterfront condo", AddressLine1 = "25 Queens Quay W", City = "Toronto", PostalCode = "M5J 2N6", YearBuilt = 2018, MonthlyRevenueTarget = 12500m, AmenitySummary = "Concierge, fitness room, parking waitlist", NeighborhoodNotes = "Strong waterfront demand with transit, trail and downtown access.", LeasingNotes = "Best suited for professionals seeking downtown access with premium views.", OperationalNotes = "Protect turnover speed during peak spring leasing cycle and keep concierge communication tight." };
            var unit = new Unit { OrganizationId = _current.OrganizationId, Property = property, UnitNumber = "1204", Bedrooms = 2, Bathrooms = 2, MonthlyRent = 3150m, IsOccupied = true };
            var tenant = new Tenant { OrganizationId = _current.OrganizationId, FullName = "Avery Martin", Email = "avery@example.com", PhoneNumber = "416-555-0198", CreditScore = 742, ScreeningCompleted = true, ScreeningProvider = "OpenRoom / FrontLobby" };

            _db.Properties.Add(property);
            _db.Units.Add(unit);
            _db.Tenants.Add(tenant);
            var lease = new Lease { OrganizationId = _current.OrganizationId, Unit = unit, Tenant = tenant, StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-6)), EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(6)), MonthlyRent = 3150m, Status = LeaseStatus.Active, StandardOntarioLeaseSigned = true, N1IncreaseNoticeScheduled = false };
            _db.Leases.Add(lease);
            _db.MaintenanceRequests.Add(new MaintenanceRequest { OrganizationId = _current.OrganizationId, PropertyId = property.Id, UnitId = unit.Id, Title = "HVAC spring inspection", Description = "Preventive maintenance before cooling season.", Priority = MaintenancePriority.Medium, Status = "Scheduled", VendorName = "Toronto HVAC Collective", EstimatedCost = 240m, RequestedDate = DateOnly.FromDateTime(DateTime.Today) });
            _db.Vendors.Add(new Vendor { OrganizationId = _current.OrganizationId, Name = "Toronto HVAC Collective", Trade = "HVAC", Email = "dispatch@torontohvac.example", PhoneNumber = "416-555-0110", ServiceArea = "Downtown Toronto", IsPreferred = true, IsActive = true, Notes = "Demo preferred HVAC vendor", CreatedUtc = DateTime.UtcNow });
            var listing = new Listing { OrganizationId = _current.OrganizationId, Property = property, Unit = unit, Title = "Waterfront two-bedroom with concierge amenities", Description = "Simulated AI listing draft for a downtown waterfront suite with strong transit access.", AskingRent = 3150m, Status = ListingStatus.ReadyToPublish, PublishTargets = "Rentals.ca, Kijiji, Facebook Marketplace", CreatedUtc = DateTime.UtcNow };
            _db.Listings.Add(listing);
            var lead = new Lead { OrganizationId = _current.OrganizationId, Listing = listing, FullName = "Sofia Reynolds", Email = "sofia.reynolds@example.com", PhoneNumber = "647-555-0107", Source = "Rentals.ca", Status = LeadStatus.ShowingScheduled, Notes = "Interested in a June move-in.", CreatedUtc = DateTime.UtcNow };
            _db.Leads.Add(lead);
            _db.Showings.Add(new Showing { OrganizationId = _current.OrganizationId, Listing = listing, Lead = lead, ScheduledUtc = DateTime.UtcNow.AddDays(2), Status = "Confirmed", Notes = "Saturday morning showing", CreatedUtc = DateTime.UtcNow });
            var invoice = new Invoice { OrganizationId = _current.OrganizationId, Lease = lease, Number = "INV-1001", DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)), Amount = 3150m, Balance = 1575m, Status = PaymentStatus.PartiallyPaid, Notes = "Initial light invoicing sample", CreatedUtc = DateTime.UtcNow };
            _db.Invoices.Add(invoice);
            _db.PaymentEntries.Add(new PaymentEntry { OrganizationId = _current.OrganizationId, Invoice = invoice, ReceivedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), Amount = 1575m, Method = "E-Transfer", Reference = "ET-2048", Notes = "Partial rent sample", CreatedUtc = DateTime.UtcNow });
            _db.MediaAssets.Add(new MediaAsset { OrganizationId = _current.OrganizationId, Property = property, Unit = unit, FileName = "lakeshore-cover.jpg", BlobPath = "demo/media/lakeshore-cover.jpg", Caption = "Primary listing cover photo", SortOrder = 1, IsPrimary = true, Category = MediaAssetCategory.UnitPhoto, CreatedUtc = DateTime.UtcNow });
            var conversation = new TenantConversation { OrganizationId = _current.OrganizationId, Tenant = tenant, Lease = lease, Subject = "Rent reminder and payment plan check-in", Channel = ConversationChannel.Email, Status = "Awaiting reply", LastContactUtc = DateTime.UtcNow.AddDays(-1), CreatedUtc = DateTime.UtcNow };
            _db.TenantConversations.Add(conversation);
            _db.TenantMessages.Add(new TenantMessage { OrganizationId = _current.OrganizationId, Conversation = conversation, IsIncoming = false, Body = "Hi Avery, this is a friendly reminder that the remaining rent balance is still outstanding. Please confirm when the transfer will be completed.", SentBy = _current.UserEmail, SentUtc = DateTime.UtcNow.AddDays(-1), IsAISuggested = true, CreatedUtc = DateTime.UtcNow });
            _db.AISuggestionLogs.Add(new AISuggestionLog { OrganizationId = _current.OrganizationId, SuggestionType = AISuggestionType.ListingDescription, SourceEntityName = nameof(Unit), PromptSummary = "Generate listing copy for the next downtown waterfront vacancy.", SuggestedContent = "Promote transit access, concierge amenities and premium views for professional renters.", ReviewedByHuman = false, ReviewOutcome = string.Empty, CreatedUtc = DateTime.UtcNow });
            _db.AISuggestionLogs.Add(new AISuggestionLog { OrganizationId = _current.OrganizationId, SuggestionType = AISuggestionType.WorkQueue, SourceEntityName = nameof(MaintenanceRequest), PromptSummary = "Prioritize the next operational actions across leasing, maintenance and rent follow-up.", SuggestedContent = "Review the scheduled HVAC inspection, publish the waterfront listing and prepare a rent reminder workflow before the next due date.", ReviewedByHuman = false, ReviewOutcome = string.Empty, CreatedUtc = DateTime.UtcNow });
            _db.AuditLogs.Add(new AuditLog { OrganizationId = _current.OrganizationId, EntityName = nameof(Property), Action = "Import", PerformedBy = _current.UserEmail, Details = "Imported starter portfolio data" });

            await _db.SaveChangesAsync();
        }

        public async Task<List<SuperAdminOrganizationDto>> GetSuperAdminOrganizationsAsync(CancellationToken cancellationToken = default)
        {
            if (!_current.IsSuperAdmin)
            {
                throw new InvalidOperationException("Current user is not a super admin.");
            }

            return await _db.Organizations
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new SuperAdminOrganizationDto
                {
                    OrganizationId = x.Id,
                    OrganizationName = x.Name,
                    IsDemo = x.IsDemo,
                    SubscriptionTier = x.SubscriptionTier.ToString(),
                    Units = _db.Units.Count(unit => unit.OrganizationId == x.Id),
                    Users = _db.OrganizationMemberships.Count(membership => membership.OrganizationId == x.Id && membership.Status == "Active")
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<SupportSessionDto> GrantSupportAccessAsync(Guid organizationId, string reason, CancellationToken cancellationToken = default)
        {
            if (!_current.IsSuperAdmin)
            {
                throw new InvalidOperationException("Current user is not a super admin.");
            }

            var normalizedReason = string.IsNullOrWhiteSpace(reason) ? "Support review" : reason.Trim();
            var organization = await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken)
                ?? throw new InvalidOperationException("Organization not found.");
            var adminUser = await _db.Users.FirstOrDefaultAsync(x => x.Id == _current.UserId, cancellationToken)
                ?? throw new InvalidOperationException("Current user profile was not found.");

            var membership = await _db.OrganizationMemberships.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == adminUser.Id, cancellationToken);
            if (membership is null)
            {
                membership = new OrganizationMembership
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    UserId = adminUser.Id,
                    Role = SupportRole,
                    Status = "Active",
                    CreatedUtc = DateTime.UtcNow
                };
                _db.OrganizationMemberships.Add(membership);
            }
            else
            {
                membership.Role = SupportRole;
                membership.Status = "Active";
            }

            adminUser.OrganizationId = organizationId;
            adminUser.Role = SupportRole;

            _db.AuditLogs.Add(new AuditLog
            {
                OrganizationId = organizationId,
                EntityName = "SupportAccess",
                Action = "Grant",
                PerformedBy = _current.UserEmail,
                Details = normalizedReason
            });

            await _db.SaveChangesAsync(cancellationToken);

            return new SupportSessionDto
            {
                OrganizationId = organizationId,
                OrganizationName = organization.Name,
                UserEmail = _current.UserEmail,
                Reason = normalizedReason,
                ExpiresUtc = DateTime.UtcNow.AddHours(2)
            };
        }

        private static string NormalizeInviteRole(string role)
            => role?.Trim() switch
            {
                "Manager" => "Manager",
                "Agent" => "Agent",
                "Viewer" => "Viewer",
                _ => "Viewer"
            };

        private void SeedDemoWorkspace(Organization organization, JurisdictionProfile profile)
        {
            var property = new Property
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Name = profile.ProvinceCode == "QC" ? "Résidences du Vieux-Port" : "Harbour View Residences",
                PropertyType = "Mid-rise rental",
                AddressLine1 = profile.ProvinceCode == "QC" ? "245 Rue de la Commune O" : "18 Queens Quay E",
                City = profile.ProvinceCode == "QC" ? "Montréal" : "Toronto",
                Province = profile.ProvinceCode,
                PostalCode = profile.ProvinceCode == "QC" ? "H2Y 2C6" : "M5E 1B3",
                YearBuilt = 2019,
                MonthlyRevenueTarget = 18450m,
                AmenitySummary = "Fitness room, rooftop lounge, bike storage",
                NeighborhoodNotes = "Transit-friendly urban location with strong renter demand.",
                LeasingNotes = "Use the demo workflow to show readiness, notices and turnover discipline.",
                OperationalNotes = "Sample workspace seeded for onboarding and sales demos.",
                CreatedUtc = DateTime.UtcNow
            };

            var unit = new Unit
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                PropertyId = property.Id,
                UnitNumber = "804",
                Bedrooms = 2,
                Bathrooms = 2,
                MonthlyRent = 3075m,
                IsOccupied = true,
                CreatedUtc = DateTime.UtcNow
            };

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                FullName = "Avery Martin",
                Email = "avery.martin@example.com",
                PhoneNumber = "416-555-0144",
                CreditScore = 736,
                ScreeningCompleted = true,
                ScreeningProvider = "SingleKey",
                CreatedUtc = DateTime.UtcNow
            };

            _db.Properties.Add(property);
            _db.Units.Add(unit);
            _db.Tenants.Add(tenant);
            var lease = new Lease
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                UnitId = unit.Id,
                TenantId = tenant.Id,
                StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-4)),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(8)),
                MonthlyRent = unit.MonthlyRent,
                Status = LeaseStatus.Active,
                StandardOntarioLeaseSigned = profile.ProvinceCode == "ON",
                N1IncreaseNoticeScheduled = profile.ProvinceCode == "ON",
                CreatedUtc = DateTime.UtcNow
            };
            _db.Leases.Add(lease);
            _db.MaintenanceRequests.Add(new MaintenanceRequest
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                PropertyId = property.Id,
                UnitId = unit.Id,
                Title = "Seasonal HVAC inspection",
                Description = "Demo maintenance workflow showing vendor coordination and planning.",
                Priority = MaintenancePriority.Medium,
                Status = "Scheduled",
                VendorName = "North Shore Mechanical",
                EstimatedCost = 240m,
                RequestedDate = DateOnly.FromDateTime(DateTime.Today),
                CreatedUtc = DateTime.UtcNow
            });
            _db.ComplianceReminders.Add(new ComplianceReminder
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Title = $"{profile.ProvinceDisplayName} notice review",
                NoticeType = profile.NoticeTypes.FirstOrDefault() ?? "General",
                Province = profile.ProvinceCode,
                DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(21)),
                IsCompleted = false,
                Reference = "Seeded demo reminder for onboarding.",
                CreatedUtc = DateTime.UtcNow
            });
            _db.DocumentTemplates.Add(new DocumentTemplate
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Name = profile.LeasePackageLabel,
                Category = "Lease",
                Province = profile.ProvinceCode,
                Description = "Seeded demo template to showcase jurisdiction-ready workflows.",
                CreatedUtc = DateTime.UtcNow
            });
            _db.Vendors.Add(new Vendor
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Name = profile.ProvinceCode == "QC" ? "Services Mécaniques du Port" : "North Shore Mechanical",
                Trade = "HVAC",
                Email = "dispatch@example.com",
                PhoneNumber = "416-555-0175",
                ServiceArea = profile.ProvinceDisplayName,
                IsPreferred = true,
                IsActive = true,
                Notes = "Demo preferred vendor for maintenance coordination.",
                CreatedUtc = DateTime.UtcNow
            });
            var listing = new Listing
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                PropertyId = property.Id,
                UnitId = unit.Id,
                Title = profile.ProvinceCode == "QC" ? "Appartement urbain prêt à louer" : "Urban rental suite ready to market",
                Description = "Simulated AI listing draft seeded for the leasing workflow demo.",
                AskingRent = unit.MonthlyRent,
                Status = ListingStatus.ReadyToPublish,
                PublishTargets = "Rentals.ca, Zumper, Facebook Marketplace",
                CreatedUtc = DateTime.UtcNow
            };
            _db.Listings.Add(listing);
            var lead = new Lead
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                ListingId = listing.Id,
                FullName = "Taylor Brooks",
                Email = "taylor.brooks@example.com",
                PhoneNumber = "416-555-0160",
                Source = "Zumper",
                Status = LeadStatus.ShowingScheduled,
                Notes = "Seeded prospect for the leasing demo.",
                CreatedUtc = DateTime.UtcNow
            };
            _db.Leads.Add(lead);
            _db.Showings.Add(new Showing
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                ListingId = listing.Id,
                LeadId = lead.Id,
                ScheduledUtc = DateTime.UtcNow.AddDays(1),
                Status = "Confirmed",
                Notes = "Demo weekend showing.",
                CreatedUtc = DateTime.UtcNow
            });
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                LeaseId = lease.Id,
                Number = "DEMO-INV-001",
                DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
                Amount = unit.MonthlyRent,
                Balance = Math.Round(unit.MonthlyRent / 2m, 2),
                Status = PaymentStatus.PartiallyPaid,
                Notes = "Demo invoice for light rent operations.",
                CreatedUtc = DateTime.UtcNow
            };
            _db.Invoices.Add(invoice);
            _db.PaymentEntries.Add(new PaymentEntry
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                InvoiceId = invoice.Id,
                ReceivedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                Amount = Math.Round(unit.MonthlyRent / 2m, 2),
                Method = "PAD",
                Reference = "DEMO-PAY-001",
                Notes = "Seeded partial payment for collections demo.",
                CreatedUtc = DateTime.UtcNow
            });
            _db.MediaAssets.Add(new MediaAsset
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                PropertyId = property.Id,
                UnitId = unit.Id,
                FileName = "demo-cover-photo.jpg",
                BlobPath = "demo/media/demo-cover-photo.jpg",
                Caption = "Primary cover photo for the simulated listing.",
                SortOrder = 1,
                IsPrimary = true,
                Category = MediaAssetCategory.UnitPhoto,
                CreatedUtc = DateTime.UtcNow
            });
            var conversation = new TenantConversation
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                TenantId = tenant.Id,
                LeaseId = lease.Id,
                Subject = profile.ProvinceCode == "QC" ? "Suivi de paiement et prochaine étape" : "Rent reminder and next-step follow-up",
                Channel = ConversationChannel.Email,
                Status = "Awaiting reply",
                LastContactUtc = DateTime.UtcNow.AddDays(-1),
                CreatedUtc = DateTime.UtcNow
            };
            _db.TenantConversations.Add(conversation);
            _db.TenantMessages.Add(new TenantMessage
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                TenantConversationId = conversation.Id,
                IsIncoming = false,
                Body = profile.ProvinceCode == "QC"
                    ? "Bonjour Avery, petit rappel concernant le solde restant. Merci de confirmer votre date de paiement prévue."
                    : "Hi Avery, this is a friendly reminder about the remaining balance. Please confirm your expected payment date.",
                SentBy = "system-demo",
                SentUtc = DateTime.UtcNow.AddDays(-1),
                IsAISuggested = true,
                CreatedUtc = DateTime.UtcNow
            });
            _db.AISuggestionLogs.Add(new AISuggestionLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                SuggestionType = AISuggestionType.WorkQueue,
                SourceEntityName = nameof(Listing),
                SourceEntityId = property.Id,
                PromptSummary = "Prioritize the next leasing and compliance actions for the demo workspace.",
                SuggestedContent = "Publish the ready listing, confirm the next showing and review the Ontario notice reminder.",
                ReviewedByHuman = false,
                ReviewOutcome = string.Empty,
                CreatedUtc = DateTime.UtcNow
            });
        }

        private async Task ClearOrganizationPortfolioAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            await _db.TenantMessages.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
            await _db.TenantConversations.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
            await _db.AISuggestionLogs.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
            await _db.AuditLogs.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
            await _db.ComplianceReminders.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
            await _db.DocumentTemplates.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
            await _db.MediaAssets.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
            await _db.PaymentEntries.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
            await _db.Invoices.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
            await _db.Showings.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
            await _db.Leads.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
            await _db.Listings.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
            await _db.Vendors.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
            await _db.MaintenanceRequests.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
            await _db.Leases.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
            await _db.Tenants.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
            await _db.Units.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
            await _db.Properties.Where(x => x.OrganizationId == organizationId).ExecuteDeleteAsync(cancellationToken);
        }

        private static string BuildSlug(string value)
        {
            var chars = value
                .ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
                .ToArray();

            var slug = new string(chars);
            while (slug.Contains("--", StringComparison.Ordinal))
            {
                slug = slug.Replace("--", "-", StringComparison.Ordinal);
            }

            return slug.Trim('-');
        }

        private static string BuildInvitationUrl(string invitationBaseUrl, string token)
            => $"{invitationBaseUrl.TrimEnd('/')}/invitations/accept?token={Uri.EscapeDataString(token)}";
    }
}

namespace PropertySaaS.Application.Dashboard
{
    using PropertySaaS.Application.Common;
    using PropertySaaS.Application.Features;

    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddTransient<SaasDataService>();
            services.AddScoped(_ => new CurrentOrganization
            {
                UserId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                OrganizationName = "Maple Leaf Property Group",
                UserEmail = "owner@mapleleafpm.ca",
                Role = "Owner",
                Province = "ON",
                CountryCode = "CA",
                PreferredLanguage = "en-CA"
            });
            return services;
        }
    }
}












