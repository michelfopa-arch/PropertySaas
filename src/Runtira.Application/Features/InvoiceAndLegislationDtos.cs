namespace Runtira.Application.Features
{
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
}
