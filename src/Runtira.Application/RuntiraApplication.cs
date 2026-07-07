using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Runtira.Domain.Entities;

namespace Runtira.Application.Abstractions
{
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

    public interface IRuntiraAssetWorkspaceStore
    {
        Task<Runtira.Application.Features.RuntiraAssetWorkspaceDto?> GetAssetWorkspaceAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraUnitActionResultDto> ManageUnitAsync(Guid tenantId, Guid unitId, string action, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraResidentActionResultDto> ManageResidentAsync(Guid tenantId, Guid residentId, string action, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraLeaseActionResultDto> ManageLeaseAsync(Guid tenantId, Guid leaseId, string action, CancellationToken cancellationToken = default);
    }

    public interface IRuntiraLeadWorkspaceStore
    {
        Task<IReadOnlyList<Runtira.Application.Features.RuntiraLeadSummaryDto>> GetLeadsAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Runtira.Application.Features.RuntiraLeadConversionCandidateDto>> GetLeadConversionCandidatesAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraLeadFormContextDto> GetLeadFormContextAsync(Guid tenantId, string preferredLanguage, string countryCode, string regionCode, Runtira.Application.Features.RuntiraLegislationProfileDto? profile, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraLeaseConversionFormContextDto?> GetLeaseConversionFormContextAsync(Guid tenantId, Guid leadId, string organizationName, string countryCode, string regionCode, Runtira.Application.Features.RuntiraLegislationProfileDto? profile, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraCreateLeadResultDto> CreateLeadAsync(Guid tenantId, string organizationName, string preferredLanguage, IReadOnlyList<string> supportedLanguages, Runtira.Application.Features.RuntiraCreateLeadRequestDto request, Func<Runtira.Domain.Entities.RuntiraAsset?, Dictionary<string, string>?, Dictionary<string, string>?, Dictionary<string, string>?, string, Runtira.Application.Features.RuntiraFlexibleDataStrategyDto> flexibleDataBuilder, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraLeadActionResultDto> ArchiveLeadAsync(Guid tenantId, Guid leadId, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraLeadActionResultDto> DeleteLeadAsync(Guid tenantId, Guid leadId, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraLeadConversionResultDto> ConvertLeadAsync(Guid tenantId, string organizationName, string preferredLanguage, Dictionary<string, string>? contextFields, Guid leadId, Func<Runtira.Domain.Entities.RuntiraAsset?, Dictionary<string, string>?, Dictionary<string, string>?, Dictionary<string, string>?, string, Runtira.Application.Features.RuntiraFlexibleDataStrategyDto> flexibleDataBuilder, CancellationToken cancellationToken = default);
    }

    public interface IRuntiraReadModelStore
    {
        Task<Runtira.Application.Features.RuntiraWorkspaceSummaryDto?> GetWorkspaceSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraInvoiceComposerDto?> GetInvoiceComposerAsync(Guid tenantId, string countryCode, string regionCode, string preferredLanguage, Runtira.Application.Features.RuntiraLegislationProfileDto? legislationProfile, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Runtira.Application.Features.RuntiraInboxMessageDto>> GetInboxAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Runtira.Application.Features.RuntiraDocumentDto>> GetDocumentsAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraImportWorkspaceDto> GetImportWorkspaceAsync(Guid tenantId, string preferredLanguage, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraLegislationExperienceDto?> GetLegislationExperienceAsync(Guid tenantId, string countryCode, string regionCode, string preferredLanguage, Runtira.Application.Features.RuntiraLegislationProfileDto? legislationProfile, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Runtira.Application.Common.OrganizationAccessOptionDto>> GetOrganizationAccessOptionsAsync(string userEmail, string clerkUserId, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Common.CurrentOrganization?> ResolveCurrentOrganizationAsync(string tenantSlug, string userEmail, string clerkUserId, string userLocale, string regionClaim, string identityName, CancellationToken cancellationToken = default);
        Task<(Guid OrganizationId, string StripeCustomerId)?> GetBillingOrganizationAsync(string tenantSlug, CancellationToken cancellationToken = default);
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
        public string PropertySlug { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
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
        public IReadOnlyList<OrganizationAccessOptionDto> OrganizationOptions { get; set; } = Array.Empty<OrganizationAccessOptionDto>();
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

    public sealed class RuntiraDocumentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime UploadedUtc { get; set; }
        public long SizeBytes { get; set; }
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
        public IReadOnlyList<RuntiraUploadJobDto> UploadJobs { get; set; } = Array.Empty<RuntiraUploadJobDto>();
    }

    public sealed class RuntiraUploadJobDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string QueueName { get; set; } = string.Empty;
        public string BlobPath { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
        public bool RequiresSuperAdminReview { get; set; }
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
        private readonly CurrentOrganization _currentOrganization;
        private readonly ITenantContextAccessor _tenantContextAccessor;
        private readonly IRuntiraAssetWorkspaceStore? _assetWorkspaceStore;
        private readonly IRuntiraLeadWorkspaceStore? _leadWorkspaceStore;
        private readonly IRuntiraReadModelStore? _readModelStore;

        public RuntiraWorkspaceService(CurrentOrganization currentOrganization, ITenantContextAccessor tenantContextAccessor, IRuntiraAssetWorkspaceStore? assetWorkspaceStore = null, IRuntiraLeadWorkspaceStore? leadWorkspaceStore = null, IRuntiraReadModelStore? readModelStore = null)
        {
            _currentOrganization = currentOrganization;
            _tenantContextAccessor = tenantContextAccessor;
            _assetWorkspaceStore = assetWorkspaceStore;
            _leadWorkspaceStore = leadWorkspaceStore;
            _readModelStore = readModelStore;
        }

        public async Task<RuntiraWorkspaceSummaryDto?> GetWorkspaceSummaryAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (tenantId.HasValue && _readModelStore is not null)
            {
                return await _readModelStore.GetWorkspaceSummaryAsync(tenantId.Value, cancellationToken);
            }

            return null;
        }

        public async Task<RuntiraInvoiceComposerDto?> GetInvoiceComposerAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return null;
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            var preferredLanguage = string.IsNullOrWhiteSpace(_currentOrganization.PreferredLanguage) ? "fr-CA" : _currentOrganization.PreferredLanguage;
            RuntiraLegislationProfileDto? legislationProfile = null;

            if (_readModelStore is not null)
            {
                return await _readModelStore.GetInvoiceComposerAsync(tenantId.Value, countryCode, regionCode, preferredLanguage, legislationProfile, cancellationToken);
            }

            return null;
        }

        public async Task<RuntiraLegislationExperienceDto?> GetLegislationExperienceAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return null;
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            if (_readModelStore is not null)
            {
                return await _readModelStore.GetLegislationExperienceAsync(tenantId.Value, countryCode, regionCode, _currentOrganization.PreferredLanguage, null, cancellationToken);
            }

            return null;
        }

        public async Task<IReadOnlyList<RuntiraLeadSummaryDto>> GetLeadsAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (tenantId.HasValue && _leadWorkspaceStore is not null)
            {
                return await _leadWorkspaceStore.GetLeadsAsync(tenantId.Value, cancellationToken);
            }

            return Array.Empty<RuntiraLeadSummaryDto>();
        }

        public async Task<IReadOnlyList<RuntiraLeadConversionCandidateDto>> GetLeadConversionCandidatesAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (tenantId.HasValue && _leadWorkspaceStore is not null)
            {
                return await _leadWorkspaceStore.GetLeadConversionCandidatesAsync(tenantId.Value, cancellationToken);
            }

            return Array.Empty<RuntiraLeadConversionCandidateDto>();
        }

        public async Task<RuntiraLeadFormContextDto?> GetLeadFormContextAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return null;
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            RuntiraLegislationProfileDto? profile = null;

            if (_leadWorkspaceStore is not null)
            {
                return await _leadWorkspaceStore.GetLeadFormContextAsync(tenantId.Value, _currentOrganization.PreferredLanguage, countryCode, regionCode, profile, cancellationToken);
            }

            return null;
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
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return null;
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            RuntiraLegislationProfileDto? profile = null;

            if (_leadWorkspaceStore is not null)
            {
                return await _leadWorkspaceStore.GetLeaseConversionFormContextAsync(tenantId.Value, leadId, _currentOrganization.OrganizationName, countryCode, regionCode, profile, cancellationToken);
            }

            return null;
        }

        public async Task<RuntiraCreateLeadResultDto> CreateLeadAsync(RuntiraCreateLeadRequestDto request, CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return new RuntiraCreateLeadResultDto { ResultCode = "Unavailable" };
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            RuntiraLegislationProfileDto? profile = null;
            var supportedLanguages = profile is null
                ? new List<string> { _currentOrganization.PreferredLanguage }
                : ParseList(profile.SupportedLanguagesJson).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (_leadWorkspaceStore is not null)
            {
                return await _leadWorkspaceStore.CreateLeadAsync(tenantId.Value, _currentOrganization.OrganizationName, _currentOrganization.PreferredLanguage, supportedLanguages, request, BuildFlexibleDataSnapshot, cancellationToken);
            }


            return new RuntiraCreateLeadResultDto { ResultCode = "Unavailable" };
        }

        private static IReadOnlyList<string> ParseList(string json)
            => RuntiraJson.Deserialize<List<string>>(json) ?? new List<string>();

        public async Task<RuntiraLeadConversionResultDto> ConvertLeadAsync(Guid leadId, Dictionary<string, string>? contextFields = null, CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return new RuntiraLeadConversionResultDto { ResultCode = "Unavailable" };
            }

            if (_leadWorkspaceStore is not null)
            {
                return await _leadWorkspaceStore.ConvertLeadAsync(tenantId.Value, _currentOrganization.OrganizationName, _currentOrganization.PreferredLanguage, contextFields, leadId, BuildFlexibleDataSnapshot, cancellationToken);
            }


            return new RuntiraLeadConversionResultDto { ResultCode = "Unavailable" };
        }

        public async Task<RuntiraLeadActionResultDto> ArchiveLeadAsync(Guid leadId, CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (tenantId.HasValue && _leadWorkspaceStore is not null)
            {
                return await _leadWorkspaceStore.ArchiveLeadAsync(tenantId.Value, leadId, cancellationToken);
            }


            return new RuntiraLeadActionResultDto { ResultCode = "Unavailable" };
        }

        public async Task<RuntiraLeadActionResultDto> DeleteLeadAsync(Guid leadId, CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (tenantId.HasValue && _leadWorkspaceStore is not null)
            {
                return await _leadWorkspaceStore.DeleteLeadAsync(tenantId.Value, leadId, cancellationToken);
            }

            return new RuntiraLeadActionResultDto { ResultCode = "Unavailable" };
        }

        public async Task<RuntiraUnitActionResultDto> ManageUnitAsync(Guid unitId, string action, CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (tenantId.HasValue && _assetWorkspaceStore is not null)
            {
                return await _assetWorkspaceStore.ManageUnitAsync(tenantId.Value, unitId, action, cancellationToken);
            }

            return new RuntiraUnitActionResultDto { ResultCode = "Unavailable" };
        }

        public async Task<RuntiraResidentActionResultDto> ManageResidentAsync(Guid residentId, string action, CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (tenantId.HasValue && _assetWorkspaceStore is not null)
            {
                return await _assetWorkspaceStore.ManageResidentAsync(tenantId.Value, residentId, action, cancellationToken);
            }

            return new RuntiraResidentActionResultDto { ResultCode = "Unavailable" };
        }

        public async Task<RuntiraLeaseActionResultDto> ManageLeaseAsync(Guid leaseId, string action, CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (tenantId.HasValue && _assetWorkspaceStore is not null)
            {
                return await _assetWorkspaceStore.ManageLeaseAsync(tenantId.Value, leaseId, action, cancellationToken);
            }

            return new RuntiraLeaseActionResultDto { ResultCode = "Unavailable" };
        }

        public async Task<RuntiraImportValidationContextDto?> GetImportValidationContextAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (!tenantId.HasValue)
            {
                return null;
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            return new RuntiraImportValidationContextDto
            {
                SourceName = "prospects-q3.xlsx",
                JurisdictionDisplayName = $"{countryCode}-{regionCode}",
                Fields =
                [
                    new RuntiraLeadFormFieldDto { Key = "fullName", Required = true, SuggestedValue = "Taylor Morgan" },
                    new RuntiraLeadFormFieldDto { Key = "email", Required = true, SuggestedValue = "taylor@example.com" },
                    new RuntiraLeadFormFieldDto { Key = "preferredLanguage", Required = false, SuggestedValue = _currentOrganization.PreferredLanguage },
                    new RuntiraLeadFormFieldDto { Key = "summary", Required = false, SuggestedValue = "Validated from mock AI import pipeline." }
                ]
            };
        }

        public async Task<RuntiraImportApprovalResultDto> ApproveImportAsync(RuntiraImportApprovalRequestDto request, CancellationToken cancellationToken = default)
        {
            foreach (var field in new[] { "fullName", "email" })
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
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (tenantId.HasValue && _assetWorkspaceStore is not null)
            {
                return await _assetWorkspaceStore.GetAssetWorkspaceAsync(tenantId.Value, cancellationToken);
            }


            return null;
        }

        public async Task<IReadOnlyList<RuntiraInboxMessageDto>> GetInboxAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (tenantId.HasValue && _readModelStore is not null)
            {
                return await _readModelStore.GetInboxAsync(tenantId.Value, cancellationToken);
            }

            return Array.Empty<RuntiraInboxMessageDto>();
        }

        public async Task<IReadOnlyList<RuntiraDocumentDto>> GetDocumentsAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (tenantId.HasValue && _readModelStore is not null)
            {
                return await _readModelStore.GetDocumentsAsync(tenantId.Value, cancellationToken);
            }

            return Array.Empty<RuntiraDocumentDto>();
        }

        public async Task<RuntiraImportWorkspaceDto> GetImportWorkspaceAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
            if (tenantId.HasValue && _readModelStore is not null)
            {
                return await _readModelStore.GetImportWorkspaceAsync(tenantId.Value, _currentOrganization.PreferredLanguage, cancellationToken);
            }

            return CreateFallbackImportWorkspace();
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
                ],
                UploadJobs =
                [
                    new()
                    {
                        Id = Guid.Parse("91919191-aaaa-bbbb-cccc-101010101010"),
                        FileName = "owner-ledger-july.xlsx",
                        OrganizationName = "Runtira Demo Alberta",
                        PropertyName = "1180 17 Ave SW · Atlas 50",
                        QueueName = "manual-import-review",
                        BlobPath = "demo-alberta/uploads/owner-ledger-july.xlsx",
                        Status = "Queued",
                        CreatedUtc = DateTime.UtcNow.AddMinutes(-32),
                        RequiresSuperAdminReview = true
                    },
                    new()
                    {
                        Id = Guid.Parse("92929292-aaaa-bbbb-cccc-111111111111"),
                        FileName = "new-rent-roll.xlsx",
                        OrganizationName = "Runtira Demo Ontario",
                        PropertyName = "25 Carlton Street",
                        QueueName = "manual-import-review",
                        BlobPath = "demo-ontario/uploads/new-rent-roll.xlsx",
                        Status = "Reviewing",
                        CreatedUtc = DateTime.UtcNow.AddHours(-2),
                        RequiresSuperAdminReview = true
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

        private static string EscapeCsv(string? value)
        {
            var normalized = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
            return $"\"{normalized.Replace("\"", "\"\"")}\"";
        }

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
