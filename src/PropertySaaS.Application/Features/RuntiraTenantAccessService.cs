using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PropertySaaS.Application.Abstractions;
using PropertySaaS.Application.Common;
using PropertySaaS.Domain.Entities;

namespace PropertySaaS.Application.Features;

public sealed class RuntiraTenantCreateResult
{
    public Guid TenantId { get; set; }
    public string TenantSlug { get; set; } = string.Empty;
}

public sealed class RuntiraTenantAccessService
{
    private readonly IApplicationDbContext _db;
    private readonly CurrentOrganization _currentOrganization;

    public RuntiraTenantAccessService(IApplicationDbContext db, CurrentOrganization currentOrganization)
    {
        _db = db;
        _currentOrganization = currentOrganization;
    }

    public async Task<List<OrganizationAccessOptionDto>> GetAccessibleTenantsAsync(CancellationToken cancellationToken = default)
    {
        var runtiraUser = await ResolveRuntiraUserAsync(cancellationToken);
        if (runtiraUser is null)
        {
            return [];
        }

        var memberships = await _db.RuntiraMemberships
            .AsNoTracking()
            .Where(x => x.UserId == runtiraUser.Id && x.Status == "Active")
            .Join(
                _db.RuntiraOrganizations.AsNoTracking(),
                membership => membership.TenantId,
                tenant => tenant.Id,
                (membership, tenant) => new OrganizationAccessOptionDto
                {
                    OrganizationId = tenant.Id,
                    OrganizationName = tenant.Name,
                    IsDemo = false,
                    Role = membership.Role,
                    Status = membership.Status
                })
            .OrderBy(x => x.OrganizationName)
            .ToListAsync(cancellationToken);

        if (memberships.Count > 0)
        {
            return memberships;
        }

        return await GetAllTenantsForSuperAdminAsync(cancellationToken);
    }

