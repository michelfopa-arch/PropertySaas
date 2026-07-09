using Runtira.Application.Abstractions;
using Runtira.Infrastructure.Data;
using static Runtira.Infrastructure.Data.CosmosDocumentHelpers;

namespace Runtira.Infrastructure.Mocks
{
    /// <summary>
    /// In-memory implementation of <see cref="IRuntiraReadModelStore"/> used when Cosmos DB
    /// mock mode is enabled. Mirrors <see cref="Runtira.Infrastructure.Data.CosmosReadModelStore"/>
    /// but reads/writes <see cref="MockTenantDataStore"/> instead of a real Cosmos container.
    /// </summary>
    internal sealed class MockReadModelStore : IRuntiraReadModelStore
    {
        private readonly MockTenantDataStore _store;

        public MockReadModelStore(MockTenantDataStore store)
        {
            _store = store;
        }

        public Task<Runtira.Application.Features.RuntiraWorkspaceSummaryDto?> GetWorkspaceSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var organization = _store.FindGlobalById(tenantId.ToString());
            if (organization is null)
            {
                return Task.FromResult<Runtira.Application.Features.RuntiraWorkspaceSummaryDto?>(null);
            }

            var assets = _store.QueryTenant(tenantId, "asset");
            var workflows = _store.QueryTenant(tenantId, "workflowTemplate");
            var quotas = _store.QueryTenant(tenantId, "quotaPolicy");
            var conversationItems = _store.QueryTenant(tenantId, "conversation");
            var archives = _store.QueryTenant(tenantId, "blobArchive");
            var quota = quotas.FirstOrDefault();

            Runtira.Application.Features.RuntiraWorkspaceSummaryDto? result = new()
            {
                TenantId = tenantId,
                OrganizationName = GetString(organization, "name"),
                OrganizationSlug = GetString(organization, "slug"),
                DefaultLocale = GetString(organization, "defaultLocale"),
                BillingPlan = GetString(organization, "billingPlan", "Trial"),
                AssetCount = assets.Count,
                ConversationCount = conversationItems.Count,
                WorkflowTemplateCount = workflows.Count,
                ArchiveCount = archives.Count,
                MonthlyAiLimit = quota is null ? 0 : GetInt(quota, "maxMonthlyAiRequests"),
                AssetLimit = quota is null ? 0 : GetInt(quota, "maxAssets")
            };

            return Task.FromResult(result);
        }

