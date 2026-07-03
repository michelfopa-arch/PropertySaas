using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Runtira.Domain.Entities;

namespace Runtira.Application.Abstractions
{
    public interface IApplicationDbContext
    {
        DbSet<RuntiraOrganization> RuntiraOrganizations { get; }
        DbSet<RuntiraUser> RuntiraUsers { get; }
        DbSet<RuntiraMembership> RuntiraMemberships { get; }
        DbSet<RuntiraAsset> RuntiraAssets { get; }
        DbSet<RuntiraUnit> RuntiraUnits { get; }
        DbSet<RuntiraResident> RuntiraResidents { get; }
        DbSet<RuntiraLease> RuntiraLeases { get; }
        DbSet<RuntiraLead> RuntiraLeads { get; }
        DbSet<RuntiraConversation> RuntiraConversations { get; }
        DbSet<RuntiraMessage> RuntiraMessages { get; }
        DbSet<RuntiraInboxMessage> RuntiraInboxMessages { get; }
        DbSet<RuntiraAttachment> RuntiraAttachments { get; }
        DbSet<RuntiraWorkflowTemplate> RuntiraWorkflowTemplates { get; }
        DbSet<RuntiraBlobArchive> RuntiraBlobArchives { get; }
        DbSet<RuntiraJurisdictionProfile> RuntiraJurisdictionProfiles { get; }
        DbSet<RuntiraQuotaPolicy> RuntiraQuotaPolicies { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    public interface IApplicationDbContextLease : IDisposable
    {
        IApplicationDbContext DbContext { get; }
    }

    public interface IApplicationDbContextFactory
    {
        IApplicationDbContextLease CreateDbContext();
    }

    public interface ITenantContextAccessor
    {
        Guid? TenantId { get; }
        bool BypassTenantFilter { get; }
    }

    public interface ILegislationCatalog
    {
        Runtira.Application.Features.RuntiraLegislationProfileDto? GetProfile(string countryCode, string regionCode);
    }

    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string html, string text, CancellationToken cancellationToken = default);
    }
}

namespace Runtira.Application.Common
{
    using Runtira.Application.Abstractions;

    public static class RuntiraJson
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static string Serialize<T>(T value)
            => JsonSerializer.Serialize(value, Options);

