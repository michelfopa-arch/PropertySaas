using System.Net;
using Microsoft.Azure.Cosmos;
using Runtira.Application.Abstractions;
using Runtira.Infrastructure.Options;
using static Runtira.Infrastructure.Data.CosmosDocumentHelpers;

namespace Runtira.Infrastructure.Data
{
    internal sealed class CosmosReadModelStore : IRuntiraReadModelStore
    {
        private readonly CosmosClient _cosmosClient;
        private readonly CosmosOptions _options;

        public CosmosReadModelStore(CosmosClient cosmosClient, CosmosOptions options)
        {
            _cosmosClient = cosmosClient;
            _options = options;
        }

        public async Task<Runtira.Application.Features.RuntiraWorkspaceSummaryDto?> GetWorkspaceSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var organization = await ReadGlobalItemAsync(database.GetContainer("Organizations"), tenantId, cancellationToken);
            if (organization is null)
            {
                return null;
            }

            var tenantCore = database.GetContainer("TenantCore");
            var conversations = database.GetContainer("Conversations");
            var blobArchives = database.GetContainer("BlobArchives");

            var assets = await QueryManyAsync(tenantCore, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'asset'", cancellationToken);
            var workflows = await QueryManyAsync(tenantCore, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'workflowTemplate'", cancellationToken);
            var quotas = await QueryManyAsync(tenantCore, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'quotaPolicy'", cancellationToken);
            var conversationItems = await QueryManyAsync(conversations, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'conversation'", cancellationToken);
            var archives = await QueryManyAsync(blobArchives, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'blobArchive'", cancellationToken);
            var quota = quotas.FirstOrDefault();

            return new Runtira.Application.Features.RuntiraWorkspaceSummaryDto
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
        }

        public async Task<Runtira.Application.Features.RuntiraInvoiceComposerDto?> GetInvoiceComposerAsync(Guid tenantId, string countryCode, string regionCode, string preferredLanguage, Runtira.Application.Features.RuntiraLegislationProfileDto? legislationProfile, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var organization = await ReadGlobalItemAsync(database.GetContainer("Organizations"), tenantId, cancellationToken);
            if (organization is null)
            {
                return null;
            }

            var tenantCore = database.GetContainer("TenantCore");
            var asset = await QuerySingleAsync(tenantCore, tenantId, "SELECT TOP 1 * FROM c WHERE c.tenantId = @tenantId AND c.type = 'asset' ORDER BY c.data.name", cancellationToken);
            var jurisdiction = await QuerySingleAsync(tenantCore, tenantId, "SELECT TOP 1 * FROM c WHERE c.tenantId = @tenantId AND c.type = 'jurisdictionProfile'", cancellationToken);

            return new Runtira.Application.Features.RuntiraInvoiceComposerDto
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
        }

        public async Task<IReadOnlyList<Runtira.Application.Features.RuntiraInboxMessageDto>> GetInboxAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var inbox = database.GetContainer("Inbox");
            var messages = await QueryManyAsync(inbox, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'inboxMessage' ORDER BY c.data.receivedUtc DESC", cancellationToken);

            return messages.Select(x => new Runtira.Application.Features.RuntiraInboxMessageDto
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
        }

        public async Task<Runtira.Application.Features.RuntiraInboxActionResultDto> ManageInboxMessageAsync(Guid tenantId, Guid messageId, string action, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var inbox = database.GetContainer("Inbox");
            var messageDocument = await ReadItemAsync(inbox, messageId, tenantId, cancellationToken);
            if (messageDocument is null)
            {
                return new Runtira.Application.Features.RuntiraInboxActionResultDto { ResultCode = "MessageNotFound" };
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
                return new Runtira.Application.Features.RuntiraInboxActionResultDto { ResultCode = "UnsupportedAction", Status = GetString(messageDocument, "status") };
            }

            SetValue(messageDocument, "status", nextStatus);
            SetValue(messageDocument, "modifiedUtc", DateTime.UtcNow);
            await inbox.UpsertItemAsync(messageDocument, new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);
            return new Runtira.Application.Features.RuntiraInboxActionResultDto { Success = true, ResultCode = "Updated", Status = nextStatus };
        }

        public async Task<IReadOnlyList<Runtira.Application.Features.RuntiraDocumentDto>> GetDocumentsAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var blobArchives = database.GetContainer("BlobArchives");
            var archives = await QueryManyAsync(blobArchives, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'blobArchive' ORDER BY c.data.createdUtc DESC", cancellationToken);

            return archives.Select(x => new Runtira.Application.Features.RuntiraDocumentDto
            {
                Id = ParseGuid(x.id),
                FileName = System.IO.Path.GetFileName(GetString(x, "blobPath")),
                Category = GetString(x, "category"),
                Status = GetString(x, "status", "Archived"),
                UploadedUtc = DateTime.TryParse(GetString(x, "createdUtc"), out var uploadedUtc) ? uploadedUtc : DateTime.MinValue,
                SizeBytes = GetInt(x, "sizeBytes")
            }).ToList();
        }

        public async Task<Runtira.Application.Features.RuntiraDocumentActionResultDto> ManageDocumentAsync(Guid tenantId, Guid documentId, string action, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var blobArchives = database.GetContainer("BlobArchives");
            var documentDocument = await ReadItemAsync(blobArchives, documentId, tenantId, cancellationToken);
            if (documentDocument is null)
            {
                return new Runtira.Application.Features.RuntiraDocumentActionResultDto { ResultCode = "DocumentNotFound" };
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
                return new Runtira.Application.Features.RuntiraDocumentActionResultDto { ResultCode = "UnsupportedAction", Status = GetString(documentDocument, "status", "Archived") };
            }

            SetValue(documentDocument, "status", nextStatus);
            SetValue(documentDocument, "modifiedUtc", DateTime.UtcNow);
            await blobArchives.UpsertItemAsync(documentDocument, new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);
            return new Runtira.Application.Features.RuntiraDocumentActionResultDto { Success = true, ResultCode = "Updated", Status = nextStatus };
        }

        public async Task<Runtira.Application.Features.RuntiraImportWorkspaceDto> GetImportWorkspaceAsync(Guid tenantId, string preferredLanguage, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var tenantCore = database.GetContainer("TenantCore");
            var inbox = database.GetContainer("Inbox");

            var topLead = (await QueryManyAsync(tenantCore, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'lead' ORDER BY c.data.qualificationScore DESC, c.data.fullName", cancellationToken)).FirstOrDefault();
            var latestMessage = (await QueryManyAsync(inbox, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'inboxMessage' ORDER BY c.data.receivedUtc DESC", cancellationToken)).FirstOrDefault();
            var firstAsset = (await QueryManyAsync(tenantCore, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'asset' ORDER BY c.data.name", cancellationToken)).FirstOrDefault();

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

            return new Runtira.Application.Features.RuntiraImportWorkspaceDto
            {
                ActiveRegion = string.Empty,
                ActiveLanguage = preferredLanguage,
                SupportedFormats = new[] { "Excel", "PDF", "Email", "Text" },
                Sources = sources,
                SuggestedFields = suggestedFields
            };
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

            var dto = new Runtira.Application.Features.RuntiraLegislationExperienceDto
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

            return Task.FromResult<Runtira.Application.Features.RuntiraLegislationExperienceDto?>(dto);
        }

        public async Task<global::Runtira.Application.Common.CurrentOrganization?> ResolveCurrentOrganizationAsync(string tenantSlug, string userEmail, string clerkUserId, string userLocale, string regionClaim, string identityName, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var organizationsContainer = database.GetContainer("Organizations");
            var usersContainer = database.GetContainer("Users");
            var tenantCore = database.GetContainer("TenantCore");

            var organizations = await QueryGlobalManyAsync(organizationsContainer, "SELECT * FROM c", cancellationToken);
            var organizationOptions = await GetOrganizationAccessOptionsAsync(userEmail, clerkUserId, cancellationToken);
            CosmosDocument? organization = null;
            if (!string.IsNullOrWhiteSpace(tenantSlug))
            {
                organization = organizations.FirstOrDefault(x => string.Equals(GetString(x, "slug"), tenantSlug, StringComparison.OrdinalIgnoreCase));
            }

            var matchedUser = string.IsNullOrWhiteSpace(userEmail) && string.IsNullOrWhiteSpace(clerkUserId)
                ? null
                : (await QueryGlobalManyAsync(usersContainer, "SELECT * FROM c", cancellationToken)).FirstOrDefault(x =>
                    (!string.IsNullOrWhiteSpace(userEmail) && string.Equals(GetString(x, "email"), userEmail, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(clerkUserId) && string.Equals(GetString(x, "clerkUserId"), clerkUserId, StringComparison.OrdinalIgnoreCase)));

            var memberships = matchedUser is null
                ? new List<CosmosDocument>()
                : await QueryManyAsync(tenantCore, ParseGuid(organizations.FirstOrDefault()?.id ?? string.Empty), "SELECT * FROM c WHERE c.type = 'membership'", cancellationToken);

            if (matchedUser is not null)
            {
                memberships = (await QueryGlobalTenantCoreManyAsync(database, "membership", cancellationToken))
                    .Where(x => string.Equals(GetString(x, "userId"), matchedUser.id, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => DateTime.TryParse(GetString(x, "lastSelectedUtc"), out var lastSelectedUtc) ? lastSelectedUtc : DateTime.MinValue)
                    .ThenByDescending(x => DateTime.TryParse(GetString(x, "createdUtc"), out var createdUtc) ? createdUtc : DateTime.MinValue)
                    .ToList();
            }

            if (organization is null && memberships.Count > 0)
            {
                var candidateTenantIds = memberships.Select(x => x.tenantId).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
                organization = organizations.FirstOrDefault(x => candidateTenantIds.Contains(x.id));
            }

            organization ??= organizations.OrderBy(x => GetString(x, "name")).FirstOrDefault();
            if (organization is null)
            {
                return null;
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

            return new Runtira.Application.Common.CurrentOrganization
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
        }

        public async Task<(Guid OrganizationId, string StripeCustomerId)?> GetBillingOrganizationAsync(string tenantSlug, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var organizations = await QueryGlobalManyAsync(database.GetContainer("Organizations"), "SELECT * FROM c", cancellationToken);
            var organization = organizations.FirstOrDefault(x => string.Equals(GetString(x, "slug"), tenantSlug, StringComparison.OrdinalIgnoreCase));
            return organization is null ? null : (ParseGuid(organization.id), GetString(organization, "stripeCustomerId"));
        }

        public async Task<IReadOnlyList<Runtira.Application.Common.OrganizationAccessOptionDto>> GetOrganizationAccessOptionsAsync(string userEmail, string clerkUserId, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var organizations = await QueryGlobalManyAsync(database.GetContainer("Organizations"), "SELECT * FROM c", cancellationToken);
            var users = await QueryGlobalManyAsync(database.GetContainer("Users"), "SELECT * FROM c", cancellationToken);
            var matchedUser = users.FirstOrDefault(x =>
                (!string.IsNullOrWhiteSpace(userEmail) && string.Equals(GetString(x, "email"), userEmail, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(clerkUserId) && string.Equals(GetString(x, "clerkUserId"), clerkUserId, StringComparison.OrdinalIgnoreCase)));
            var isSuperAdmin = matchedUser is not null && bool.TryParse(GetString(matchedUser, "isSuperAdmin"), out var superAdminFlag) && superAdminFlag;

            if (isSuperAdmin)
            {
                return organizations
                    .OrderBy(x => GetString(x, "name"))
                    .Select(x => new Runtira.Application.Common.OrganizationAccessOptionDto
                    {
                        OrganizationId = ParseGuid(x.id),
                        OrganizationName = GetString(x, "name"),
                        Role = string.Equals(GetString(x, "ownerEmail"), userEmail, StringComparison.OrdinalIgnoreCase) ? "Owner" : "SuperAdmin",
                        Status = bool.TryParse(GetString(x, "isActive"), out var orgIsActive) && orgIsActive ? "Active" : "Inactive"
                    })
                    .ToList();
            }

            if (matchedUser is null)
            {
                return Array.Empty<Runtira.Application.Common.OrganizationAccessOptionDto>();
            }

            var memberships = (await QueryGlobalTenantCoreManyAsync(database, "membership", cancellationToken))
                .Where(x => string.Equals(GetString(x, "userId"), matchedUser.id, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return memberships
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
        }

        private static async Task<List<CosmosDocument>> QueryGlobalManyAsync(Container container, string queryText, CancellationToken cancellationToken)
        {
            var iterator = container.GetItemQueryIterator<CosmosDocument>(new QueryDefinition(queryText));
            var results = new List<CosmosDocument>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                results.AddRange(response);
            }

            return results;
        }

        private async Task<List<CosmosDocument>> QueryGlobalTenantCoreManyAsync(Database database, string type, CancellationToken cancellationToken)
        {
            var organizations = await QueryGlobalManyAsync(database.GetContainer("Organizations"), "SELECT * FROM c", cancellationToken);
            var tenantCore = database.GetContainer("TenantCore");
            var all = new List<CosmosDocument>();
            foreach (var organization in organizations)
            {
                var tenantId = ParseGuid(organization.id);
                if (tenantId == Guid.Empty)
                {
                    continue;
                }

                all.AddRange(await QueryManyAsync(tenantCore, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = @type", cancellationToken, ("@type", (object)type)));
            }

            return all;
        }

        private static async Task<CosmosDocument?> ReadGlobalItemAsync(Container container, Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await container.ReadItemAsync<CosmosDocument>(id.ToString(), new PartitionKey(id.ToString()), cancellationToken: cancellationToken);
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }
}
