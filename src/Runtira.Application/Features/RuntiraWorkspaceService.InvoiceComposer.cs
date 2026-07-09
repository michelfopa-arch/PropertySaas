namespace Runtira.Application.Features
{
    public sealed partial class RuntiraWorkspaceService
    {
        public async Task<RuntiraInvoiceComposerDto?> GetInvoiceComposerAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (!tenantId.HasValue)
            {
                return null;
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            var preferredLanguage = string.IsNullOrWhiteSpace(_currentOrganization.PreferredLanguage) ? "fr-CA" : _currentOrganization.PreferredLanguage;
            RuntiraLegislationProfileDto? legislationProfile = null;

            if (_readModelStore is not null)
            {
                return await _readModelStore.GetInvoiceComposerAsync(tenantId.Value, countryCode, regionCode, preferredLanguage, legislationProfile, cancellationToken);
            }

            return null;
        }

        public async Task<RuntiraLegislationExperienceDto?> GetLegislationExperienceAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (!tenantId.HasValue)
            {
                return null;
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            if (_readModelStore is not null)
            {
                return await _readModelStore.GetLegislationExperienceAsync(tenantId.Value, countryCode, regionCode, _currentOrganization.PreferredLanguage, null, cancellationToken);
            }

            return null;
        }
    }
}
