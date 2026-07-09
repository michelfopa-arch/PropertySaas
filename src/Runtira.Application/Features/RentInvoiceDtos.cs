namespace Runtira.Application.Features
{
    public sealed class RuntiraRentLedgerEntryDto
    {
        public DateTime PeriodMonthUtc { get; set; }
        public decimal AmountDue { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime? PaidUtc { get; set; }
    }

    public sealed class RuntiraRentLedgerDto
    {
        public Guid LeaseId { get; set; }
        public decimal MonthlyRent { get; set; }
        public int PaidCount { get; set; }
        public int LateCount { get; set; }
        public int PendingCount { get; set; }
        public decimal OutstandingBalance { get; set; }
        public IReadOnlyList<RuntiraRentLedgerEntryDto> Entries { get; set; } = Array.Empty<RuntiraRentLedgerEntryDto>();
    }

    public sealed class RuntiraRentLedgerActionResultDto
    {
        public bool Success { get; set; }
        public string ResultCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public sealed class RuntiraRentInvoiceDto
    {
        public Guid LeaseId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string PropertyAddress { get; set; } = string.Empty;
        public string ResidentName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public decimal MonthlyRent { get; set; }
        public string BillingPeriod { get; set; } = string.Empty;
        public DateTime LeaseStartUtc { get; set; }
        public DateTime? LeaseEndUtc { get; set; }
        public DateTime PeriodMonthUtc { get; set; }
        public string JurisdictionDisplayName { get; set; } = string.Empty;
        public string RegionCode { get; set; } = string.Empty;
        public bool AddAutomaticSalesTax { get; set; }
    }

    public sealed class RuntiraLeaseInvoiceEntryDto
    {
        public Guid LeaseId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string PropertyAddress { get; set; } = string.Empty;
        public string PropertySlug { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public string ResidentName { get; set; } = string.Empty;
        public decimal MonthlyRent { get; set; }
        public string BillingPeriod { get; set; } = string.Empty;
        public string LeaseStatus { get; set; } = string.Empty;
        public DateTime LeaseStartUtc { get; set; }
        public DateTime? LeaseEndUtc { get; set; }
        public IReadOnlyList<DateTime> AvailableMonths { get; set; } = Array.Empty<DateTime>();
    }
}
