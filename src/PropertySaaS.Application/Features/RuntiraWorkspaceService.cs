using Microsoft.EntityFrameworkCore;
using PropertySaaS.Application.Abstractions;
using PropertySaaS.Application.Common;
using PropertySaaS.Domain.Entities;

namespace PropertySaaS.Application.Features;

public sealed class RuntiraWorkspaceSummaryDto
{
    public Guid TenantId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string OrganizationSlug { get; set; } = string.Empty;
    public string DefaultLocale { get; set; } = string.Empty;
    public int AssetCount { get; set; }
    public int ConversationCount { get; set; }
    public int WorkflowTemplateCount { get; set; }
    public int ArchiveCount { get; set; }
    public int MonthlyAiLimit { get; set; }
    public int AssetLimit { get; set; }
}

public sealed class RuntiraQuestionPromptDto
{
    public string Intent { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string RegionCode { get; set; } = string.Empty;
    public string RequiredQuestionsJson { get; set; } = "[]";
    public string ValidationRulesJson { get; set; } = "{}";
}

public sealed class RuntiraWorkspaceService
{
    private readonly IApplicationDbContext _db;
    private readonly CurrentOrganization _currentOrganization;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public RuntiraWorkspaceService(
        IApplicationDbContext db,
        CurrentOrganization currentOrganization,
        ITenantContextAccessor tenantContextAccessor)
    {
        _db = db;
        _currentOrganization = currentOrganization;
        _tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<RuntiraWorkspaceSummaryDto?> GetWorkspaceSummaryAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = ResolveTenantId();
        if (!tenantId.HasValue)
        {
            return null;
        }

        var organization = await _db.RuntiraOrganizations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tenantId.Value, cancellationToken);

        if (organization is null)
        {
            return null;
        }

        var quota = await _db.RuntiraQuotaPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value, cancellationToken);

        return new RuntiraWorkspaceSummaryDto
        {
            TenantId = organization.Id,
            OrganizationName = organization.Name,
            OrganizationSlug = organization.Slug,
            DefaultLocale = organization.DefaultLocale,
            AssetCount = await _db.RuntiraAssets.CountAsync(x => x.TenantId == tenantId.Value, cancellationToken),
            ConversationCount = await _db.RuntiraConversations.CountAsync(x => x.TenantId == tenantId.Value, cancellationToken),
            WorkflowTemplateCount = await _db.RuntiraWorkflowTemplates.CountAsync(x => x.TenantId == tenantId.Value, cancellationToken),
            ArchiveCount = await _db.RuntiraBlobArchives.CountAsync(x => x.TenantId == tenantId.Value, cancellationToken),
            MonthlyAiLimit = quota?.MaxMonthlyAiRequests ?? 0,
            AssetLimit = quota?.MaxAssets ?? 0
        };
    }

    public async Task<RuntiraQuestionPromptDto?> GetRequiredQuestionsAsync(string intent, CancellationToken cancellationToken = default)
    {
        var tenantId = ResolveTenantId();
        if (!tenantId.HasValue)
        {
            return null;
        }

        var jurisdictionProfile = await _db.RuntiraJurisdictionProfiles
            .AsNoTracking()
            .OrderByDescending(x => x.RegionCode)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value, cancellationToken);

        if (jurisdictionProfile is null)
        {
            return null;
        }

        return new RuntiraQuestionPromptDto
        {
            Intent = intent,
            CountryCode = jurisdictionProfile.CountryCode,
            RegionCode = jurisdictionProfile.RegionCode,
            RequiredQuestionsJson = jurisdictionProfile.RequiredQuestionsJson,
            ValidationRulesJson = jurisdictionProfile.ValidationRulesJson
        };
    }

    private Guid? ResolveTenantId()
    {
        if (_tenantContextAccessor.TenantId.HasValue)
        {
            return _tenantContextAccessor.TenantId;
        }

        return _currentOrganization.OrganizationId == Guid.Empty ? null : _currentOrganization.OrganizationId;
    }
}