        public Task<Runtira.Application.Features.RuntiraInvoiceComposerDto?> GetInvoiceComposerAsync(Guid tenantId, string countryCode, string regionCode, string preferredLanguage, Runtira.Application.Features.RuntiraLegislationProfileDto? legislationProfile, CancellationToken cancellationToken = default)
        {
            var organization = _store.FindGlobalById(tenantId.ToString());
            if (organization is null)
            {
                return Task.FromResult<Runtira.Application.Features.RuntiraInvoiceComposerDto?>(null);
            }

            var asset = _store.QueryTenant(tenantId, "asset").OrderBy(x => GetString(x, "name")).FirstOrDefault();
            var jurisdiction = _store.QueryTenant(tenantId, "jurisdictionProfile").FirstOrDefault();

            Runtira.Application.Features.RuntiraInvoiceComposerDto? result = new()
            {
                TenantId = tenantId,
                OrganizationName = GetString(organization, "name"),
                OrganizationSlug = GetString(organization, "slug"),
                CountryCode = string.IsNullOrWhiteSpace(countryCode) ? GetString(organization, "countryCode", "CA") : countryCode,
                RegionCode = string.IsNullOrWhiteSpace(regionCode) ? GetString(organization, "regionCode", "AB") : regionCode,
                JurisdictionDisplayName = legislationProfile?.DisplayName ?? $"{countryCode}-{regionCode}",
                SupportedLanguagesJson = legislationProfile?.SupportedLanguagesJson ?? GetString(jurisdiction ?? new CosmosDocument(), "supportedLanguagesJson", "[]"),
                PropertyAddress = asset is null ? string.Empty : GetString(asset, "addressLine1"),
                BillingPeriod = DateTime.UtcNow.ToString("yyyy-MM"),
                MonthlyRent = 2450m,
                AddAutomaticGst = legislationProfile?.AddAutomaticSalesTax ?? false,
                GeneratePdf = legislationProfile?.GeneratePdf ?? true,
                RequiredQuestionsJson = legislationProfile?.RequiredQuestionsJson ?? GetString(jurisdiction ?? new CosmosDocument(), "requiredQuestionsJson", "[]"),
                InvoiceRulesJson = legislationProfile?.InvoiceRulesJson ?? GetString(jurisdiction ?? new CosmosDocument(), "invoiceRulesJson", "{}"),
                SuggestedPrompt = $"Créer une facture {(legislationProfile?.GeneratePdf == false ? string.Empty : "PDF ")}pour {(asset is null ? "ce bien" : GetString(asset, "addressLine1"))} pour la période {DateTime.UtcNow:yyyy-MM} selon la juridiction {legislationProfile?.JurisdictionCode ?? $"{countryCode}-{regionCode}"} en {preferredLanguage}."
            };

            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<Runtira.Application.Features.RuntiraInboxMessageDto>> GetInboxAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var messages = _store.QueryTenant(tenantId, "inboxMessage").OrderByDescending(x => GetDateTime(x, "receivedUtc")).ToList();

            IReadOnlyList<Runtira.Application.Features.RuntiraInboxMessageDto> result = messages.Select(x => new Runtira.Application.Features.RuntiraInboxMessageDto
            {
                Id = ParseGuid(x.id),
                FromEmail = GetString(x, "fromEmail"),
                Subject = GetString(x, "subject"),
                PreviewText = GetString(x, "previewText"),
                Status = GetString(x, "status"),
                Category = GetString(x, "category"),
                ReceivedUtc = DateTime.TryParse(GetString(x, "receivedUtc"), out var receivedUtc) ? receivedUtc : DateTime.MinValue,
                HasAttachments = bool.TryParse(GetString(x, "hasAttachments"), out var hasAttachments) && hasAttachments
            }).ToList();

            return Task.FromResult(result);
        }

        public Task<Runtira.Application.Features.RuntiraInboxActionResultDto> ManageInboxMessageAsync(Guid tenantId, Guid messageId, string action, CancellationToken cancellationToken = default)
        {
            var messageDocument = _store.FindTenantById(tenantId, messageId.ToString());
            if (messageDocument is null)
            {
                return Task.FromResult(new Runtira.Application.Features.RuntiraInboxActionResultDto { ResultCode = "MessageNotFound" });
            }

            var normalizedAction = action?.Trim().ToLowerInvariant() ?? string.Empty;
            var nextStatus = normalizedAction switch
            {
                "markclassified" => "Classified",
                "markresolved" => "Resolved",
                "markpending" => "Pending",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(nextStatus))
            {
                return Task.FromResult(new Runtira.Application.Features.RuntiraInboxActionResultDto { ResultCode = "UnsupportedAction", Status = GetString(messageDocument, "status") });
            }

            SetValue(messageDocument, "status", nextStatus);
            SetValue(messageDocument, "modifiedUtc", DateTime.UtcNow);
            _store.Upsert(messageDocument);
            return Task.FromResult(new Runtira.Application.Features.RuntiraInboxActionResultDto { Success = true, ResultCode = "Updated", Status = nextStatus });
        }

        public Task<IReadOnlyList<Runtira.Application.Features.RuntiraDocumentDto>> GetDocumentsAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var archives = _store.QueryTenant(tenantId, "blobArchive").OrderByDescending(x => GetDateTime(x, "createdUtc")).ToList();

