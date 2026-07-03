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
        DbSet<RuntiraConversation> RuntiraConversations { get; }
        DbSet<RuntiraMessage> RuntiraMessages { get; }
        DbSet<RuntiraWorkflowTemplate> RuntiraWorkflowTemplates { get; }
        DbSet<RuntiraBlobArchive> RuntiraBlobArchives { get; }
        DbSet<RuntiraJurisdictionProfile> RuntiraJurisdictionProfiles { get; }
        DbSet<RuntiraQuotaPolicy> RuntiraQuotaPolicies { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
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
            var db = _serviceProvider.GetService<IApplicationDbContext>();
            if (db is null)
            {
                return null;
            }

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
            var db = _serviceProvider.GetService<IApplicationDbContext>();
            if (db is null)
            {
                return null;
            }

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
            var db = _serviceProvider.GetService<IApplicationDbContext>();
            var legislationCatalog = _serviceProvider.GetService<ILegislationCatalog>();
            if (db is null || legislationCatalog is null)
            {
                return null;
            }

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

        private static IReadOnlyList<string> ParseList(string json)
            => JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

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
    }

    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddRuntiraApplication(this IServiceCollection services)
        {
            services.AddTransient<RuntiraWorkspaceService>();
            services.AddScoped<ITenantContextAccessor>(_ => new TenantContext { TenantId = null, BypassTenantFilter = false });
            services.AddScoped<CurrentOrganization>();
            return services;
        }
    }
}
