namespace Runtira.Application.Features
{
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

    public sealed class RuntiraLeadActionResultDto
    {
        public bool Success { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
    }
}
