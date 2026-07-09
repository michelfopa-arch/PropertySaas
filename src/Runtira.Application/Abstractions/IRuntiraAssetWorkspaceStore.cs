namespace Runtira.Application.Abstractions
{
    public interface IRuntiraAssetWorkspaceStore
    {
        Task<Runtira.Application.Features.RuntiraAssetWorkspaceDto?> GetAssetWorkspaceAsync(Guid tenantId, string? propertySlug = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Runtira.Application.Features.RuntiraAssetSummaryDto>> GetAssetsAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Runtira.Application.Features.RuntiraLeaseInvoiceEntryDto>> GetLeaseInvoiceEntriesAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraUnitActionResultDto> ManageUnitAsync(Guid tenantId, Guid unitId, string action, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraResidentActionResultDto> ManageResidentAsync(Guid tenantId, Guid residentId, string action, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraLeaseActionResultDto> ManageLeaseAsync(Guid tenantId, Guid leaseId, string action, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraRentLedgerDto?> GetRentLedgerAsync(Guid tenantId, Guid leaseId, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraRentLedgerActionResultDto> MarkRentPaymentAsync(Guid tenantId, Guid leaseId, DateTime periodMonthUtc, string action, CancellationToken cancellationToken = default);
    }
}
