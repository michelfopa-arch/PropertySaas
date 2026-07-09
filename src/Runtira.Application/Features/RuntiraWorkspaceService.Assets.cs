namespace Runtira.Application.Features
{
    public sealed partial class RuntiraWorkspaceService
    {
        public async Task<RuntiraAssetWorkspaceDto?> GetAssetWorkspaceAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (tenantId.HasValue && _assetWorkspaceStore is not null)
            {
                return await _assetWorkspaceStore.GetAssetWorkspaceAsync(tenantId.Value, _currentOrganization.PropertySlug, cancellationToken);
            }

            return null;
        }

        public async Task<IReadOnlyList<RuntiraAssetSummaryDto>> GetAssetsAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (tenantId.HasValue && _assetWorkspaceStore is not null)
            {
                return await _assetWorkspaceStore.GetAssetsAsync(tenantId.Value, cancellationToken);
            }

            return Array.Empty<RuntiraAssetSummaryDto>();
        }

        public async Task<RuntiraUnitActionResultDto> ManageUnitAsync(Guid unitId, string action, CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (tenantId.HasValue && _assetWorkspaceStore is not null)
            {
                return await _assetWorkspaceStore.ManageUnitAsync(tenantId.Value, unitId, action, cancellationToken);
            }

            return new RuntiraUnitActionResultDto { ResultCode = "Unavailable" };
        }

        public async Task<RuntiraResidentActionResultDto> ManageResidentAsync(Guid residentId, string action, CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (tenantId.HasValue && _assetWorkspaceStore is not null)
            {
                return await _assetWorkspaceStore.ManageResidentAsync(tenantId.Value, residentId, action, cancellationToken);
            }

            return new RuntiraResidentActionResultDto { ResultCode = "Unavailable" };
        }

        public async Task<RuntiraLeaseActionResultDto> ManageLeaseAsync(Guid leaseId, string action, CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (tenantId.HasValue && _assetWorkspaceStore is not null)
            {
                return await _assetWorkspaceStore.ManageLeaseAsync(tenantId.Value, leaseId, action, cancellationToken);
            }

            return new RuntiraLeaseActionResultDto { ResultCode = "Unavailable" };
        }

        public async Task<RuntiraRentLedgerDto?> GetRentLedgerAsync(Guid leaseId, CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (tenantId.HasValue && _assetWorkspaceStore is not null)
            {
                return await _assetWorkspaceStore.GetRentLedgerAsync(tenantId.Value, leaseId, cancellationToken);
            }

            return null;
        }

        public async Task<RuntiraRentLedgerActionResultDto> MarkRentPaymentAsync(Guid leaseId, DateTime periodMonthUtc, string action, CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (tenantId.HasValue && _assetWorkspaceStore is not null)
            {
                return await _assetWorkspaceStore.MarkRentPaymentAsync(tenantId.Value, leaseId, periodMonthUtc, action, cancellationToken);
            }

            return new RuntiraRentLedgerActionResultDto { ResultCode = "Unavailable" };
        }
    }
}
