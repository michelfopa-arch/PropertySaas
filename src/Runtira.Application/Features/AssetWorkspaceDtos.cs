namespace Runtira.Application.Features
{
    public sealed class RuntiraUnitSummaryDto
    {
        public Guid Id { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public string UnitType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal MarketRent { get; set; }
        public bool CanDelete { get; set; }
    }

    public sealed class RuntiraUnitActionResultDto
    {
        public bool Success { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
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

    public sealed class RuntiraResidentActionResultDto
    {
        public bool Success { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string ResidentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public sealed class RuntiraLeaseSummaryDto
    {
        public Guid Id { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public string ResidentName { get; set; } = string.Empty;
        public decimal MonthlyRent { get; set; }
        public string Status { get; set; } = string.Empty;
        public string BillingPeriod { get; set; } = string.Empty;
        public DateTime LeaseStartUtc { get; set; }
        public DateTime? LeaseEndUtc { get; set; }
        public string ComplianceDataJson { get; set; } = "{}";
        public RuntiraLeaseComplianceData? ComplianceData { get; set; }
    }

    public sealed class RuntiraLeaseActionResultDto
    {
        public bool Success { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public string LeaseStatus { get; set; } = string.Empty;
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

    public sealed class RuntiraAssetSummaryDto
    {
        public Guid AssetId { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string AssetAddress { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string PropertySlug { get; set; } = string.Empty;
        public int UnitCount { get; set; }
        public int OccupiedUnitCount { get; set; }
        public int ActiveLeaseCount { get; set; }
        public int ActiveResidentCount { get; set; }
        public decimal MonthlyRevenue { get; set; }
    }
}
