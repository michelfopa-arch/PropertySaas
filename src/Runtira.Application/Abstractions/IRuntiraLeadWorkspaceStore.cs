namespace Runtira.Application.Abstractions
{
    public interface IRuntiraLeadWorkspaceStore
    {
        Task<IReadOnlyList<Runtira.Application.Features.RuntiraLeadSummaryDto>> GetLeadsAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Runtira.Application.Features.RuntiraLeadConversionCandidateDto>> GetLeadConversionCandidatesAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraLeadFormContextDto> GetLeadFormContextAsync(Guid tenantId, string preferredLanguage, string countryCode, string regionCode, Runtira.Application.Features.RuntiraLegislationProfileDto? profile, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraLeaseConversionFormContextDto?> GetLeaseConversionFormContextAsync(Guid tenantId, Guid leadId, string organizationName, string countryCode, string regionCode, Runtira.Application.Features.RuntiraLegislationProfileDto? profile, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraCreateLeadResultDto> CreateLeadAsync(Guid tenantId, string organizationName, string preferredLanguage, IReadOnlyList<string> supportedLanguages, Runtira.Application.Features.RuntiraCreateLeadRequestDto request, Func<Runtira.Domain.Entities.RuntiraAsset?, Dictionary<string, string>?, Dictionary<string, string>?, Dictionary<string, string>?, string, Runtira.Application.Features.RuntiraFlexibleDataStrategyDto> flexibleDataBuilder, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraLeadActionResultDto> ArchiveLeadAsync(Guid tenantId, Guid leadId, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraLeadActionResultDto> DeleteLeadAsync(Guid tenantId, Guid leadId, CancellationToken cancellationToken = default);
        Task<Runtira.Application.Features.RuntiraLeadConversionResultDto> ConvertLeadAsync(Guid tenantId, string organizationName, string preferredLanguage, Dictionary<string, string>? contextFields, Guid leadId, Func<Runtira.Domain.Entities.RuntiraAsset?, Dictionary<string, string>?, Dictionary<string, string>?, Dictionary<string, string>?, string, Runtira.Application.Features.RuntiraFlexibleDataStrategyDto> flexibleDataBuilder, CancellationToken cancellationToken = default);
    }
}