        public static T? Deserialize<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json, Options);
        }

        public static Dictionary<string, string> ReadStringDictionary(string? json)
            => Deserialize<Dictionary<string, string>>(json)
               ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class TenantContext : ITenantContextAccessor
    {
        public Guid? TenantId { get; init; }
        public bool BypassTenantFilter { get; init; }
        public string TenantSlug { get; init; } = string.Empty;
        public string UserEmail { get; init; } = string.Empty;
    }

    public class JurisdictionProfile
    {
        public string CountryCode { get; init; } = "CA";
        public string ProvinceCode { get; init; } = "ON";
        public string ProvinceDisplayName { get; init; } = "Ontario";
        public string DefaultLanguage { get; init; } = "en-CA";
        public IReadOnlyList<string> SupportedLanguages { get; init; } = new[] { "en-CA" };
    }

    public static class JurisdictionCatalog
    {
        private static readonly Dictionary<string, JurisdictionProfile> Profiles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ON"] = new() { ProvinceCode = "ON", ProvinceDisplayName = "Ontario", DefaultLanguage = "en-CA", SupportedLanguages = new[] { "en-CA", "fr-CA", "es-MX" } },
            ["QC"] = new() { ProvinceCode = "QC", ProvinceDisplayName = "Québec", DefaultLanguage = "fr-CA", SupportedLanguages = new[] { "fr-CA", "en-CA", "es-MX" } },
            ["AB"] = new() { ProvinceCode = "AB", ProvinceDisplayName = "Alberta", DefaultLanguage = "en-CA", SupportedLanguages = new[] { "en-CA", "fr-CA", "es-MX" } }
        };

        public static JurisdictionProfile GetProfile(string? province)
            => Profiles.TryGetValue(province ?? string.Empty, out var profile) ? profile : Profiles["ON"];
    }

    public class CurrentOrganization
    {
        public Guid UserId { get; set; }
        public Guid OrganizationId { get; set; }
        public int AccessibleOrganizationCount { get; set; }
        public bool HasSuperAdminOrganizationSelection { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string OrganizationSlug { get; set; } = string.Empty;
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
        public bool RequiresOrganizationSelection => !HasOrganizationAccess && (AccessibleOrganizationCount > 1 || HasSuperAdminOrganizationSelection);
        public bool IsSupervisor => string.Equals(SystemRole, "Supervisor", StringComparison.OrdinalIgnoreCase);
        public bool CanManageData => Role is "Owner" or "Manager" or "SuperAdmin" or "Supervisor" || IsSupervisor;
        public bool IsSuperAdmin => string.Equals(SystemRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        public JurisdictionProfile Jurisdiction => JurisdictionCatalog.GetProfile(Province);
    }

    public sealed class OrganizationAccessOptionDto
    {
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public bool IsDemo { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

namespace Runtira.Application.Features
{
    using Microsoft.EntityFrameworkCore;
    using Runtira.Application.Abstractions;
    using Runtira.Application.Common;

    public sealed class RuntiraWorkspaceSummaryDto
    {
        public Guid TenantId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string OrganizationSlug { get; set; } = string.Empty;
        public string DefaultLocale { get; set; } = string.Empty;
        public string BillingPlan { get; set; } = "Trial";
        public int AssetCount { get; set; }
        public int ConversationCount { get; set; }
        public int WorkflowTemplateCount { get; set; }
        public int ArchiveCount { get; set; }
        public int MonthlyAiLimit { get; set; }
        public int AssetLimit { get; set; }
    }

    public sealed class RuntiraQuestionPromptDto
    {
        public string Intent { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string RegionCode { get; set; } = string.Empty;
        public string RequiredQuestionsJson { get; set; } = "[]";
        public string ValidationRulesJson { get; set; } = "{}";
    }

    public sealed class RuntiraInvoiceComposerDto
    {
        public Guid TenantId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string OrganizationSlug { get; set; } = string.Empty;
        public string CountryCode { get; set; } = "CA";
        public string RegionCode { get; set; } = "AB";
        public string JurisdictionDisplayName { get; set; } = string.Empty;
        public string SupportedLanguagesJson { get; set; } = "[]";
        public string PropertyAddress { get; set; } = string.Empty;
        public string BillingPeriod { get; set; } = string.Empty;
        public decimal MonthlyRent { get; set; }
        public bool AddAutomaticGst { get; set; }
        public bool GeneratePdf { get; set; }
        public string RequiredQuestionsJson { get; set; } = "[]";
        public string InvoiceRulesJson { get; set; } = "{}";
        public string SuggestedPrompt { get; set; } = string.Empty;
    }

    public sealed class RuntiraLegislationProfileDto
    {
        public string JurisdictionCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string RegionCode { get; set; } = string.Empty;
        public string SupportedLanguagesJson { get; set; } = "[]";
        public string RequiredQuestionsJson { get; set; } = "[]";
        public string ValidationRulesJson { get; set; } = "{}";
        public string InvoiceRulesJson { get; set; } = "{}";
        public string AssetRulesJson { get; set; } = "{}";
        public string MaintenanceRulesJson { get; set; } = "{}";
        public bool GeneratePdf { get; set; }
        public bool AddAutomaticSalesTax { get; set; }
    }

    public sealed class RuntiraLegislationExperienceDto
    {
        public string JurisdictionCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string RegionCode { get; set; } = string.Empty;
        public string PreferredLanguage { get; set; } = "en-CA";
        public IReadOnlyList<string> SupportedLanguages { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> RequiredQuestions { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> VisibleInvoiceOptions { get; set; } = Array.Empty<string>();
    }

    public sealed class RuntiraLeadSummaryDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string PreferredLanguage { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public int QualificationScore { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string ContextDataJson { get; set; } = "{}";
        public RuntiraLeadContextData? ContextData { get; set; }
    }

    public sealed class RuntiraLeadConversionCandidateDto
    {
        public Guid LeadId { get; set; }
        public string LeadName { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string PreferredLanguage { get; set; } = string.Empty;
        public int QualificationScore { get; set; }
        public string SuggestedUnitCode { get; set; } = string.Empty;
        public decimal SuggestedRent { get; set; }
        public string ResidentName { get; set; } = string.Empty;
        public string NextAction { get; set; } = string.Empty;
    }

    public sealed class RuntiraLeadConversionResultDto
    {
        public bool Success { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string ResidentName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public string LeaseStatus { get; set; } = string.Empty;
    }

    public sealed class RuntiraLeadFormFieldDto
    {
        public string Key { get; set; } = string.Empty;
        public bool Required { get; set; }
        public string SuggestedValue { get; set; } = string.Empty;
    }

    public sealed class RuntiraLeadAssetOptionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public sealed class RuntiraLeadFormContextDto
    {
        public string JurisdictionCode { get; set; } = string.Empty;
        public string JurisdictionDisplayName { get; set; } = string.Empty;
        public string PreferredLanguage { get; set; } = string.Empty;
        public IReadOnlyList<string> SupportedLanguages { get; set; } = Array.Empty<string>();
        public IReadOnlyList<RuntiraLeadFormFieldDto> Fields { get; set; } = Array.Empty<RuntiraLeadFormFieldDto>();
        public IReadOnlyList<RuntiraLeadAssetOptionDto> Assets { get; set; } = Array.Empty<RuntiraLeadAssetOptionDto>();
        public string AssetRulesJson { get; set; } = "{}";
    }

    public sealed class RuntiraCreateLeadRequestDto
    {
        public Guid? AssetId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PreferredLanguage { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public Dictionary<string, string> DynamicFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class RuntiraCreateLeadResultDto
    {
        public bool Success { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string FieldKey { get; set; } = string.Empty;
    }

    public sealed class RuntiraLeaseConversionFormContextDto
    {
        public Guid LeadId { get; set; }
        public string LeadName { get; set; } = string.Empty;
        public string JurisdictionDisplayName { get; set; } = string.Empty;
        public IReadOnlyList<RuntiraLeadFormFieldDto> Fields { get; set; } = Array.Empty<RuntiraLeadFormFieldDto>();
    }

    public sealed class RuntiraImportValidationContextDto
    {
        public string SourceName { get; set; } = string.Empty;
        public string JurisdictionDisplayName { get; set; } = string.Empty;
        public IReadOnlyList<RuntiraLeadFormFieldDto> Fields { get; set; } = Array.Empty<RuntiraLeadFormFieldDto>();
    }

    public sealed class RuntiraImportApprovalRequestDto
    {
        public string SourceName { get; set; } = string.Empty;
        public Dictionary<string, string> DynamicFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class RuntiraImportApprovalResultDto
    {
        public bool Success { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string FieldKey { get; set; } = string.Empty;
    }

    public sealed class RuntiraInboxMessageDto
    {
        public Guid Id { get; set; }
        public string FromEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string PreviewText { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime ReceivedUtc { get; set; }
        public bool HasAttachments { get; set; }
    }

    public sealed class RuntiraUnitSummaryDto
    {
        public Guid Id { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public string UnitType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal MarketRent { get; set; }
        public bool CanDelete { get; set; }
    }

    public sealed class RuntiraLeadActionResultDto
    {
        public bool Success { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
    }

    public sealed class RuntiraUnitActionResultDto
    {
        public bool Success { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public sealed class RuntiraResidentActionResultDto
    {
        public bool Success { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string ResidentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public sealed class RuntiraLeaseActionResultDto
    {
        public bool Success { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public string LeaseStatus { get; set; } = string.Empty;
    }

    public sealed class RuntiraLeaseSummaryDto
    {
        public Guid Id { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public string ResidentName { get; set; } = string.Empty;
        public decimal MonthlyRent { get; set; }
        public string Status { get; set; } = string.Empty;
        public string BillingPeriod { get; set; } = string.Empty;
        public string ComplianceDataJson { get; set; } = "{}";
        public RuntiraLeaseComplianceData? ComplianceData { get; set; }
    }

    public sealed class RuntiraResidentSummaryDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PreferredLanguage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ProfileDataJson { get; set; } = "{}";
        public RuntiraResidentProfileData? ProfileData { get; set; }
    }

    public sealed class RuntiraAssetWorkspaceDto
    {
        public Guid AssetId { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string AssetAddress { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public int UnitCount { get; set; }
        public int TotalResidentCount { get; set; }
        public int TotalLeaseCount { get; set; }
        public string ContextDataJson { get; set; } = "{}";
        public RuntiraAssetContextData? ContextData { get; set; }
        public IReadOnlyList<RuntiraUnitSummaryDto> Units { get; set; } = Array.Empty<RuntiraUnitSummaryDto>();
        public IReadOnlyList<RuntiraLeaseSummaryDto> Leases { get; set; } = Array.Empty<RuntiraLeaseSummaryDto>();
        public IReadOnlyList<RuntiraResidentSummaryDto> Residents { get; set; } = Array.Empty<RuntiraResidentSummaryDto>();
    }

    public sealed class RuntiraImportSourceDto
    {
        public string SourceName { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int SuggestedRecordCount { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    public sealed class RuntiraImportFieldSuggestionDto
    {
        public string FieldName { get; set; } = string.Empty;
        public string SuggestedValue { get; set; } = string.Empty;
        public int ConfidenceScore { get; set; }
    }

    public sealed class RuntiraImportWorkspaceDto
    {
        public string ActiveRegion { get; set; } = string.Empty;
        public string ActiveLanguage { get; set; } = string.Empty;
        public IReadOnlyList<string> SupportedFormats { get; set; } = Array.Empty<string>();
        public IReadOnlyList<RuntiraImportSourceDto> Sources { get; set; } = Array.Empty<RuntiraImportSourceDto>();
        public IReadOnlyList<RuntiraImportFieldSuggestionDto> SuggestedFields { get; set; } = Array.Empty<RuntiraImportFieldSuggestionDto>();
    }

    public sealed class RuntiraExportOptionDto
    {
        public string Format { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }

    public sealed class RuntiraExportWorkspaceDto
    {
        public string ActiveRegion { get; set; } = string.Empty;
        public string ActiveLanguage { get; set; } = string.Empty;
        public IReadOnlyList<RuntiraExportOptionDto> Options { get; set; } = Array.Empty<RuntiraExportOptionDto>();
        public IReadOnlyList<string> SupportedDestinations { get; set; } = Array.Empty<string>();
    }

    public sealed class RuntiraExportFileDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "text/csv";
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }

    public sealed class RuntiraFlexibleDataStrategyDto
    {
        public string AssetContextDataJson { get; set; } = "{}";
        public string LeadContextDataJson { get; set; } = "{}";
        public string LeaseComplianceDataJson { get; set; } = "{}";
        public string ResidentProfileDataJson { get; set; } = "{}";
    }

    public sealed class RuntiraAssetContextData
    {
        public string Source { get; set; } = string.Empty;
        public string Market { get; set; } = string.Empty;
        public bool SupportsMultiUnit { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string AssetAddress { get; set; } = string.Empty;
    }

    public sealed class RuntiraLeadContextData
    {
        public string Source { get; set; } = string.Empty;
        public string Market { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public Dictionary<string, string> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class RuntiraLeaseComplianceData
    {
        public string Source { get; set; } = string.Empty;
        public string Market { get; set; } = string.Empty;
        public Dictionary<string, string> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class RuntiraResidentProfileData
    {
        public string Source { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public Dictionary<string, string> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class RuntiraWorkspaceService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly CurrentOrganization _currentOrganization;
        private readonly ITenantContextAccessor _tenantContextAccessor;

        public RuntiraWorkspaceService(IServiceProvider serviceProvider, CurrentOrganization currentOrganization, ITenantContextAccessor tenantContextAccessor)
        {
            _serviceProvider = serviceProvider;
            _currentOrganization = currentOrganization;
            _tenantContextAccessor = tenantContextAccessor;
        }

        public async Task<RuntiraWorkspaceSummaryDto?> GetWorkspaceSummaryAsync(CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            if (dbLease is null)
            {
                return null;
            }

            var db = dbLease.DbContext;

            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return null;
            }

            var organization = await db.RuntiraOrganizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tenantId.Value, cancellationToken);
            if (organization is null)
            {
                return null;
            }

            var quota = await db.RuntiraQuotaPolicies.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tenantId.Value, cancellationToken);

            return new RuntiraWorkspaceSummaryDto
            {
                TenantId = organization.Id,
                OrganizationName = organization.Name,
                OrganizationSlug = organization.Slug,
                DefaultLocale = organization.DefaultLocale,
                BillingPlan = string.IsNullOrWhiteSpace(organization.BillingPlan) ? "Trial" : organization.BillingPlan,
                AssetCount = await db.RuntiraAssets.CountAsync(x => x.TenantId == tenantId.Value, cancellationToken),
                ConversationCount = await db.RuntiraConversations.CountAsync(x => x.TenantId == tenantId.Value, cancellationToken),
                WorkflowTemplateCount = await db.RuntiraWorkflowTemplates.CountAsync(x => x.TenantId == tenantId.Value, cancellationToken),
                ArchiveCount = await db.RuntiraBlobArchives.CountAsync(x => x.TenantId == tenantId.Value, cancellationToken),
                MonthlyAiLimit = quota?.MaxMonthlyAiRequests ?? 0,
                AssetLimit = quota?.MaxAssets ?? 0
            };
        }

        public async Task<RuntiraInvoiceComposerDto?> GetInvoiceComposerAsync(CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            if (dbLease is null)
            {
                return null;
            }

            var db = dbLease.DbContext;

            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return null;
            }

            var organization = await db.RuntiraOrganizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tenantId.Value, cancellationToken);
            if (organization is null)
            {
                return null;
            }

            var asset = await db.RuntiraAssets.AsNoTracking().OrderBy(x => x.Name).FirstOrDefaultAsync(x => x.TenantId == tenantId.Value, cancellationToken);
            var jurisdiction = await db.RuntiraJurisdictionProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tenantId.Value, cancellationToken);
            var legislationCatalog = _serviceProvider.GetService<ILegislationCatalog>();
            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? organization.CountryCode : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? organization.RegionCode : _currentOrganization.Province;
            var preferredLanguage = string.IsNullOrWhiteSpace(_currentOrganization.PreferredLanguage) ? organization.DefaultLocale : _currentOrganization.PreferredLanguage;
            var legislationProfile = legislationCatalog?.GetProfile(countryCode, regionCode);

            return new RuntiraInvoiceComposerDto
            {
                TenantId = organization.Id,
                OrganizationName = organization.Name,
                OrganizationSlug = organization.Slug,
                CountryCode = string.IsNullOrWhiteSpace(countryCode) ? "CA" : countryCode,
                RegionCode = string.IsNullOrWhiteSpace(regionCode) ? "AB" : regionCode,
                JurisdictionDisplayName = legislationProfile?.DisplayName ?? $"{countryCode}-{regionCode}",
                SupportedLanguagesJson = legislationProfile?.SupportedLanguagesJson ?? jurisdiction?.SupportedLanguagesJson ?? "[]",
                PropertyAddress = asset?.AddressLine1 ?? string.Empty,
                BillingPeriod = DateTime.UtcNow.ToString("yyyy-MM"),
                MonthlyRent = 2450m,
                AddAutomaticGst = legislationProfile?.AddAutomaticSalesTax ?? false,
                GeneratePdf = legislationProfile?.GeneratePdf ?? true,
                RequiredQuestionsJson = legislationProfile?.RequiredQuestionsJson ?? jurisdiction?.RequiredQuestionsJson ?? "[]",
                InvoiceRulesJson = legislationProfile?.InvoiceRulesJson ?? jurisdiction?.InvoiceRulesJson ?? "{}",
                SuggestedPrompt = $"Créer une facture {(legislationProfile?.GeneratePdf == false ? string.Empty : "PDF ")}pour {(asset?.AddressLine1 ?? "ce bien")} pour la période {DateTime.UtcNow:yyyy-MM} selon la juridiction {legislationProfile?.JurisdictionCode ?? $"{countryCode}-{regionCode}"} en {preferredLanguage}."
            };
        }

        public async Task<RuntiraLegislationExperienceDto?> GetLegislationExperienceAsync(CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            var legislationCatalog = _serviceProvider.GetService<ILegislationCatalog>();
            if (dbLease is null || legislationCatalog is null)
            {
                return null;
            }

            var db = dbLease.DbContext;

            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return null;
            }

            var organization = await db.RuntiraOrganizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tenantId.Value, cancellationToken);
            if (organization is null)
            {
                return null;
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? organization.CountryCode : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? organization.RegionCode : _currentOrganization.Province;
            var legislationProfile = legislationCatalog.GetProfile(countryCode, regionCode);
            if (legislationProfile is null)
            {
                return null;
            }

            var invoiceRules = ParseDictionary(legislationProfile.InvoiceRulesJson);
            var visibleInvoiceOptions = new List<string>();

            if (legislationProfile.GeneratePdf)
            {
                visibleInvoiceOptions.Add("GeneratePdf");
            }

            if (invoiceRules.TryGetValue("includePropertyAddress", out var includePropertyAddress) && includePropertyAddress)
            {
                visibleInvoiceOptions.Add("IncludePropertyAddress");
            }

            if (invoiceRules.TryGetValue("includeBillingPeriod", out var includeBillingPeriod) && includeBillingPeriod)
            {
                visibleInvoiceOptions.Add("IncludeBillingPeriod");
            }

            visibleInvoiceOptions.Add(legislationProfile.AddAutomaticSalesTax ? "AddAutomaticSalesTax" : "NoAutomaticSalesTax");

            return new RuntiraLegislationExperienceDto
            {
                JurisdictionCode = legislationProfile.JurisdictionCode,
                DisplayName = legislationProfile.DisplayName,
                CountryCode = legislationProfile.CountryCode,
                RegionCode = legislationProfile.RegionCode,
                PreferredLanguage = _currentOrganization.PreferredLanguage,
                SupportedLanguages = ParseList(legislationProfile.SupportedLanguagesJson),
                RequiredQuestions = ParseList(legislationProfile.RequiredQuestionsJson),
                VisibleInvoiceOptions = visibleInvoiceOptions
            };
        }

        public async Task<IReadOnlyList<RuntiraLeadSummaryDto>> GetLeadsAsync(CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            if (dbLease is null)
            {
                return Array.Empty<RuntiraLeadSummaryDto>();
            }

            var db = dbLease.DbContext;

            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return Array.Empty<RuntiraLeadSummaryDto>();
            }

            return await db.RuntiraLeads
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value)
                .OrderByDescending(x => x.QualificationScore)
                .ThenBy(x => x.FullName)
                .Select(x => new RuntiraLeadSummaryDto
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    Email = x.Email,
                    Status = x.Status,
                    Source = x.Source,
                    PreferredLanguage = x.PreferredLanguage,
                    AssetName = x.Asset != null ? x.Asset.Name : string.Empty,
                    QualificationScore = x.QualificationScore,
                    Summary = x.Summary,
                    ContextDataJson = x.ContextDataJson,
                    ContextData = RuntiraJson.Deserialize<RuntiraLeadContextData>(x.ContextDataJson)
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<RuntiraLeadConversionCandidateDto>> GetLeadConversionCandidatesAsync(CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            if (dbLease is null)
            {
                return Array.Empty<RuntiraLeadConversionCandidateDto>();
            }

            var db = dbLease.DbContext;

            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return Array.Empty<RuntiraLeadConversionCandidateDto>();
            }

            var leads = await db.RuntiraLeads
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value)
                .OrderByDescending(x => x.QualificationScore)
                .ThenBy(x => x.FullName)
                .Select(x => new
                {
                    x.Id,
                    x.AssetId,
                    x.FullName,
                    x.Email,
                    x.PreferredLanguage,
                    x.QualificationScore
                })
                .ToListAsync(cancellationToken);

            if (leads.Count == 0)
            {
                return Array.Empty<RuntiraLeadConversionCandidateDto>();
            }

            var assetIds = leads.Where(x => x.AssetId.HasValue).Select(x => x.AssetId!.Value).Distinct().ToList();
            var assets = await db.RuntiraAssets
                .AsNoTracking()
                .Where(x => assetIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            var units = await db.RuntiraUnits
                .AsNoTracking()
                .Where(x => assetIds.Contains(x.AssetId))
                .OrderBy(x => x.Status == "Available" ? 0 : 1)
                .ThenBy(x => x.UnitCode)
                .ToListAsync(cancellationToken);

            var residents = await db.RuntiraResidents
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value)
                .ToListAsync(cancellationToken);

            return leads
                .Take(5)
                .Select(lead =>
                {
                    var matchedResident = residents.FirstOrDefault(x =>
                        string.Equals(x.Email, lead.Email, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.FullName, lead.FullName, StringComparison.OrdinalIgnoreCase));

                    var suggestedUnit = lead.AssetId.HasValue
                        ? units.FirstOrDefault(x => x.AssetId == lead.AssetId.Value)
                        : null;

                    var nextAction = matchedResident is not null
                        ? "ExistingResidentReview"
                        : suggestedUnit is null
                            ? "AssignAsset"
                            : string.Equals(suggestedUnit.Status, "Available", StringComparison.OrdinalIgnoreCase)
                                ? "CreateResidentLease"
                                : "PrepareWaitlist";

                    return new RuntiraLeadConversionCandidateDto
                    {
                        LeadId = lead.Id,
                        LeadName = lead.FullName,
                        AssetName = lead.AssetId.HasValue && assets.TryGetValue(lead.AssetId.Value, out var asset) ? asset.Name : "—",
                        PreferredLanguage = lead.PreferredLanguage,
                        QualificationScore = lead.QualificationScore,
                        SuggestedUnitCode = suggestedUnit?.UnitCode ?? "—",
                        SuggestedRent = suggestedUnit?.MarketRent ?? 0m,
                        ResidentName = matchedResident?.FullName ?? "New resident",
                        NextAction = nextAction
                    };
                })
                .ToList();
        }

        public async Task<RuntiraLeadFormContextDto?> GetLeadFormContextAsync(CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            var legislationCatalog = _serviceProvider.GetService<ILegislationCatalog>();
            if (dbLease is null)
            {
                return null;
            }

            var db = dbLease.DbContext;

            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return null;
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            var profile = legislationCatalog?.GetProfile(countryCode, regionCode);
            var leadFormDefinition = ParseContextFormDefinition(profile?.AssetRulesJson, "leadForm", ["fullName", "email", "phoneNumber", "preferredLanguage", "targetAsset", "summary"], ["fullName", "email"]);

            var assets = await db.RuntiraAssets
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value)
                .OrderBy(x => x.Name)
                .Select(x => new RuntiraLeadAssetOptionDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync(cancellationToken);

            var supportedLanguages = profile is null
                ? new List<string> { _currentOrganization.PreferredLanguage }
                : ParseList(profile.SupportedLanguagesJson).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (supportedLanguages.Count == 0)
            {
                supportedLanguages.Add(_currentOrganization.PreferredLanguage);
            }

            return new RuntiraLeadFormContextDto
            {
                JurisdictionCode = profile?.JurisdictionCode ?? $"{countryCode}-{regionCode}",
                JurisdictionDisplayName = profile?.DisplayName ?? $"{countryCode}-{regionCode}",
                PreferredLanguage = _currentOrganization.PreferredLanguage,
                SupportedLanguages = supportedLanguages,
                Fields = leadFormDefinition.VisibleFields
                    .Select(x => new RuntiraLeadFormFieldDto
                    {
                        Key = x,
                        Required = leadFormDefinition.RequiredFields.Contains(x),
                        SuggestedValue = x.Equals("preferredLanguage", StringComparison.OrdinalIgnoreCase) ? _currentOrganization.PreferredLanguage : string.Empty
                    })
                    .ToList(),
                Assets = assets,
                AssetRulesJson = profile?.AssetRulesJson ?? "{}"
            };
        }

        public RuntiraFlexibleDataStrategyDto BuildFlexibleDataSnapshot(
            RuntiraAsset? asset,
            Dictionary<string, string>? leadContext,
            Dictionary<string, string>? leaseContext,
            Dictionary<string, string>? residentContext,
            string source)
        {
            var assetContext = new RuntiraAssetContextData
            {
                Source = source,
                Market = $"{_currentOrganization.CountryCode}-{_currentOrganization.Province}",
                SupportsMultiUnit = asset is not null,
                AssetName = asset?.Name ?? string.Empty,
                AssetAddress = asset?.AddressLine1 ?? string.Empty
            };

            var leadPayload = new RuntiraLeadContextData
            {
                Source = source,
                Market = $"{_currentOrganization.CountryCode}-{_currentOrganization.Province}",
                Language = _currentOrganization.PreferredLanguage,
                Fields = leadContext ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };

            var leasePayload = new RuntiraLeaseComplianceData
            {
                Source = source,
                Market = $"{_currentOrganization.CountryCode}-{_currentOrganization.Province}",
                Fields = leaseContext ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };

            var residentPayload = new RuntiraResidentProfileData
            {
                Source = source,
                Language = _currentOrganization.PreferredLanguage,
                Fields = residentContext ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };

            return new RuntiraFlexibleDataStrategyDto
            {
                AssetContextDataJson = RuntiraJson.Serialize(assetContext),
                LeadContextDataJson = RuntiraJson.Serialize(leadPayload),
                LeaseComplianceDataJson = RuntiraJson.Serialize(leasePayload),
                ResidentProfileDataJson = RuntiraJson.Serialize(residentPayload)
            };
        }

        public async Task<RuntiraLeaseConversionFormContextDto?> GetLeaseConversionFormContextAsync(Guid leadId, CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            var legislationCatalog = _serviceProvider.GetService<ILegislationCatalog>();
            if (dbLease is null)
            {
                return null;
            }

            var db = dbLease.DbContext;

            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return null;
            }

            var lead = await db.RuntiraLeads.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tenantId.Value && x.Id == leadId, cancellationToken);
            if (lead is null)
            {
                return null;
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            var profile = legislationCatalog?.GetProfile(countryCode, regionCode);
            var formDefinition = ParseContextFormDefinition(profile?.AssetRulesJson, "leaseConversionForm", ["residentName", "unitCode", "leaseStartDate", "monthlyRent", "billingPeriod"], ["residentName", "unitCode", "leaseStartDate", "monthlyRent"]);

            var asset = lead.AssetId.HasValue
                ? await db.RuntiraAssets.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tenantId.Value && x.Id == lead.AssetId.Value, cancellationToken)
                : null;
            var suggestedUnit = lead.AssetId.HasValue
                ? await db.RuntiraUnits.AsNoTracking().Where(x => x.TenantId == tenantId.Value && x.AssetId == lead.AssetId.Value).OrderBy(x => x.Status == "Available" ? 0 : 1).ThenBy(x => x.UnitCode).FirstOrDefaultAsync(cancellationToken)
                : await db.RuntiraUnits.AsNoTracking().Where(x => x.TenantId == tenantId.Value).OrderBy(x => x.Status == "Available" ? 0 : 1).ThenBy(x => x.UnitCode).FirstOrDefaultAsync(cancellationToken);

            var suggestedValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["residentName"] = lead.FullName,
                ["preferredLanguage"] = lead.PreferredLanguage,
                ["unitCode"] = suggestedUnit?.UnitCode ?? string.Empty,
                ["leaseStartDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["monthlyRent"] = (suggestedUnit?.MarketRent ?? 0m).ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["billingPeriod"] = "Monthly",
                ["propertyAddress"] = asset?.AddressLine1 ?? string.Empty,
                ["tenantName"] = lead.FullName,
                ["ownerName"] = _currentOrganization.OrganizationName
            };

            return new RuntiraLeaseConversionFormContextDto
            {
                LeadId = lead.Id,
                LeadName = lead.FullName,
                JurisdictionDisplayName = profile?.DisplayName ?? $"{countryCode}-{regionCode}",
                Fields = formDefinition.VisibleFields.Select(x => new RuntiraLeadFormFieldDto
                {
                    Key = x,
                    Required = formDefinition.RequiredFields.Contains(x),
                    SuggestedValue = suggestedValues.TryGetValue(x, out var value) ? value : string.Empty
                }).ToList()
            };
        }

        public async Task<RuntiraCreateLeadResultDto> CreateLeadAsync(RuntiraCreateLeadRequestDto request, CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            var legislationCatalog = _serviceProvider.GetService<ILegislationCatalog>();
            if (dbLease is null)
            {
                return new RuntiraCreateLeadResultDto { ResultCode = "Unavailable" };
            }

            var db = dbLease.DbContext;

            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return new RuntiraCreateLeadResultDto { ResultCode = "Unavailable" };
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            var profile = legislationCatalog?.GetProfile(countryCode, regionCode);
            var leadFormDefinition = ParseContextFormDefinition(profile?.AssetRulesJson, "leadForm", ["fullName", "email", "phoneNumber", "preferredLanguage", "targetAsset", "summary"], ["fullName", "email"]);

            foreach (var field in leadFormDefinition.RequiredFields)
            {
                var value = field.ToLowerInvariant() switch
                {
                    "fullname" => request.FullName,
                    "email" => request.Email,
                    "phonenumber" => request.PhoneNumber,
                    "preferredlanguage" => request.PreferredLanguage,
                    "targetasset" => request.AssetId?.ToString(),
                    "summary" => request.Summary,
                    _ => request.DynamicFields.TryGetValue(field, out var dynamicValue) ? dynamicValue : string.Empty
                };

                if (string.IsNullOrWhiteSpace(value))
                {
                    return new RuntiraCreateLeadResultDto
                    {
                        ResultCode = "MissingRequiredField",
                        FieldKey = field
                    };
                }
            }

            RuntiraAsset? asset = null;
            if (request.AssetId.HasValue)
            {
                asset = await db.RuntiraAssets.FirstOrDefaultAsync(x => x.TenantId == tenantId.Value && x.Id == request.AssetId.Value, cancellationToken);
                if (asset is null)
                {
                    return new RuntiraCreateLeadResultDto { ResultCode = "InvalidAsset" };
                }
            }

            var supportedLanguages = profile is null
                ? new List<string> { _currentOrganization.PreferredLanguage }
                : ParseList(profile.SupportedLanguagesJson).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var preferredLanguage = supportedLanguages.Contains(request.PreferredLanguage, StringComparer.OrdinalIgnoreCase)
                ? request.PreferredLanguage
                : supportedLanguages.FirstOrDefault() ?? _currentOrganization.PreferredLanguage;

            var notes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in leadFormDefinition.VisibleFields)
            {
                if (field is "fullName" or "email" or "phoneNumber" or "preferredLanguage" or "targetAsset" or "summary")
                {
                    continue;
                }

                if (request.DynamicFields.TryGetValue(field, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    notes[field] = value;
                }
            }

            var flexibleData = BuildFlexibleDataSnapshot(asset, request.DynamicFields, null, request.DynamicFields, "ManualContextForm");

            var lead = new RuntiraLead
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                AssetId = request.AssetId,
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                PhoneNumber = request.PhoneNumber.Trim(),
                Source = "ManualContextForm",
                Status = "New",
                PreferredLanguage = preferredLanguage,
                QualificationScore = 55,
                Summary = string.IsNullOrWhiteSpace(request.Summary)
                    ? $"Manual lead created for {asset?.Name ?? _currentOrganization.OrganizationName}."
                    : request.Summary.Trim(),
                NotesJson = flexibleData.LeadContextDataJson,
                CreatedUtc = DateTime.UtcNow
            };

            db.RuntiraLeads.Add(lead);
            await db.SaveChangesAsync(cancellationToken);

            return new RuntiraCreateLeadResultDto
            {
                Success = true,
                ResultCode = "Created",
                LeadName = lead.FullName
            };
        }

        public async Task<RuntiraLeadConversionResultDto> ConvertLeadAsync(Guid leadId, Dictionary<string, string>? contextFields = null, CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            var legislationCatalog = _serviceProvider.GetService<ILegislationCatalog>();
            if (dbLease is null)
            {
                return new RuntiraLeadConversionResultDto { ResultCode = "Unavailable" };
            }

            var db = dbLease.DbContext;

            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return new RuntiraLeadConversionResultDto { ResultCode = "Unavailable" };
            }

            var lead = await db.RuntiraLeads
                .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value && x.Id == leadId, cancellationToken);

            if (lead is null)
            {
                return new RuntiraLeadConversionResultDto { ResultCode = "LeadNotFound" };
            }

            if (string.Equals(lead.Status, "Converted", StringComparison.OrdinalIgnoreCase))
            {
                return new RuntiraLeadConversionResultDto
                {
                    ResultCode = "AlreadyConverted",
                    LeadName = lead.FullName
                };
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            var profile = legislationCatalog?.GetProfile(countryCode, regionCode);
            var conversionDefinition = ParseContextFormDefinition(profile?.AssetRulesJson, "leaseConversionForm", ["residentName", "unitCode", "leaseStartDate", "monthlyRent", "billingPeriod"], ["residentName", "unitCode", "leaseStartDate", "monthlyRent"]);
            var normalizedContext = new Dictionary<string, string>(contextFields ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);

            foreach (var field in conversionDefinition.RequiredFields)
            {
                if (!normalizedContext.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    return new RuntiraLeadConversionResultDto
                    {
                        ResultCode = "MissingRequiredField",
                        LeadName = lead.FullName,
                        UnitCode = field
                    };
                }
            }

            IQueryable<RuntiraUnit> unitQuery = db.RuntiraUnits.Where(x => x.TenantId == tenantId.Value);
            if (lead.AssetId.HasValue)
            {
                unitQuery = unitQuery.Where(x => x.AssetId == lead.AssetId.Value);
            }

            if (normalizedContext.TryGetValue("unitCode", out var unitCode) && !string.IsNullOrWhiteSpace(unitCode))
            {
                unitQuery = unitQuery.Where(x => x.UnitCode == unitCode);
            }

            var unit = await unitQuery
                .OrderBy(x => x.Status == "Available" ? 0 : 1)
                .ThenBy(x => x.UnitCode)
                .FirstOrDefaultAsync(cancellationToken);

            if (unit is null)
            {
                return new RuntiraLeadConversionResultDto
                {
                    ResultCode = "UnitNotFound",
                    LeadName = lead.FullName
                };
            }

            var leadEmail = lead.Email.Trim().ToUpperInvariant();
            var leadFullName = lead.FullName.Trim().ToUpperInvariant();

            var resident = await db.RuntiraResidents
                .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value &&
                    ((x.Email ?? string.Empty).ToUpper() == leadEmail
                    || (x.FullName ?? string.Empty).ToUpper() == leadFullName), cancellationToken);

            if (resident is null)
            {
                resident = new RuntiraResident
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId.Value,
                    FullName = normalizedContext.TryGetValue("residentName", out var residentName) && !string.IsNullOrWhiteSpace(residentName) ? residentName : lead.FullName,
                    Email = lead.Email,
                    PhoneNumber = lead.PhoneNumber,
                    PreferredLanguage = normalizedContext.TryGetValue("preferredLanguage", out var preferredLanguage) && !string.IsNullOrWhiteSpace(preferredLanguage)
                        ? preferredLanguage
                        : (string.IsNullOrWhiteSpace(lead.PreferredLanguage) ? _currentOrganization.PreferredLanguage : lead.PreferredLanguage),
                    Status = "Active",
                    NotesJson = JsonSerializer.Serialize(normalizedContext),
                    CreatedUtc = DateTime.UtcNow
                };

                db.RuntiraResidents.Add(resident);
            }

            var monthlyRent = normalizedContext.TryGetValue("monthlyRent", out var monthlyRentValue)
                && decimal.TryParse(monthlyRentValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedMonthlyRent)
                    ? parsedMonthlyRent
                    : unit.MarketRent;
            var leaseStartUtc = normalizedContext.TryGetValue("leaseStartDate", out var leaseStartValue)
                && DateTime.TryParse(leaseStartValue, out var parsedLeaseStart)
                    ? parsedLeaseStart.Date
                    : DateTime.UtcNow.Date;
            var billingPeriod = normalizedContext.TryGetValue("billingPeriod", out var billingPeriodValue) && !string.IsNullOrWhiteSpace(billingPeriodValue)
                ? billingPeriodValue
                : "Monthly";
            var flexibleData = BuildFlexibleDataSnapshot(null, null, normalizedContext, normalizedContext, "MockLeadConversion");

            var existingLease = await db.RuntiraLeases
                .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value && x.UnitId == unit.Id && x.ResidentId == resident.Id, cancellationToken);

            var leaseStatus = string.Equals(unit.Status, "Available", StringComparison.OrdinalIgnoreCase) ? "Active" : "Pending";
            if (existingLease is null)
            {
                db.RuntiraLeases.Add(new RuntiraLease
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId.Value,
                    AssetId = unit.AssetId,
                    UnitId = unit.Id,
                    ResidentId = resident.Id,
                    LeaseStartUtc = leaseStartUtc,
                    LeaseEndUtc = leaseStartUtc.AddYears(1).AddDays(-1),
                    MonthlyRent = monthlyRent,
                    BillingPeriod = billingPeriod,
                    Status = leaseStatus,
                    TermsJson = flexibleData.LeaseComplianceDataJson,
                    CreatedUtc = DateTime.UtcNow
                });
            }

            if (existingLease is null)
            {
                var addedLease = db.RuntiraLeases.Local.LastOrDefault();
                if (addedLease is not null)
                {
                    addedLease.ComplianceDataJson = flexibleData.LeaseComplianceDataJson;
                }
            }
            else
            {
                leaseStatus = existingLease.Status;
                existingLease.ComplianceDataJson = flexibleData.LeaseComplianceDataJson;
                existingLease.ModifiedUtc = DateTime.UtcNow;
            }

            if (string.Equals(unit.Status, "Available", StringComparison.OrdinalIgnoreCase))
            {
                unit.Status = "Occupied";
                unit.ModifiedUtc = DateTime.UtcNow;
            }

            lead.Status = "Converted";
            lead.Summary = string.IsNullOrWhiteSpace(lead.Summary)
                ? $"Converted to resident {resident.FullName} and unit {unit.UnitCode}."
                : $"{lead.Summary} Converted to resident {resident.FullName} and unit {unit.UnitCode}.";
            lead.ContextDataJson = flexibleData.LeadContextDataJson;
            lead.ModifiedUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);

            return new RuntiraLeadConversionResultDto
            {
                Success = true,
                ResultCode = "Converted",
                LeadName = lead.FullName,
                ResidentName = resident.FullName,
                UnitCode = unit.UnitCode,
                LeaseStatus = leaseStatus
            };
        }

        public async Task<RuntiraLeadActionResultDto> ArchiveLeadAsync(Guid leadId, CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            if (dbLease is null)
            {
                return new RuntiraLeadActionResultDto { ResultCode = "Unavailable" };
            }

            var db = dbLease.DbContext;
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return new RuntiraLeadActionResultDto { ResultCode = "Unavailable" };
            }

            var lead = await db.RuntiraLeads.FirstOrDefaultAsync(x => x.TenantId == tenantId.Value && x.Id == leadId, cancellationToken);
            if (lead is null)
            {
                return new RuntiraLeadActionResultDto { ResultCode = "LeadNotFound" };
            }

            if (string.Equals(lead.Status, "Archived", StringComparison.OrdinalIgnoreCase))
            {
                return new RuntiraLeadActionResultDto
                {
                    Success = true,
                    ResultCode = "AlreadyArchived",
                    LeadName = lead.FullName
                };
            }

            lead.Status = "Archived";
            lead.ModifiedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            return new RuntiraLeadActionResultDto
            {
                Success = true,
                ResultCode = "Archived",
                LeadName = lead.FullName
            };
        }

        public async Task<RuntiraLeadActionResultDto> DeleteLeadAsync(Guid leadId, CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            if (dbLease is null)
            {
                return new RuntiraLeadActionResultDto { ResultCode = "Unavailable" };
            }

            var db = dbLease.DbContext;
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return new RuntiraLeadActionResultDto { ResultCode = "Unavailable" };
            }

            var lead = await db.RuntiraLeads.FirstOrDefaultAsync(x => x.TenantId == tenantId.Value && x.Id == leadId, cancellationToken);
            if (lead is null)
            {
                return new RuntiraLeadActionResultDto { ResultCode = "LeadNotFound" };
            }

            var leadName = lead.FullName;
            db.RuntiraLeads.Remove(lead);
            await db.SaveChangesAsync(cancellationToken);

            return new RuntiraLeadActionResultDto
            {
                Success = true,
                ResultCode = "Deleted",
                LeadName = leadName
            };
        }

        public async Task<RuntiraUnitActionResultDto> ManageUnitAsync(Guid unitId, string action, CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            if (dbLease is null)
            {
                return new RuntiraUnitActionResultDto { ResultCode = "Unavailable" };
            }

            var db = dbLease.DbContext;
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return new RuntiraUnitActionResultDto { ResultCode = "Unavailable" };
            }

            var unit = await db.RuntiraUnits.FirstOrDefaultAsync(x => x.TenantId == tenantId.Value && x.Id == unitId, cancellationToken);
            if (unit is null)
            {
                return new RuntiraUnitActionResultDto { ResultCode = "UnitNotFound" };
            }

            var normalizedAction = action?.Trim().ToLowerInvariant() ?? string.Empty;
            var activeLeaseExists = await db.RuntiraLeases.AnyAsync(x => x.TenantId == tenantId.Value && x.UnitId == unitId && x.Status.ToUpper() == "ACTIVE", cancellationToken);

            switch (normalizedAction)
            {
                case "markmaintenance":
                    unit.Status = "Maintenance";
                    break;
                case "markavailable":
                    unit.Status = "Available";
                    break;
                case "delete":
                    if (activeLeaseExists)
                    {
                        return new RuntiraUnitActionResultDto
                        {
                            ResultCode = "UnitHasActiveLease",
                            UnitCode = unit.UnitCode,
                            Status = unit.Status
                        };
                    }

                    db.RuntiraUnits.Remove(unit);
                    await db.SaveChangesAsync(cancellationToken);
                    return new RuntiraUnitActionResultDto
                    {
                        Success = true,
                        ResultCode = "Deleted",
                        UnitCode = unit.UnitCode,
                        Status = "Deleted"
                    };
                default:
                    return new RuntiraUnitActionResultDto
                    {
                        ResultCode = "UnsupportedAction",
                        UnitCode = unit.UnitCode,
                        Status = unit.Status
                    };
            }

            unit.ModifiedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            return new RuntiraUnitActionResultDto
            {
                Success = true,
                ResultCode = "Updated",
                UnitCode = unit.UnitCode,
                Status = unit.Status
            };
        }

        public async Task<RuntiraResidentActionResultDto> ManageResidentAsync(Guid residentId, string action, CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            if (dbLease is null)
            {
                return new RuntiraResidentActionResultDto { ResultCode = "Unavailable" };
            }

            var db = dbLease.DbContext;
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return new RuntiraResidentActionResultDto { ResultCode = "Unavailable" };
            }

            var resident = await db.RuntiraResidents.FirstOrDefaultAsync(x => x.TenantId == tenantId.Value && x.Id == residentId, cancellationToken);
            if (resident is null)
            {
                return new RuntiraResidentActionResultDto { ResultCode = "ResidentNotFound" };
            }

            var normalizedAction = action?.Trim().ToLowerInvariant() ?? string.Empty;

            switch (normalizedAction)
            {
                case "markwatch":
                    resident.Status = "Watch";
                    break;
                case "markactive":
                    resident.Status = "Active";
                    break;
                default:
                    return new RuntiraResidentActionResultDto
                    {
                        ResultCode = "UnsupportedAction",
                        ResidentName = resident.FullName,
                        Status = resident.Status
                    };
            }

            resident.ModifiedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            return new RuntiraResidentActionResultDto
            {
                Success = true,
                ResultCode = "Updated",
                ResidentName = resident.FullName,
                Status = resident.Status
            };
        }

        public async Task<RuntiraLeaseActionResultDto> ManageLeaseAsync(Guid leaseId, string action, CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            if (dbLease is null)
            {
                return new RuntiraLeaseActionResultDto { ResultCode = "Unavailable" };
            }

            var db = dbLease.DbContext;
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return new RuntiraLeaseActionResultDto { ResultCode = "Unavailable" };
            }

            var lease = await db.RuntiraLeases
                .Join(db.RuntiraUnits, left => left.UnitId, right => right.Id, (left, right) => new { Lease = left, Unit = right })
                .FirstOrDefaultAsync(x => x.Lease.TenantId == tenantId.Value && x.Lease.Id == leaseId, cancellationToken);

            if (lease is null)
            {
                return new RuntiraLeaseActionResultDto { ResultCode = "LeaseNotFound" };
            }

            var normalizedAction = action?.Trim().ToLowerInvariant() ?? string.Empty;

            switch (normalizedAction)
            {
                case "markreview":
                    lease.Lease.Status = "Review";
                    break;
                case "markactive":
                    lease.Lease.Status = "Active";
                    break;
                default:
                    return new RuntiraLeaseActionResultDto
                    {
                        ResultCode = "UnsupportedAction",
                        UnitCode = lease.Unit.UnitCode,
                        LeaseStatus = lease.Lease.Status
                    };
            }

            lease.Lease.ModifiedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            return new RuntiraLeaseActionResultDto
            {
                Success = true,
                ResultCode = "Updated",
                UnitCode = lease.Unit.UnitCode,
                LeaseStatus = lease.Lease.Status
            };
        }

        public async Task<RuntiraImportValidationContextDto?> GetImportValidationContextAsync(CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            var legislationCatalog = _serviceProvider.GetService<ILegislationCatalog>();
            if (dbLease is null)
            {
                return null;
            }

            var db = dbLease.DbContext;

            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return null;
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            var profile = legislationCatalog?.GetProfile(countryCode, regionCode);
            var importDefinition = ParseContextFormDefinition(profile?.AssetRulesJson, "importValidationForm", ["fullName", "email", "preferredLanguage", "targetAsset", "summary"], ["fullName", "email"]);

            var topLead = await db.RuntiraLeads.AsNoTracking().Where(x => x.TenantId == tenantId.Value).OrderByDescending(x => x.QualificationScore).ThenBy(x => x.FullName).FirstOrDefaultAsync(cancellationToken);
            var firstAsset = await db.RuntiraAssets.AsNoTracking().Where(x => x.TenantId == tenantId.Value).OrderBy(x => x.Name).FirstOrDefaultAsync(cancellationToken);

            var suggestedValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["fullName"] = topLead?.FullName ?? "Taylor Morgan",
                ["email"] = topLead?.Email ?? "taylor@example.com",
                ["preferredLanguage"] = _currentOrganization.PreferredLanguage,
                ["targetAsset"] = firstAsset?.Id.ToString() ?? string.Empty,
                ["summary"] = "Validated from mock AI import pipeline.",
                ["propertyAddress"] = firstAsset?.AddressLine1 ?? string.Empty,
                ["tenantName"] = topLead?.FullName ?? "Taylor Morgan",
                ["ownerName"] = _currentOrganization.OrganizationName,
                ["billingPeriod"] = DateTime.UtcNow.ToString("yyyy-MM")
            };

            return new RuntiraImportValidationContextDto
            {
                SourceName = "prospects-q3.xlsx",
                JurisdictionDisplayName = profile?.DisplayName ?? $"{countryCode}-{regionCode}",
                Fields = importDefinition.VisibleFields.Select(x => new RuntiraLeadFormFieldDto
                {
                    Key = x,
                    Required = importDefinition.RequiredFields.Contains(x),
                    SuggestedValue = suggestedValues.TryGetValue(x, out var value) ? value : string.Empty
                }).ToList()
            };
        }

        public async Task<RuntiraImportApprovalResultDto> ApproveImportAsync(RuntiraImportApprovalRequestDto request, CancellationToken cancellationToken = default)
        {
            var legislationCatalog = _serviceProvider.GetService<ILegislationCatalog>();

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            var profile = legislationCatalog?.GetProfile(countryCode, regionCode);
            var importDefinition = ParseContextFormDefinition(profile?.AssetRulesJson, "importValidationForm", ["fullName", "email", "preferredLanguage", "targetAsset", "summary"], ["fullName", "email"]);

            foreach (var field in importDefinition.RequiredFields)
            {
                if (!request.DynamicFields.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    return new RuntiraImportApprovalResultDto
                    {
                        ResultCode = "MissingRequiredField",
                        FieldKey = field
                    };
                }
            }

            var createRequest = new RuntiraCreateLeadRequestDto
            {
                AssetId = request.DynamicFields.TryGetValue("targetAsset", out var assetIdValue) && Guid.TryParse(assetIdValue, out var assetId) ? assetId : null,
                FullName = request.DynamicFields.TryGetValue("fullName", out var fullName) ? fullName : string.Empty,
                Email = request.DynamicFields.TryGetValue("email", out var email) ? email : string.Empty,
                PhoneNumber = request.DynamicFields.TryGetValue("phoneNumber", out var phoneNumber) ? phoneNumber : string.Empty,
                PreferredLanguage = request.DynamicFields.TryGetValue("preferredLanguage", out var preferredLanguage) ? preferredLanguage : _currentOrganization.PreferredLanguage,
                Summary = request.DynamicFields.TryGetValue("summary", out var summary) ? summary : $"Validated from {request.SourceName}.",
                DynamicFields = new Dictionary<string, string>(request.DynamicFields, StringComparer.OrdinalIgnoreCase)
            };

            var createResult = await CreateLeadAsync(createRequest, cancellationToken);
            return new RuntiraImportApprovalResultDto
            {
                Success = createResult.Success,
                ResultCode = createResult.Success ? "Approved" : createResult.ResultCode,
                LeadName = createResult.LeadName,
                FieldKey = createResult.FieldKey
            };
        }

        public async Task<RuntiraAssetWorkspaceDto?> GetAssetWorkspaceAsync(CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            if (dbLease is null)
            {
                return null;
            }

            var db = dbLease.DbContext;

            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return null;
            }

            var asset = await db.RuntiraAssets
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value, cancellationToken);

            if (asset is null)
            {
                return null;
            }

            var units = await db.RuntiraUnits
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value && x.AssetId == asset.Id)
                .OrderBy(x => x.UnitCode)
                .Select(x => new RuntiraUnitSummaryDto
                {
                    Id = x.Id,
                    UnitCode = x.UnitCode,
                    UnitType = x.UnitType,
                    Status = x.Status,
                    MarketRent = x.MarketRent,
                    CanDelete = !db.RuntiraLeases.Any(lease => lease.TenantId == tenantId.Value && lease.UnitId == x.Id && lease.Status.ToUpper() == "ACTIVE")
                })
                .ToListAsync(cancellationToken);

            var leases = await db.RuntiraLeases
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value && x.AssetId == asset.Id)
                .Join(db.RuntiraUnits.AsNoTracking(), lease => lease.UnitId, unit => unit.Id, (lease, unit) => new { lease, unit })
                .Join(db.RuntiraResidents.AsNoTracking(), joined => joined.lease.ResidentId, resident => resident.Id, (joined, resident) => new RuntiraLeaseSummaryDto
                {
                    Id = joined.lease.Id,
                    UnitCode = joined.unit.UnitCode,
                    ResidentName = resident.FullName,
                    MonthlyRent = joined.lease.MonthlyRent,
                    Status = joined.lease.Status,
                    BillingPeriod = joined.lease.BillingPeriod,
                    ComplianceDataJson = joined.lease.ComplianceDataJson,
                    ComplianceData = RuntiraJson.Deserialize<RuntiraLeaseComplianceData>(joined.lease.ComplianceDataJson)
                })
                .ToListAsync(cancellationToken);

            var residents = await db.RuntiraResidents
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value)
                .OrderBy(x => x.FullName)
                .Select(x => new RuntiraResidentSummaryDto
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    Email = x.Email,
                    PreferredLanguage = x.PreferredLanguage,
                    Status = x.Status,
                    ProfileDataJson = x.ProfileDataJson,
                    ProfileData = RuntiraJson.Deserialize<RuntiraResidentProfileData>(x.ProfileDataJson)
                })
                .ToListAsync(cancellationToken);

            return new RuntiraAssetWorkspaceDto
            {
                AssetId = asset.Id,
                AssetName = asset.Name,
                AssetAddress = asset.AddressLine1,
                AssetType = asset.AssetType,
                UnitCount = asset.UnitCount,
                TotalResidentCount = residents.Count,
                TotalLeaseCount = leases.Count,
                ContextDataJson = asset.ContextDataJson,
                ContextData = RuntiraJson.Deserialize<RuntiraAssetContextData>(asset.ContextDataJson),
                Units = units,
                Leases = leases,
                Residents = residents
            };
        }

        public async Task<IReadOnlyList<RuntiraInboxMessageDto>> GetInboxAsync(CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            if (dbLease is null)
            {
                return Array.Empty<RuntiraInboxMessageDto>();
            }

            var db = dbLease.DbContext;

            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return Array.Empty<RuntiraInboxMessageDto>();
            }

            return await db.RuntiraInboxMessages
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value)
                .OrderByDescending(x => x.ReceivedUtc)
                .Select(x => new RuntiraInboxMessageDto
                {
                    Id = x.Id,
                    FromEmail = x.FromEmail,
                    Subject = x.Subject,
                    PreviewText = x.PreviewText,
                    Status = x.Status,
                    Category = x.Category,
                    ReceivedUtc = x.ReceivedUtc,
                    HasAttachments = x.HasAttachments
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<RuntiraImportWorkspaceDto> GetImportWorkspaceAsync(CancellationToken cancellationToken = default)
        {
            using var dbLease = TryCreateDbContext();
            if (dbLease is null)
            {
                return CreateFallbackImportWorkspace();
            }

            var db = dbLease.DbContext;

            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return CreateFallbackImportWorkspace();
            }

            var topLead = await db.RuntiraLeads
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value)
                .OrderByDescending(x => x.QualificationScore)
                .ThenBy(x => x.FullName)
                .FirstOrDefaultAsync(cancellationToken);

            var latestMessage = await db.RuntiraInboxMessages
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value)
                .OrderByDescending(x => x.ReceivedUtc)
                .FirstOrDefaultAsync(cancellationToken);

            var firstAsset = await db.RuntiraAssets
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value)
                .OrderBy(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken);

            var sources = new List<RuntiraImportSourceDto>
            {
                new()
                {
                    SourceName = "prospects-q3.xlsx",
                    SourceType = "Excel",
                    Status = "MockReady",
                    SuggestedRecordCount = topLead is null ? 12 : Math.Max(12, topLead.QualificationScore / 5),
                    Summary = topLead is null
                        ? "Extraction simulée de prospects multi-unités prête pour validation."
                        : $"Extraction simulée alignée sur le lead {topLead.FullName} et ses préférences de marché."
                },
                new()
                {
                    SourceName = latestMessage?.Subject ?? "lease-renewal.pdf",
                    SourceType = latestMessage?.HasAttachments == true ? "PDF" : "Email",
                    Status = latestMessage is null ? "Preview" : latestMessage.Status,
                    SuggestedRecordCount = latestMessage?.HasAttachments == true ? 3 : 1,
                    Summary = latestMessage is null
                        ? "Document mocké prêt pour extraction des champs bail, unité et contact."
                        : $"Source reliée à l’inbox mockée pour classer {latestMessage.Category}."
                },
                new()
                {
                    SourceName = firstAsset?.Name ?? "tenant-notes.txt",
                    SourceType = "Text",
                    Status = "NeedsValidation",
                    SuggestedRecordCount = firstAsset is null ? 2 : 4,
                    Summary = firstAsset is null
                        ? "Texte libre converti en données structurées avant archivage JSON."
                        : $"Pré-remplissage métier autour du bien {firstAsset.Name}."
                }
            };

            var suggestedFields = new List<RuntiraImportFieldSuggestionDto>
            {
                new()
                {
                    FieldName = "LeadFullName",
                    SuggestedValue = topLead?.FullName ?? "Taylor Morgan",
                    ConfidenceScore = 96
                },
                new()
                {
                    FieldName = "LeadEmail",
                    SuggestedValue = topLead?.Email ?? "taylor@example.com",
                    ConfidenceScore = 94
                },
                new()
                {
                    FieldName = "PreferredLanguage",
                    SuggestedValue = _currentOrganization.PreferredLanguage,
                    ConfidenceScore = 98
                },
                new()
                {
                    FieldName = "TargetAsset",
                    SuggestedValue = firstAsset?.Name ?? "Runtira Demo Asset",
                    ConfidenceScore = 90
                },
                new()
                {
                    FieldName = "ImportIntent",
                    SuggestedValue = latestMessage?.Category ?? "LeadQualification",
                    ConfidenceScore = 88
                }
            };

            return new RuntiraImportWorkspaceDto
            {
                ActiveRegion = $"{_currentOrganization.CountryCode}-{_currentOrganization.Province}",
                ActiveLanguage = _currentOrganization.PreferredLanguage,
                SupportedFormats = ["Excel", "CSV", "Text", "PDF"],
                Sources = sources,
                SuggestedFields = suggestedFields
            };
        }

        public async Task<RuntiraExportWorkspaceDto> GetExportWorkspaceAsync(CancellationToken cancellationToken = default)
        {
            var summary = await GetWorkspaceSummaryAsync(cancellationToken);

            var options = new List<RuntiraExportOptionDto>
            {
                new()
                {
                    Format = "Excel",
                    Audience = "Operations",
                    Status = "Ready",
                    Summary = "Exporter les leads, résidents et actifs en tableur pour traitement métier."
                },
                new()
                {
                    Format = "PDF",
                    Audience = "Client",
                    Status = "Ready",
                    Summary = "Produire des documents envoyables comme factures, résumés locatifs et dossiers de validation."
                },
                new()
                {
                    Format = "Word",
                    Audience = "Legal",
                    Status = "Planned",
                    Summary = "Préparer des documents narratifs et modèles éditables pour contrats et communications."
                },
                new()
                {
                    Format = "CSV",
                    Audience = "Integrations",
                    Status = "Ready",
                    Summary = "Partager des extractions simples pour imports externes, BI ou scripts internes."
                }
            };

            return new RuntiraExportWorkspaceDto
            {
                ActiveRegion = summary is null ? $"{_currentOrganization.CountryCode}-{_currentOrganization.Province}" : $"{_currentOrganization.CountryCode}-{_currentOrganization.Province}",
                ActiveLanguage = _currentOrganization.PreferredLanguage,
                Options = options,
                SupportedDestinations = ["Download", "EmailDraft", "BlobArchive", "ExternalShare"]
            };
        }

        public async Task<RuntiraExportFileDto?> ExportLeadsCsvAsync(CancellationToken cancellationToken = default)
        {
            var leads = await GetLeadsAsync(cancellationToken);
            if (leads.Count == 0)
            {
                return null;
            }

            var rows = new List<string>
            {
                "FullName,Email,Status,Source,AssetName,PreferredLanguage,QualificationScore,Summary"
            };

            rows.AddRange(leads.Select(lead => string.Join(',',
                EscapeCsv(lead.FullName),
                EscapeCsv(lead.Email),
                EscapeCsv(lead.Status),
                EscapeCsv(lead.Source),
                EscapeCsv(lead.AssetName),
                EscapeCsv(lead.PreferredLanguage),
                lead.QualificationScore.ToString(System.Globalization.CultureInfo.InvariantCulture),
                EscapeCsv(lead.Summary))));

            var content = string.Join(Environment.NewLine, rows);
            var slug = string.IsNullOrWhiteSpace(_currentOrganization.OrganizationSlug) ? "workspace" : _currentOrganization.OrganizationSlug;

            return new RuntiraExportFileDto
            {
                FileName = $"{slug}-leads-{DateTime.UtcNow:yyyyMMddHHmmss}.csv",
                ContentType = "text/csv; charset=utf-8",
                Content = System.Text.Encoding.UTF8.GetBytes(content)
            };
        }

        private RuntiraImportWorkspaceDto CreateFallbackImportWorkspace()
            => new()
            {
                ActiveRegion = $"{_currentOrganization.CountryCode}-{_currentOrganization.Province}",
                ActiveLanguage = _currentOrganization.PreferredLanguage,
                SupportedFormats = ["Excel", "CSV", "Text", "PDF"],
                Sources =
                [
                    new()
                    {
                        SourceName = "prospects-q3.xlsx",
                        SourceType = "Excel",
                        Status = "MockReady",
                        SuggestedRecordCount = 12,
                        Summary = "Extraction simulée prête pour validation utilisateur."
                    }
                ],
                SuggestedFields =
                [
                    new()
                    {
                        FieldName = "PreferredLanguage",
                        SuggestedValue = _currentOrganization.PreferredLanguage,
                        ConfidenceScore = 98
                    }
                ]
            };

        private static FormDefinition ParseContextFormDefinition(string? assetRulesJson, string sectionName, IReadOnlyList<string> defaultVisibleFields, IReadOnlyList<string> defaultRequiredFields)
        {
            if (string.IsNullOrWhiteSpace(assetRulesJson))
            {
                return new FormDefinition(defaultVisibleFields, defaultRequiredFields);
            }

            using var document = JsonDocument.Parse(assetRulesJson);
            if (!document.RootElement.TryGetProperty(sectionName, out var leadFormElement) || leadFormElement.ValueKind != JsonValueKind.Object)
            {
                return new FormDefinition(defaultVisibleFields, defaultRequiredFields);
            }

            var visibleFields = ReadStringArray(leadFormElement, "visibleFields", defaultVisibleFields);
            var requiredFields = ReadStringArray(leadFormElement, "requiredFields", defaultRequiredFields);
            return new FormDefinition(visibleFields, requiredFields);
        }

        private static IReadOnlyList<string> ReadStringArray(JsonElement parent, string propertyName, IReadOnlyList<string> fallback)
        {
            if (!parent.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
            {
                return fallback;
            }

            var values = property
                .EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToList();

            return values.Count == 0 ? fallback : values;
        }

        private static IReadOnlyList<string> ParseList(string json)
            => RuntiraJson.Deserialize<List<string>>(json) ?? new List<string>();

        private static Dictionary<string, bool> ParseDictionary(string json)
        {
            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.True || property.Value.ValueKind == JsonValueKind.False)
                {
                    result[property.Name] = property.Value.GetBoolean();
                }
            }

            return result;
        }

        private static string EscapeCsv(string? value)
        {
            var normalized = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
            return $"\"{normalized.Replace("\"", "\"\"")}\"";
        }

        private IApplicationDbContextLease? TryCreateDbContext()
            => _serviceProvider.GetService<IApplicationDbContextFactory>()?.CreateDbContext();

        private sealed class FormDefinition
        {
            public FormDefinition(IReadOnlyList<string> visibleFields, IReadOnlyList<string> requiredFields)
            {
                VisibleFields = visibleFields;
                RequiredFields = new HashSet<string>(requiredFields, StringComparer.OrdinalIgnoreCase);
            }

            public IReadOnlyList<string> VisibleFields { get; }
            public HashSet<string> RequiredFields { get; }
        }
    }

    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddRuntiraApplication(this IServiceCollection services)
        {
            services.AddScoped<RuntiraWorkspaceService>();
            services.AddScoped<ITenantContextAccessor>(_ => new TenantContext { TenantId = null, BypassTenantFilter = false });
            services.AddScoped<CurrentOrganization>();
            return services;
        }
    }
}
