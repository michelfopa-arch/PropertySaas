namespace Runtira.Application.Features
{
    public sealed partial class RuntiraWorkspaceService
    {
        public async Task<RuntiraWorkspaceSummaryDto?> GetWorkspaceSummaryAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (tenantId.HasValue && _readModelStore is not null)
            {
                return await _readModelStore.GetWorkspaceSummaryAsync(tenantId.Value, cancellationToken);
            }

            return null;
        }
    }
}