            IReadOnlyList<Runtira.Application.Features.RuntiraDocumentDto> result = archives.Select(x => new Runtira.Application.Features.RuntiraDocumentDto
            {
                Id = ParseGuid(x.id),
                FileName = System.IO.Path.GetFileName(GetString(x, "blobPath")),
                Category = GetString(x, "category"),
                Status = GetString(x, "status", "Archived"),
                UploadedUtc = DateTime.TryParse(GetString(x, "createdUtc"), out var uploadedUtc) ? uploadedUtc : DateTime.MinValue,
                SizeBytes = GetInt(x, "sizeBytes")
            }).ToList();

            return Task.FromResult(result);
        }

        public Task<Runtira.Application.Features.RuntiraDocumentActionResultDto> ManageDocumentAsync(Guid tenantId, Guid documentId, string action, CancellationToken cancellationToken = default)
        {
            var documentDocument = _store.FindTenantById(tenantId, documentId.ToString());
            if (documentDocument is null)
            {
                return Task.FromResult(new Runtira.Application.Features.RuntiraDocumentActionResultDto { ResultCode = "DocumentNotFound" });
            }

            var normalizedAction = action?.Trim().ToLowerInvariant() ?? string.Empty;
            var nextStatus = normalizedAction switch
            {
                "markarchived" => "Archived",
                "markpendingreview" => "PendingReview",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(nextStatus))
            {
                return Task.FromResult(new Runtira.Application.Features.RuntiraDocumentActionResultDto { ResultCode = "UnsupportedAction", Status = GetString(documentDocument, "status", "Archived") });
            }

            SetValue(documentDocument, "status", nextStatus);
            SetValue(documentDocument, "modifiedUtc", DateTime.UtcNow);
            _store.Upsert(documentDocument);
            return Task.FromResult(new Runtira.Application.Features.RuntiraDocumentActionResultDto { Success = true, ResultCode = "Updated", Status = nextStatus });
        }

        public Task<Runtira.Application.Features.RuntiraImportWorkspaceDto> GetImportWorkspaceAsync(Guid tenantId, string preferredLanguage, CancellationToken cancellationToken = default)
        {
            var topLead = _store.QueryTenant(tenantId, "lead").OrderByDescending(x => GetInt(x, "qualificationScore")).ThenBy(x => GetString(x, "fullName")).FirstOrDefault();
            var latestMessage = _store.QueryTenant(tenantId, "inboxMessage").OrderByDescending(x => GetDateTime(x, "receivedUtc")).FirstOrDefault();
            var firstAsset = _store.QueryTenant(tenantId, "asset").OrderBy(x => GetString(x, "name")).FirstOrDefault();

            var sources = new List<Runtira.Application.Features.RuntiraImportSourceDto>
            {
                new()
                {
                    SourceName = "prospects-q3.xlsx",
                    SourceType = "Excel",
                    Status = "MockReady",
                    SuggestedRecordCount = topLead is null ? 12 : Math.Max(12, GetInt(topLead, "qualificationScore") / 5),
                    Summary = topLead is null ? "Extraction simulée de prospects multi-unités prête pour validation." : $"Extraction simulée alignée sur le lead {GetString(topLead, "fullName")} et ses préférences de marché."
                },
                new()
                {
                    SourceName = latestMessage is null ? "lease-renewal.pdf" : GetString(latestMessage, "subject"),
                    SourceType = latestMessage is not null && bool.TryParse(GetString(latestMessage, "hasAttachments"), out var hasAttachments) && hasAttachments ? "PDF" : "Email",
                    Status = latestMessage is null ? "Preview" : GetString(latestMessage, "status"),
                    SuggestedRecordCount = latestMessage is not null && bool.TryParse(GetString(latestMessage, "hasAttachments"), out var inboxHasAttachments) && inboxHasAttachments ? 3 : 1,
                    Summary = latestMessage is null ? "Document mocké prêt pour extraction des champs bail, unité et contact." : $"Source reliée à l’inbox mockée pour classer {GetString(latestMessage, "category")}."
                },
                new()
                {
                    SourceName = firstAsset is null ? "tenant-notes.txt" : GetString(firstAsset, "name"),
                    SourceType = "Text",
                    Status = "NeedsValidation",
                    SuggestedRecordCount = firstAsset is null ? 2 : 4,
                    Summary = firstAsset is null ? "Texte libre converti en données structurées avant archivage JSON." : $"Pré-remplissage métier autour du bien {GetString(firstAsset, "name")}."
                }
            };

            var suggestedFields = new List<Runtira.Application.Features.RuntiraImportFieldSuggestionDto>
            {
                new() { FieldName = "LeadFullName", SuggestedValue = topLead is null ? "Taylor Morgan" : GetString(topLead, "fullName"), ConfidenceScore = 96 },
                new() { FieldName = "LeadEmail", SuggestedValue = topLead is null ? "taylor@example.com" : GetString(topLead, "email"), ConfidenceScore = 94 },
                new() { FieldName = "PreferredLanguage", SuggestedValue = preferredLanguage, ConfidenceScore = 90 }
            };

            var result = new Runtira.Application.Features.RuntiraImportWorkspaceDto
            {
                ActiveRegion = string.Empty,
                ActiveLanguage = preferredLanguage,
                SupportedFormats = new[] { "Excel", "PDF", "Email", "Text" },
                Sources = sources,
                SuggestedFields = suggestedFields
            };

            return Task.FromResult(result);
        }

        public Task<Runtira.Application.Features.RuntiraLegislationExperienceDto?> GetLegislationExperienceAsync(Guid tenantId, string countryCode, string regionCode, string preferredLanguage, Runtira.Application.Features.RuntiraLegislationProfileDto? legislationProfile, CancellationToken cancellationToken = default)
        {
            if (legislationProfile is null)
            {
                return Task.FromResult<Runtira.Application.Features.RuntiraLegislationExperienceDto?>(null);
            }

            var invoiceRules = Runtira.Application.Common.RuntiraJson.Deserialize<Dictionary<string, bool>>(legislationProfile.InvoiceRulesJson)
                ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var visibleInvoiceOptions = new List<string>();

            if (legislationProfile.GeneratePdf)
            {
                visibleInvoiceOptions.Add("GeneratePdf");
            }

            if (invoiceRules.TryGetValue("includePropertyAddress", out var includePropertyAddress) && includePropertyAddress)
            {
                visibleInvoiceOptions.Add("IncludePropertyAddress");
            }

            if (invoiceRules.TryGetValue("includeBillingPeriod", out var includeBillingPeriod) && includeBillingPeriod)
            {
                visibleInvoiceOptions.Add("IncludeBillingPeriod");
            }

            visibleInvoiceOptions.Add(legislationProfile.AddAutomaticSalesTax ? "AddAutomaticSalesTax" : "NoAutomaticSalesTax");

            Runtira.Application.Features.RuntiraLegislationExperienceDto? dto = new()
            {
                JurisdictionCode = legislationProfile.JurisdictionCode,
                DisplayName = legislationProfile.DisplayName,
                CountryCode = legislationProfile.CountryCode,
                RegionCode = legislationProfile.RegionCode,
                PreferredLanguage = preferredLanguage,
                SupportedLanguages = Runtira.Application.Common.RuntiraJson.Deserialize<List<string>>(legislationProfile.SupportedLanguagesJson) ?? new List<string>(),
                RequiredQuestions = Runtira.Application.Common.RuntiraJson.Deserialize<List<string>>(legislationProfile.RequiredQuestionsJson) ?? new List<string>(),
                VisibleInvoiceOptions = visibleInvoiceOptions
            };

            return Task.FromResult(dto);
        }

        public Task<Runtira.Application.Common.CurrentOrganization?> ResolveCurrentOrganizationAsync(string tenantSlug, string userEmail, string clerkUserId, string userLocale, string regionClaim, string identityName, CancellationToken cancellationToken = default)
        {
            var organizations = _store.QueryGlobal("organization");
            var organizationOptionsTask = GetOrganizationAccessOptionsAsync(userEmail, clerkUserId, cancellationToken);
            var organizationOptions = organizationOptionsTask.GetAwaiter().GetResult();
            CosmosDocument? organization = null;
            if (!string.IsNullOrWhiteSpace(tenantSlug))
            {
                organization = organizations.FirstOrDefault(x => string.Equals(GetString(x, "slug"), tenantSlug, StringComparison.OrdinalIgnoreCase));
            }

            var users = _store.QueryGlobal("user");
            var matchedUser = string.IsNullOrWhiteSpace(userEmail) && string.IsNullOrWhiteSpace(clerkUserId)
                ? null
                : users.FirstOrDefault(x =>
                    (!string.IsNullOrWhiteSpace(userEmail) && string.Equals(GetString(x, "email"), userEmail, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(clerkUserId) && string.Equals(GetString(x, "clerkUserId"), clerkUserId, StringComparison.OrdinalIgnoreCase)));

            var allMemberships = _store.QueryGlobal("membership");
            var memberships = matchedUser is null
                ? new List<CosmosDocument>()
                : allMemberships
                    .Where(x => string.Equals(GetString(x, "userId"), matchedUser.id, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => DateTime.TryParse(GetString(x, "lastSelectedUtc"), out var lastSelectedUtc) ? lastSelectedUtc : DateTime.MinValue)
                    .ThenByDescending(x => DateTime.TryParse(GetString(x, "createdUtc"), out var createdUtc) ? createdUtc : DateTime.MinValue)
                    .ToList();

            if (organization is null && memberships.Count > 0)
            {
                var candidateTenantIds = memberships.Select(x => x.tenantId).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
                organization = organizations.FirstOrDefault(x => candidateTenantIds.Contains(x.id));
            }

            organization ??= organizations.OrderBy(x => GetString(x, "name")).FirstOrDefault();
            if (organization is null)
            {
                return Task.FromResult<Runtira.Application.Common.CurrentOrganization?>(null);
            }

            var activeMembership = memberships.FirstOrDefault(x => string.Equals(x.tenantId, organization.id, StringComparison.OrdinalIgnoreCase));
            var accessibleOrganizationCount = memberships.Select(x => x.tenantId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            var isSuperAdmin = (matchedUser is not null && bool.TryParse(GetString(matchedUser, "isSuperAdmin"), out var isUserSuperAdmin) && isUserSuperAdmin)
                || string.Equals(GetString(organization, "ownerEmail"), "michelfopa@gmail.com", StringComparison.OrdinalIgnoreCase)
                || string.Equals(userEmail, "michelfopa@gmail.com", StringComparison.OrdinalIgnoreCase);

            var effectiveLocale = !string.IsNullOrWhiteSpace(userLocale)
                ? userLocale
                : matchedUser is null ? GetString(organization, "defaultLocale") : GetString(matchedUser, "preferredLanguage", GetString(organization, "defaultLocale"));
            var organizationRegionCode = GetString(organization, "regionCode");
            // The organization's own jurisdiction is authoritative once it has been resolved by tenant slug;
            // regionClaim only comes from the visitor's browser Accept-Language header and must never override it.
            var effectiveRegion = !string.IsNullOrWhiteSpace(organizationRegionCode) ? organizationRegionCode : (!string.IsNullOrWhiteSpace(regionClaim) ? regionClaim : "AB");
            var effectiveCountryCode = string.IsNullOrWhiteSpace(GetString(organization, "countryCode")) ? "CA" : GetString(organization, "countryCode");

            Runtira.Application.Common.CurrentOrganization? result = new()
            {
                UserId = matchedUser is null ? Guid.Empty : ParseGuid(matchedUser.id),
                OrganizationId = ParseGuid(organization.id),
                AccessibleOrganizationCount = Math.Max(accessibleOrganizationCount, 1),
                HasSuperAdminOrganizationSelection = isSuperAdmin,
                OrganizationName = GetString(organization, "name"),
                OrganizationSlug = GetString(organization, "slug"),
                UserEmail = !string.IsNullOrWhiteSpace(userEmail) ? userEmail : GetString(organization, "ownerEmail"),
                UserFullName = matchedUser is null ? (string.IsNullOrWhiteSpace(identityName) ? GetString(organization, "ownerEmail") : identityName) : GetString(matchedUser, "fullName", identityName),
                Role = activeMembership is null ? (isSuperAdmin ? "SuperAdmin" : "Owner") : GetString(activeMembership, "role", "Owner"),
                SystemRole = isSuperAdmin ? "SuperAdmin" : "User",
                Province = string.IsNullOrWhiteSpace(effectiveRegion) ? "AB" : effectiveRegion,
                CountryCode = effectiveCountryCode,
                PreferredLanguage = effectiveLocale,
                SubscriptionIsActive = !bool.TryParse(GetString(organization, "isActive"), out var isActive) || isActive,
                TrialExpired = false,
                OrganizationOptions = organizationOptions
            };

            return Task.FromResult(result);
        }

        public Task<(Guid OrganizationId, string StripeCustomerId)?> GetBillingOrganizationAsync(string tenantSlug, CancellationToken cancellationToken = default)
        {
            var organizations = _store.QueryGlobal("organization");
            var organization = organizations.FirstOrDefault(x => string.Equals(GetString(x, "slug"), tenantSlug, StringComparison.OrdinalIgnoreCase));
            (Guid OrganizationId, string StripeCustomerId)? result = organization is null ? null : (ParseGuid(organization.id), GetString(organization, "stripeCustomerId"));
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<Runtira.Application.Common.OrganizationAccessOptionDto>> GetOrganizationAccessOptionsAsync(string userEmail, string clerkUserId, CancellationToken cancellationToken = default)
        {
            var organizations = _store.QueryGlobal("organization");
            var users = _store.QueryGlobal("user");
            var matchedUser = users.FirstOrDefault(x =>
                (!string.IsNullOrWhiteSpace(userEmail) && string.Equals(GetString(x, "email"), userEmail, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(clerkUserId) && string.Equals(GetString(x, "clerkUserId"), clerkUserId, StringComparison.OrdinalIgnoreCase)));
            var isSuperAdmin = matchedUser is not null && bool.TryParse(GetString(matchedUser, "isSuperAdmin"), out var superAdminFlag) && superAdminFlag;

            if (isSuperAdmin)
            {
                IReadOnlyList<Runtira.Application.Common.OrganizationAccessOptionDto> superAdminResult = organizations
                    .OrderBy(x => GetString(x, "name"))
                    .Select(x => new Runtira.Application.Common.OrganizationAccessOptionDto
                    {
                        OrganizationId = ParseGuid(x.id),
                        OrganizationName = GetString(x, "name"),
                        Role = string.Equals(GetString(x, "ownerEmail"), userEmail, StringComparison.OrdinalIgnoreCase) ? "Owner" : "SuperAdmin",
                        Status = bool.TryParse(GetString(x, "isActive"), out var orgIsActive) && orgIsActive ? "Active" : "Inactive"
                    })
                    .ToList();

                return Task.FromResult(superAdminResult);
            }

            if (matchedUser is null)
            {
                return Task.FromResult<IReadOnlyList<Runtira.Application.Common.OrganizationAccessOptionDto>>(Array.Empty<Runtira.Application.Common.OrganizationAccessOptionDto>());
            }

            var memberships = _store.QueryGlobal("membership")
                .Where(x => string.Equals(GetString(x, "userId"), matchedUser.id, StringComparison.OrdinalIgnoreCase))
                .ToList();

            IReadOnlyList<Runtira.Application.Common.OrganizationAccessOptionDto> result = memberships
                .Join(
                    organizations,
                    membership => membership.tenantId,
                    organizationDocument => organizationDocument.id,
                    (membership, organizationDocument) => new Runtira.Application.Common.OrganizationAccessOptionDto
                    {
                        OrganizationId = ParseGuid(organizationDocument.id),
                        OrganizationName = GetString(organizationDocument, "name"),
                        Role = GetString(membership, "role", "Member"),
                        Status = GetString(membership, "status", "Active")
                    })
                .OrderBy(x => x.OrganizationName)
                .ToList();

            return Task.FromResult(result);
        }
    }
}
