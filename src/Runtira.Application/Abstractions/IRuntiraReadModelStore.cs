namespace Runtira.Application.Abstractions
{
    public interface IRuntiraReadModelStore
    {
        Task<Runtira.Application.Features.RuntiraWorkspaceSummaryDto?> GetWorkspaceSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraInvoiceComposerDto?> GetInvoiceComposerAsync(Guid tenantId, string countryCode, string regionCode, string preferredLanguage, Runtira.Application.Features.RuntiraLegislationProfileDto? legislationProfile, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Runtira.Application.Features.RuntiraInboxMessageDto>> GetInboxAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraInboxActionResultDto> ManageInboxMessageAsync(Guid tenantId, Guid messageId, string action, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Runtira.Application.Features.RuntiraDocumentDto>> GetDocumentsAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraDocumentActionResultDto> ManageDocumentAsync(Guid tenantId, Guid documentId, string action, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraImportWorkspaceDto> GetImportWorkspaceAsync(Guid tenantId, string preferredLanguage, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraLegislationExperienceDto?> GetLegislationExperienceAsync(Guid tenantId, string countryCode, string regionCode, string preferredLanguage, Runtira.Application.Features.RuntiraLegislationProfileDto? legislationProfile, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Runtira.Application.Common.OrganizationAccessOptionDto>> GetOrganizationAccessOptionsAsync(string userEmail, string clerkUserId, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Common.CurrentOrganization?> ResolveCurrentOrganizationAsync(string tenantSlug, string userEmail, string clerkUserId, string userLocale, string regionClaim, string identityName, CancellationToken cancellationToken = default);
        Task<(Guid OrganizationId, string StripeCustomerId)?> GetBillingOrganizationAsync(string tenantSlug, CancellationToken cancellationToken = default);
    }
}
