namespace Runtira.Application.Features
{
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
}
