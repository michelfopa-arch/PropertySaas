using Runtira.Application.Abstractions;

namespace Runtira.Application.Features
{
    using Runtira.Application.Common;

    /// <summary>
    /// Central application-facing facade over the tenant's workspace data (assets, leads, invoices,
    /// inbox, documents, imports/exports). Split into partial files by domain area; this file only
    /// holds shared state and the constructor.
    /// </summary>
    public sealed partial class RuntiraWorkspaceService
    {
        private readonly CurrentOrganization _currentOrganization;
        private readonly ITenantContextAccessor _tenantContextAccessor;
        private readonly IRuntiraAssetWorkspaceStore? _assetWorkspaceStore;
        private readonly IRuntiraLeadWorkspaceStore? _leadWorkspaceStore;
        private readonly IRuntiraReadModelStore? _readModelStore;
        private readonly IRentInvoicePdfRenderer? _rentInvoicePdfRenderer;
        private readonly IRentInvoiceArchiveStore? _rentInvoiceArchiveStore;

        public RuntiraWorkspaceService(CurrentOrganization currentOrganization, ITenantContextAccessor tenantContextAccessor, IRuntiraAssetWorkspaceStore? assetWorkspaceStore = null, IRuntiraLeadWorkspaceStore? leadWorkspaceStore = null, IRuntiraReadModelStore? readModelStore = null, IRentInvoicePdfRenderer? rentInvoicePdfRenderer = null, IRentInvoiceArchiveStore? rentInvoiceArchiveStore = null)
        {
            _currentOrganization = currentOrganization;
            _tenantContextAccessor = tenantContextAccessor;
            _assetWorkspaceStore = assetWorkspaceStore;
            _leadWorkspaceStore = leadWorkspaceStore;
            _readModelStore = readModelStore;
            _rentInvoicePdfRenderer = rentInvoicePdfRenderer;
            _rentInvoiceArchiveStore = rentInvoiceArchiveStore;
        }

        private Guid? ResolveTenantId()
            => _tenantContextAccessor.TenantId ?? (_currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId);
    }
}
