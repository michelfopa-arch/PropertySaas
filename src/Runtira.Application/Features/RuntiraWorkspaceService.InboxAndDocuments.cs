namespace Runtira.Application.Features
{
    public sealed partial class RuntiraWorkspaceService
    {
        public async Task<IReadOnlyList<RuntiraInboxMessageDto>> GetInboxAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (tenantId.HasValue && _readModelStore is not null)
            {
                return await _readModelStore.GetInboxAsync(tenantId.Value, cancellationToken);
            }

            return Array.Empty<RuntiraInboxMessageDto>();
        }

        public async Task<RuntiraInboxActionResultDto> ManageInboxMessageAsync(Guid messageId, string action, CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (tenantId.HasValue && _readModelStore is not null)
            {
                return await _readModelStore.ManageInboxMessageAsync(tenantId.Value, messageId, action, cancellationToken);
            }

            return new RuntiraInboxActionResultDto { ResultCode = "Unavailable" };
        }

        public async Task<IReadOnlyList<RuntiraDocumentDto>> GetDocumentsAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (tenantId.HasValue && _readModelStore is not null)
            {
                return await _readModelStore.GetDocumentsAsync(tenantId.Value, cancellationToken);
            }

            return Array.Empty<RuntiraDocumentDto>();
        }

        public async Task<RuntiraDocumentActionResultDto> ManageDocumentAsync(Guid documentId, string action, CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (tenantId.HasValue && _readModelStore is not null)
            {
                return await _readModelStore.ManageDocumentAsync(tenantId.Value, documentId, action, cancellationToken);
            }

            return new RuntiraDocumentActionResultDto { ResultCode = "Unavailable" };
        }
    }
}
