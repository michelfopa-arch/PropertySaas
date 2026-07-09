using Runtira.Application.Common;
using Runtira.Domain.Entities;

namespace Runtira.Application.Features
{
    public sealed partial class RuntiraWorkspaceService
    {
        public async Task<IReadOnlyList<RuntiraLeadSummaryDto>> GetLeadsAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (tenantId.HasValue && _leadWorkspaceStore is not null)
            {
                return await _leadWorkspaceStore.GetLeadsAsync(tenantId.Value, cancellationToken);
            }

            return Array.Empty<RuntiraLeadSummaryDto>();
        }

        public async Task<IReadOnlyList<RuntiraLeadConversionCandidateDto>> GetLeadConversionCandidatesAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (tenantId.HasValue && _leadWorkspaceStore is not null)
            {
                return await _leadWorkspaceStore.GetLeadConversionCandidatesAsync(tenantId.Value, cancellationToken);
            }

            return Array.Empty<RuntiraLeadConversionCandidateDto>();
        }

        public async Task<RuntiraLeadFormContextDto?> GetLeadFormContextAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (!tenantId.HasValue)
            {
                return null;
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            RuntiraLegislationProfileDto? profile = null;

            if (_leadWorkspaceStore is not null)
            {
                return await _leadWorkspaceStore.GetLeadFormContextAsync(tenantId.Value, _currentOrganization.PreferredLanguage, countryCode, regionCode, profile, cancellationToken);
            }

            return null;
        }

        public RuntiraFlexibleDataStrategyDto BuildFlexibleDataSnapshot(
            RuntiraAsset? asset,
            Dictionary<string, string>? leadContext,
            Dictionary<string, string>? leaseContext,
            Dictionary<string, string>? residentContext,
            string source)
        {
            var assetContext = new RuntiraAssetContextData
            {
                Source = source,
                Market = $"{_currentOrganization.CountryCode}-{_currentOrganization.Province}",
                SupportsMultiUnit = asset is not null,
                AssetName = asset?.Name ?? string.Empty,
                AssetAddress = asset?.AddressLine1 ?? string.Empty
            };

            var leadPayload = new RuntiraLeadContextData
            {
                Source = source,
                Market = $"{_currentOrganization.CountryCode}-{_currentOrganization.Province}",
                Language = _currentOrganization.PreferredLanguage,
                Fields = leadContext ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };

            var leasePayload = new RuntiraLeaseComplianceData
            {
                Source = source,
                Market = $"{_currentOrganization.CountryCode}-{_currentOrganization.Province}",
                Fields = leaseContext ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };

            var residentPayload = new RuntiraResidentProfileData
            {
                Source = source,
                Language = _currentOrganization.PreferredLanguage,
                Fields = residentContext ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };

            return new RuntiraFlexibleDataStrategyDto
            {
                AssetContextDataJson = RuntiraJson.Serialize(assetContext),
                LeadContextDataJson = RuntiraJson.Serialize(leadPayload),
                LeaseComplianceDataJson = RuntiraJson.Serialize(leasePayload),
                ResidentProfileDataJson = RuntiraJson.Serialize(residentPayload)
            };
        }

        public async Task<RuntiraLeaseConversionFormContextDto?> GetLeaseConversionFormContextAsync(Guid leadId, CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (!tenantId.HasValue)
            {
                return null;
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            RuntiraLegislationProfileDto? profile = null;

            if (_leadWorkspaceStore is not null)
            {
                return await _leadWorkspaceStore.GetLeaseConversionFormContextAsync(tenantId.Value, leadId, _currentOrganization.OrganizationName, countryCode, regionCode, profile, cancellationToken);
            }

            return null;
        }

        public async Task<RuntiraCreateLeadResultDto> CreateLeadAsync(RuntiraCreateLeadRequestDto request, CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (!tenantId.HasValue)
            {
                return new RuntiraCreateLeadResultDto { ResultCode = "Unavailable" };
            }

            var countryCode = string.IsNullOrWhiteSpace(_currentOrganization.CountryCode) ? "CA" : _currentOrganization.CountryCode;
            var regionCode = string.IsNullOrWhiteSpace(_currentOrganization.Province) ? "AB" : _currentOrganization.Province;
            RuntiraLegislationProfileDto? profile = null;
            var supportedLanguages = profile is null
                ? new List<string> { _currentOrganization.PreferredLanguage }
                : ParseList(profile.SupportedLanguagesJson).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (_leadWorkspaceStore is not null)
            {
                return await _leadWorkspaceStore.CreateLeadAsync(tenantId.Value, _currentOrganization.OrganizationName, _currentOrganization.PreferredLanguage, supportedLanguages, request, BuildFlexibleDataSnapshot, cancellationToken);
            }

            return new RuntiraCreateLeadResultDto { ResultCode = "Unavailable" };
        }

        private static IReadOnlyList<string> ParseList(string json)
            => RuntiraJson.Deserialize<List<string>>(json) ?? new List<string>();

        public async Task<RuntiraLeadConversionResultDto> ConvertLeadAsync(Guid leadId, Dictionary<string, string>? contextFields = null, CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (!tenantId.HasValue)
            {
                return new RuntiraLeadConversionResultDto { ResultCode = "Unavailable" };
            }

            if (_leadWorkspaceStore is not null)
            {
                return await _leadWorkspaceStore.ConvertLeadAsync(tenantId.Value, _currentOrganization.OrganizationName, _currentOrganization.PreferredLanguage, contextFields, leadId, BuildFlexibleDataSnapshot, cancellationToken);
            }

            return new RuntiraLeadConversionResultDto { ResultCode = "Unavailable" };
        }

        public async Task<RuntiraLeadActionResultDto> ArchiveLeadAsync(Guid leadId, CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (tenantId.HasValue && _leadWorkspaceStore is not null)
            {
                return await _leadWorkspaceStore.ArchiveLeadAsync(tenantId.Value, leadId, cancellationToken);
            }

            return new RuntiraLeadActionResultDto { ResultCode = "Unavailable" };
        }

        public async Task<RuntiraLeadActionResultDto> DeleteLeadAsync(Guid leadId, CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (tenantId.HasValue && _leadWorkspaceStore is not null)
            {
                return await _leadWorkspaceStore.DeleteLeadAsync(tenantId.Value, leadId, cancellationToken);
            }

            return new RuntiraLeadActionResultDto { ResultCode = "Unavailable" };
        }
    }
}