    public async Task<List<OrganizationAccessOptionDto>> GetAllTenantsForSuperAdminAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentOrganization.IsSuperAdmin)
        {
            return [];
        }

        return await _db.RuntiraOrganizations
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new OrganizationAccessOptionDto
            {
                OrganizationId = x.Id,
                OrganizationName = x.Name,
                IsDemo = false,
                Role = "SuperAdmin",
                Status = x.IsActive ? "Active" : "Inactive"
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<RuntiraTenantCreateResult> CreateTenantAsync(string name, string regionCode, bool loadDemoData = false, CancellationToken cancellationToken = default)
    {
        if (!_currentOrganization.IsAuthenticated || string.IsNullOrWhiteSpace(_currentOrganization.UserEmail))
        {
            throw new InvalidOperationException("You must be authenticated to create a workspace.");
        }

        var normalizedName = string.IsNullOrWhiteSpace(name)
            ? throw new InvalidOperationException("Workspace name is required.")
            : name.Trim();

        var selectedRegionCode = string.IsNullOrWhiteSpace(regionCode) ? "ON" : regionCode.Trim().ToUpperInvariant();
        var profile = JurisdictionCatalog.GetProfile(selectedRegionCode);
        var runtiraUser = await EnsureRuntiraUserAsync(cancellationToken);
        var baseSlug = BuildSlug(normalizedName);
        var hashSuffix = BuildOwnerHashSuffix(_currentOrganization.UserEmail);
        var candidateSlug = string.IsNullOrWhiteSpace(baseSlug) ? $"workspace-{hashSuffix}" : $"{baseSlug}-{hashSuffix}";
        var slug = candidateSlug;
        var duplicateIndex = 1;

        while (await _db.RuntiraOrganizations.AsNoTracking().AnyAsync(x => x.Slug == slug, cancellationToken))
        {
            duplicateIndex++;
            slug = $"{candidateSlug}-{duplicateIndex}";
        }

        var tenantId = Guid.NewGuid();
        var locale = ToLocaleCode(_currentOrganization.PreferredLanguage, profile.DefaultLanguage);
        var createdUtc = DateTime.UtcNow;
        var supportedLanguagesJson = JsonSerializer.Serialize(profile.SupportedLanguages);
        var legalProfileJson = JsonSerializer.Serialize(new
        {
            jurisdiction = profile.ProvinceCode,
            supportedLanguages = profile.SupportedLanguages
        });
        var additionalSettingsJson = JsonSerializer.Serialize(new
        {
            tenantMode = "subdomain",
            archive = "blob",
            isDemo = loadDemoData
        });

        var organization = new RuntiraOrganization
        {
            Id = tenantId,
            Name = normalizedName,
            Slug = slug,
            OwnerEmail = _currentOrganization.UserEmail.Trim().ToLowerInvariant(),
            DefaultLocale = locale,
            CountryCode = profile.CountryCode,
            RegionCode = profile.ProvinceCode,
            TimeZone = "America/Toronto",
            LegalProfileJson = legalProfileJson,
            AdditionalSettingsJson = additionalSettingsJson,
            IsActive = true,
            CreatedUtc = createdUtc
        };

        _db.RuntiraOrganizations.Add(organization);
        _db.RuntiraMemberships.Add(new RuntiraMembership
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = runtiraUser.Id,
            Role = "Owner",
            Status = "Active",
            LastSelectedUtc = createdUtc,
            CreatedUtc = createdUtc
        });

        _db.RuntiraJurisdictionProfiles.Add(new RuntiraJurisdictionProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CountryCode = profile.CountryCode,
            RegionCode = profile.ProvinceCode,
            SupportedLanguagesJson = supportedLanguagesJson,
            RequiredQuestionsJson = loadDemoData
                ? "[\"address\",\"unitCount\",\"ownerName\",\"rentSchedule\"]"
                : "[\"address\",\"unitCount\",\"ownerName\"]",
            ValidationRulesJson = "{\"unitCount\":{\"required\":true,\"min\":1}}",
            InvoiceRulesJson = "{\"supportsMonthlyInvoice\":true}",
            AssetRulesJson = "{\"supportsMultiUnit\":true}",
            MaintenanceRulesJson = "{\"supportInboxClassification\":true}",
            CreatedUtc = createdUtc
        });

        _db.RuntiraQuotaPolicies.Add(new RuntiraQuotaPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MaxAssets = loadDemoData ? 25 : 100,
            MaxDocuments = 1000,
            MaxMonthlyAiRequests = 5000,
            MaxBlobStorageMb = 2048,
            MaxActiveWorkflows = 50,
            EnforceHardLimit = true,
            CreatedUtc = createdUtc
        });

        _db.RuntiraWorkflowTemplates.Add(new RuntiraWorkflowTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Create asset from natural language",
            TriggerType = "CreateAsset",
            Description = "Guides the user through required jurisdiction-aware questions.",
            PromptTemplate = "Ask only for missing required fields and confirm before creation.",
            RequiredQuestionsJson = "[\"address\",\"unitCount\",\"jurisdiction\",\"rentSchedule\"]",
            ValidationSchemaJson = "{\"unitCount\":{\"min\":1}}",
            IsActive = true,
            CreatedUtc = createdUtc
        });

        if (loadDemoData)
        {
            SeedDemoWorkspace(tenantId, profile, createdUtc, _currentOrganization.UserEmail);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new RuntiraTenantCreateResult
        {
            TenantId = tenantId,
            TenantSlug = slug
        };
    }

    private async Task<RuntiraUser?> ResolveRuntiraUserAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentOrganization.UserEmail))
        {
            return null;
        }

        var normalizedEmail = _currentOrganization.UserEmail.Trim().ToLowerInvariant();
        return await _db.RuntiraUsers.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
    }

    private async Task<RuntiraUser> EnsureRuntiraUserAsync(CancellationToken cancellationToken)
    {
        var runtiraUser = await ResolveRuntiraUserAsync(cancellationToken);
        if (runtiraUser is not null)
        {
            if (string.IsNullOrWhiteSpace(runtiraUser.FullName) && !string.IsNullOrWhiteSpace(_currentOrganization.UserFullName))
            {
                runtiraUser.FullName = _currentOrganization.UserFullName;
            }

            if (string.IsNullOrWhiteSpace(runtiraUser.PreferredLanguage))
            {
                runtiraUser.PreferredLanguage = ToLocaleCode(_currentOrganization.PreferredLanguage, "en");
            }

            return runtiraUser;
        }

        runtiraUser = new RuntiraUser
        {
            Id = Guid.NewGuid(),
            ClerkUserId = string.Empty,
            Email = _currentOrganization.UserEmail.Trim().ToLowerInvariant(),
            FullName = string.IsNullOrWhiteSpace(_currentOrganization.UserFullName) ? _currentOrganization.UserEmail : _currentOrganization.UserFullName,
            PreferredLanguage = ToLocaleCode(_currentOrganization.PreferredLanguage, "en"),
            IsSuperAdmin = _currentOrganization.IsSuperAdmin,
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        };

        _db.RuntiraUsers.Add(runtiraUser);
        return runtiraUser;
    }

    private void SeedDemoWorkspace(Guid tenantId, JurisdictionProfile profile, DateTime createdUtc, string userEmail)
    {
        var conversationId = Guid.NewGuid();

        _db.RuntiraAssets.Add(new RuntiraAsset
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Runtira Demo Asset",
            AssetType = "Property",
            AddressLine1 = "100 King Street West",
            City = "Toronto",
            RegionCode = profile.ProvinceCode,
            CountryCode = profile.CountryCode,
            UnitCount = 3,
            LegalProfileJson = "{\"requiredQuestions\":[\"address\",\"unitCount\",\"rentSchedule\"]}",
            AdditionalDataJson = "{\"source\":\"onboarding-demo\"}",
            WorkflowSummaryJson = "{\"status\":\"ready\"}",
            CreatedUtc = createdUtc
        });

        _db.RuntiraConversations.Add(new RuntiraConversation
        {
            Id = conversationId,
            TenantId = tenantId,
            Channel = "Chat",
            Subject = "Create a 3-unit property",
            Locale = ToLocaleCode(_currentOrganization.PreferredLanguage, profile.DefaultLanguage),
            Status = "Open",
            Intent = "CreateAsset",
            JurisdictionCode = $"{profile.CountryCode}-{profile.ProvinceCode}",
            LastMessageUtc = createdUtc,
            SummaryJson = "{\"nextQuestion\":\"What is the full property address?\"}",
            CreatedUtc = createdUtc
        });

        _db.RuntiraMessages.Add(new RuntiraMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConversationId = conversationId,
            Direction = "Incoming",
            AuthorType = "User",
            Content = "I want to create a property with 3 units.",
            StructuredPayloadJson = "{\"intent\":\"CreateAsset\"}",
            RequiresAction = true,
            CreatedByEmail = userEmail,
            CreatedUtc = createdUtc
        });

        _db.RuntiraBlobArchives.Add(new RuntiraBlobArchive
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BlobPath = $"{BuildSlug(_currentOrganization.UserEmail)}/activity/{createdUtc:yyyy/MM/dd}/create-asset.json",
            ContentType = "application/json",
            Category = "Activity",
            MetadataJson = "{\"intent\":\"CreateAsset\"}",
            SizeBytes = 256,
            SourceSystem = "onboarding-demo",
            Hash = $"demo-{tenantId:N}",
            CreatedUtc = createdUtc
        });
    }

    private static string ToLocaleCode(string? preferredLanguage, string fallback)
    {
        var source = string.IsNullOrWhiteSpace(preferredLanguage) ? fallback : preferredLanguage;
        var normalized = source.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized.ToLowerInvariant();
    }

    private static string BuildOwnerHashSuffix(string email)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant()));
        return Convert.ToHexString(hash)[..6].ToLowerInvariant();
    }

    private static string BuildSlug(string value)
    {
        var chars = value
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();

        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }
}
