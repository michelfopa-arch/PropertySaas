using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Runtira.Application.Abstractions;
using Runtira.Domain.Common;
using Runtira.Domain.Entities;

namespace Runtira.Infrastructure.Options
{
    public sealed class ClerkOptions
    {
        public string Authority { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string SignInUrl { get; set; } = string.Empty;
        public string SignUpUrl { get; set; } = string.Empty;
        public string UnauthorizedSignInUrl { get; set; } = string.Empty;
        public string UserProfileUrl { get; set; } = string.Empty;
    }

    public sealed class StripeOptions
    {
        public string SecretKey { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string StarterPriceId { get; set; } = string.Empty;
        public string GrowthPriceId { get; set; } = string.Empty;
        public string ProPriceId { get; set; } = string.Empty;
    }

    public sealed class AzureBlobOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string TenantArchiveContainer { get; set; } = "tenant-archive";
    }

    public sealed class Microsoft365Options
    {
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string SupportMailbox { get; set; } = "support@runtira.com";
    }

    public sealed class AiOptions
    {
        public string Provider { get; set; } = "MicrosoftAgentFramework";
        public string ModelFast { get; set; } = "gpt-4.1-mini";
        public string ModelReasoning { get; set; } = "gpt-4.1";
    }

    public sealed class CosmosOptions
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = "runtiradb";
        public bool Enabled { get; set; }
        public bool MockModeEnabled { get; set; } = true;
        public int SharedAutoscaleMaxThroughput { get; set; } = 4000;
        public int TenantCoreAutoscaleMaxThroughput { get; set; } = 4000;
        public int MessagesAutoscaleMaxThroughput { get; set; } = 4000;
        public int InboxAutoscaleMaxThroughput { get; set; } = 2000;
        public int BillingAutoscaleMaxThroughput { get; set; } = 2000;
        public int OrganizationsAutoscaleMaxThroughput { get; set; } = 1000;
        public int UsersAutoscaleMaxThroughput { get; set; } = 1000;
        public int ConversationsAutoscaleMaxThroughput { get; set; } = 1000;
        public int BlobArchivesAutoscaleMaxThroughput { get; set; } = 1000;
    }

    public sealed class ResendOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string FromEmail { get; set; } = "onboarding@resend.dev";
        public string FromName { get; set; } = "PropertySaaS";
        public string SupportEmail { get; set; } = "michelfopa@gmail.com";
    }
}

namespace Runtira.Infrastructure.Data
{
    using System.Net;
    using Runtira.Infrastructure.Options;
    using static CosmosDocumentHelpers;

    public sealed record CosmosContainerDefinition(string Name, string PartitionKeyPath);

    public static class CosmosSchemaDefinition
    {
        public static IReadOnlyList<CosmosContainerDefinition> Build(CosmosOptions options)
            =>
            [
                new("Organizations", "/id"),
                new("Users", "/id"),
                new("TenantCore", "/tenantId"),
                new("Conversations", "/tenantId"),
                new("Messages", "/tenantId"),
                new("Inbox", "/tenantId"),
                new("BlobArchives", "/tenantId"),
                new("Billing", "/tenantId")
            ];
    }

    internal sealed class CosmosDocument
    {
        public string id { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public string? tenantId { get; set; }
        public Dictionary<string, object?> data { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    internal static class CosmosDocumentHelpers
    {
        public static async Task<CosmosDocument?> ReadItemAsync(Container container, Guid id, Guid tenantId, CancellationToken cancellationToken)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            try
            {
                var response = await container.ReadItemAsync<CosmosDocument>(id.ToString(), new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public static async Task<CosmosDocument?> QuerySingleAsync(Container container, Guid tenantId, string queryText, CancellationToken cancellationToken, params (string Name, object Value)[] extraParameters)
            => (await QueryManyAsync(container, tenantId, queryText, cancellationToken, extraParameters)).FirstOrDefault();

        public static async Task<List<CosmosDocument>> QueryManyAsync(Container container, Guid tenantId, string queryText, CancellationToken cancellationToken, params (string Name, object Value)[] extraParameters)
        {
            var queryDefinition = new QueryDefinition(queryText).WithParameter("@tenantId", tenantId.ToString());
            foreach (var parameter in extraParameters)
            {
                queryDefinition = queryDefinition.WithParameter(parameter.Name, parameter.Value);
            }

            var iterator = container.GetItemQueryIterator<CosmosDocument>(queryDefinition, requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(tenantId.ToString())
            });

            var results = new List<CosmosDocument>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                results.AddRange(response);
            }

            return results;
        }

        public static Guid ParseGuid(string? value)
            => Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty;

        public static Guid GetGuid(CosmosDocument document, string fieldName)
            => Guid.TryParse(GetString(document, fieldName), out var parsed) ? parsed : Guid.Empty;

        public static string GetString(CosmosDocument document, string fieldName, string defaultValue = "")
            => document.data.TryGetValue(fieldName, out var value) && value is not null
                ? value.ToString() ?? defaultValue
                : defaultValue;

        public static int GetInt(CosmosDocument document, string fieldName)
            => document.data.TryGetValue(fieldName, out var value) && value is not null && int.TryParse(value.ToString(), out var parsed) ? parsed : 0;

        public static decimal GetDecimal(CosmosDocument document, string fieldName)
            => document.data.TryGetValue(fieldName, out var value) && value is not null && decimal.TryParse(value.ToString(), out var parsed) ? parsed : 0m;

        public static void SetValue(CosmosDocument document, string fieldName, object? value)
            => document.data[fieldName] = value;

        public static CosmosDocument CreateOrganizationDocument(string id, string name, string slug, string ownerEmail, string defaultLocale, string countryCode, string regionCode, string timeZone, string legalProfileJson)
            => new()
            {
                id = id,
                type = "organization",
                data = new Dictionary<string, object?>
                {
                    ["name"] = name,
                    ["slug"] = slug,
                    ["ownerEmail"] = ownerEmail,
                    ["defaultLocale"] = defaultLocale,
                    ["countryCode"] = countryCode,
                    ["regionCode"] = regionCode,
                    ["timeZone"] = timeZone,
                    ["legalProfileJson"] = legalProfileJson,
                    ["additionalSettingsJson"] = "{\"tenantMode\":\"path\",\"archive\":\"blob\"}",
                    ["stripeCustomerId"] = string.Empty,
                    ["stripeSubscriptionId"] = string.Empty,
                    ["billingPlan"] = "Trial",
                    ["isActive"] = true,
                    ["createdUtc"] = "2026-07-03T00:00:00Z",
                    ["modifiedUtc"] = null
                }
            };

        public static CosmosDocument CreateUserDocument(string id, string clerkUserId, string email, string fullName, string preferredLanguage, bool isSuperAdmin)
            => new()
            {
                id = id,
                type = "user",
                data = new Dictionary<string, object?>
                {
                    ["clerkUserId"] = clerkUserId,
                    ["email"] = email,
                    ["fullName"] = fullName,
                    ["preferredLanguage"] = preferredLanguage,
                    ["isSuperAdmin"] = isSuperAdmin,
                    ["isActive"] = true,
                    ["createdUtc"] = "2026-07-03T00:00:00Z",
                    ["modifiedUtc"] = null
                }
            };

        public static CosmosDocument CreateTenantDocument(string type, string id, string tenantId, Dictionary<string, object?> data)
            => new()
            {
                id = id,
                type = type,
                tenantId = tenantId,
                data = data
            };

        public static List<string> ParseList(string? json)
            => Runtira.Application.Common.RuntiraJson.Deserialize<List<string>>(json) ?? new List<string>();

        public static (IReadOnlyList<string> VisibleFields, HashSet<string> RequiredFields) ParseContextFormDefinition(string? json, string formKey, IReadOnlyList<string> defaultVisibleFields, IReadOnlyList<string> defaultRequiredFields)
        {
            var payload = Runtira.Application.Common.RuntiraJson.Deserialize<Dictionary<string, Dictionary<string, List<string>>>>(json);
            if (payload is not null
                && payload.TryGetValue(formKey, out var formDefinition)
                && formDefinition.TryGetValue("visibleFields", out var visibleFields)
                && visibleFields.Count > 0)
            {
                var required = formDefinition.TryGetValue("requiredFields", out var requiredFields)
                    ? requiredFields
                    : defaultRequiredFields.ToList();
                return (visibleFields, new HashSet<string>(required, StringComparer.OrdinalIgnoreCase));
            }

            return (defaultVisibleFields, new HashSet<string>(defaultRequiredFields, StringComparer.OrdinalIgnoreCase));
        }
    }

    internal sealed class CosmosAssetWorkspaceStore : IRuntiraAssetWorkspaceStore
    {
        private readonly CosmosClient _cosmosClient;
        private readonly CosmosOptions _options;

        public CosmosAssetWorkspaceStore(CosmosClient cosmosClient, CosmosOptions options)
        {
            _cosmosClient = cosmosClient;
            _options = options;
        }

        public async Task<Runtira.Application.Features.RuntiraAssetWorkspaceDto?> GetAssetWorkspaceAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var coreContainer = database.GetContainer("TenantCore");

            var assetDocument = await QuerySingleAsync(coreContainer, tenantId, "SELECT TOP 1 * FROM c WHERE c.tenantId = @tenantId AND c.type = 'asset' ORDER BY c.data.name", cancellationToken);
            if (assetDocument is null)
            {
                return null;
            }

            var assetId = ParseGuid(assetDocument.id);
            var units = await QueryManyAsync(coreContainer, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'unit' AND c.data.assetId = @assetId ORDER BY c.data.unitCode", cancellationToken, ("@assetId", (object)assetId.ToString()));
            var residents = await QueryManyAsync(coreContainer, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'resident' ORDER BY c.data.fullName", cancellationToken);
            var leases = await QueryManyAsync(coreContainer, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'lease' AND c.data.assetId = @assetId", cancellationToken, ("@assetId", (object)assetId.ToString()));

            var residentById = residents.ToDictionary(x => ParseGuid(x.id));
            var unitById = units.ToDictionary(x => ParseGuid(x.id));
            var activeLeaseUnitIds = leases
                .Where(x => string.Equals(GetString(x, "status"), "Active", StringComparison.OrdinalIgnoreCase))
                .Select(x => GetGuid(x, "unitId"))
                .Where(x => x != Guid.Empty)
                .ToHashSet();

            return new Runtira.Application.Features.RuntiraAssetWorkspaceDto
            {
                AssetId = assetId,
                AssetName = GetString(assetDocument, "name"),
                AssetAddress = GetString(assetDocument, "addressLine1"),
                AssetType = GetString(assetDocument, "assetType"),
                UnitCount = GetInt(assetDocument, "unitCount"),
                TotalResidentCount = residents.Count,
                TotalLeaseCount = leases.Count,
                ContextDataJson = GetString(assetDocument, "additionalDataJson", "{}"),
                ContextData = Runtira.Application.Common.RuntiraJson.Deserialize<Runtira.Application.Features.RuntiraAssetContextData>(GetString(assetDocument, "additionalDataJson", "{}")),
                Units = units.Select(unit => new Runtira.Application.Features.RuntiraUnitSummaryDto
                {
                    Id = ParseGuid(unit.id),
                    UnitCode = GetString(unit, "unitCode"),
                    UnitType = GetString(unit, "unitType"),
                    Status = GetString(unit, "status"),
                    MarketRent = GetDecimal(unit, "marketRent"),
                    CanDelete = !activeLeaseUnitIds.Contains(ParseGuid(unit.id))
                }).ToList(),
                Residents = residents.Select(resident => new Runtira.Application.Features.RuntiraResidentSummaryDto
                {
                    Id = ParseGuid(resident.id),
                    FullName = GetString(resident, "fullName"),
                    Email = GetString(resident, "email"),
                    PreferredLanguage = GetString(resident, "preferredLanguage"),
                    Status = GetString(resident, "status"),
                    ProfileDataJson = GetString(resident, "notesJson", "{}"),
                    ProfileData = Runtira.Application.Common.RuntiraJson.Deserialize<Runtira.Application.Features.RuntiraResidentProfileData>(GetString(resident, "notesJson", "{}"))
                }).ToList(),
                Leases = leases.Select(lease =>
                {
                    var unitId = GetGuid(lease, "unitId");
                    var residentId = GetGuid(lease, "residentId");
                    unitById.TryGetValue(unitId, out var unitDocument);
                    residentById.TryGetValue(residentId, out var residentDocument);

                    var complianceDataJson = GetString(lease, "termsJson", "{}");
                    return new Runtira.Application.Features.RuntiraLeaseSummaryDto
                    {
                        Id = ParseGuid(lease.id),
                        UnitCode = unitDocument is null ? string.Empty : GetString(unitDocument, "unitCode"),
                        ResidentName = residentDocument is null ? string.Empty : GetString(residentDocument, "fullName"),
                        MonthlyRent = GetDecimal(lease, "monthlyRent"),
                        Status = GetString(lease, "status"),
                        BillingPeriod = GetString(lease, "billingPeriod"),
                        ComplianceDataJson = complianceDataJson,
                        ComplianceData = Runtira.Application.Common.RuntiraJson.Deserialize<Runtira.Application.Features.RuntiraLeaseComplianceData>(complianceDataJson)
                    };
                }).ToList()
            };
        }

        public async Task<Runtira.Application.Features.RuntiraUnitActionResultDto> ManageUnitAsync(Guid tenantId, Guid unitId, string action, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var coreContainer = database.GetContainer("TenantCore");
            var unitDocument = await ReadItemAsync(coreContainer, unitId, tenantId, cancellationToken);
            if (unitDocument is null)
            {
                return new Runtira.Application.Features.RuntiraUnitActionResultDto { ResultCode = "UnitNotFound" };
            }

            var unitCode = GetString(unitDocument, "unitCode");
            var normalizedAction = action?.Trim().ToLowerInvariant() ?? string.Empty;
            var activeLeaseExists = (await QueryManyAsync(coreContainer, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'lease' AND c.data.unitId = @unitId AND UPPER(c.data.status) = 'ACTIVE'", cancellationToken, ("@unitId", (object)unitId.ToString()))).Count > 0;

            switch (normalizedAction)
            {
                case "markmaintenance":
                    SetValue(unitDocument, "status", "Maintenance");
                    SetValue(unitDocument, "modifiedUtc", DateTime.UtcNow);
                    await coreContainer.UpsertItemAsync(unitDocument, new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);
                    return new Runtira.Application.Features.RuntiraUnitActionResultDto { Success = true, ResultCode = "Updated", UnitCode = unitCode, Status = "Maintenance" };
                case "markavailable":
                    SetValue(unitDocument, "status", "Available");
                    SetValue(unitDocument, "modifiedUtc", DateTime.UtcNow);
                    await coreContainer.UpsertItemAsync(unitDocument, new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);
                    return new Runtira.Application.Features.RuntiraUnitActionResultDto { Success = true, ResultCode = "Updated", UnitCode = unitCode, Status = "Available" };
                case "delete":
                    if (activeLeaseExists)
                    {
                        return new Runtira.Application.Features.RuntiraUnitActionResultDto { ResultCode = "UnitHasActiveLease", UnitCode = unitCode, Status = GetString(unitDocument, "status") };
                    }

                    await coreContainer.DeleteItemAsync<CosmosDocument>(unitId.ToString(), new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);
                    return new Runtira.Application.Features.RuntiraUnitActionResultDto { Success = true, ResultCode = "Deleted", UnitCode = unitCode, Status = "Deleted" };
                default:
                    return new Runtira.Application.Features.RuntiraUnitActionResultDto { ResultCode = "UnsupportedAction", UnitCode = unitCode, Status = GetString(unitDocument, "status") };
            }
        }

        public async Task<Runtira.Application.Features.RuntiraResidentActionResultDto> ManageResidentAsync(Guid tenantId, Guid residentId, string action, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var coreContainer = database.GetContainer("TenantCore");
            var residentDocument = await ReadItemAsync(coreContainer, residentId, tenantId, cancellationToken);
            if (residentDocument is null)
            {
                return new Runtira.Application.Features.RuntiraResidentActionResultDto { ResultCode = "ResidentNotFound" };
            }

            var residentName = GetString(residentDocument, "fullName");
            var normalizedAction = action?.Trim().ToLowerInvariant() ?? string.Empty;
            var nextStatus = normalizedAction switch
            {
                "markwatch" => "Watch",
                "markactive" => "Active",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(nextStatus))
            {
                return new Runtira.Application.Features.RuntiraResidentActionResultDto { ResultCode = "UnsupportedAction", ResidentName = residentName, Status = GetString(residentDocument, "status") };
            }

            SetValue(residentDocument, "status", nextStatus);
            SetValue(residentDocument, "modifiedUtc", DateTime.UtcNow);
            await coreContainer.UpsertItemAsync(residentDocument, new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);
            return new Runtira.Application.Features.RuntiraResidentActionResultDto { Success = true, ResultCode = "Updated", ResidentName = residentName, Status = nextStatus };
        }

        public async Task<Runtira.Application.Features.RuntiraLeaseActionResultDto> ManageLeaseAsync(Guid tenantId, Guid leaseId, string action, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var coreContainer = database.GetContainer("TenantCore");
            var leaseDocument = await ReadItemAsync(coreContainer, leaseId, tenantId, cancellationToken);
            if (leaseDocument is null)
            {
                return new Runtira.Application.Features.RuntiraLeaseActionResultDto { ResultCode = "LeaseNotFound" };
            }

            var unitDocument = await ReadItemAsync(coreContainer, GetGuid(leaseDocument, "unitId"), tenantId, cancellationToken);
            var unitCode = unitDocument is null ? string.Empty : GetString(unitDocument, "unitCode");
            var normalizedAction = action?.Trim().ToLowerInvariant() ?? string.Empty;
            var nextStatus = normalizedAction switch
            {
                "markreview" => "Review",
                "markactive" => "Active",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(nextStatus))
            {
                return new Runtira.Application.Features.RuntiraLeaseActionResultDto { ResultCode = "UnsupportedAction", UnitCode = unitCode, LeaseStatus = GetString(leaseDocument, "status") };
            }

            SetValue(leaseDocument, "status", nextStatus);
            SetValue(leaseDocument, "modifiedUtc", DateTime.UtcNow);
            await coreContainer.UpsertItemAsync(leaseDocument, new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);
            return new Runtira.Application.Features.RuntiraLeaseActionResultDto { Success = true, ResultCode = "Updated", UnitCode = unitCode, LeaseStatus = nextStatus };
        }

    }

    internal sealed class CosmosSeedService : IHostedService
    {
        private readonly CosmosClient? _cosmosClient;
        private readonly CosmosOptions _options;
        private readonly ILogger<CosmosSeedService> _logger;

        public CosmosSeedService(CosmosClient? cosmosClient, CosmosOptions options, ILogger<CosmosSeedService> logger)
        {
            _cosmosClient = cosmosClient;
            _options = options;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_cosmosClient is null || !_options.Enabled)
            {
                return;
            }

            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var organizations = database.GetContainer("Organizations");
            var users = database.GetContainer("Users");
            var tenantCore = database.GetContainer("TenantCore");

            foreach (var document in BuildSeedDocuments())
            {
                var container = document.type switch
                {
                    "organization" => organizations,
                    "user" => users,
                    _ => tenantCore
                };

                var partitionKey = document.type switch
                {
                    "organization" => new PartitionKey(document.id),
                    "user" => new PartitionKey(document.id),
                    _ => new PartitionKey(document.tenantId)
                };

                await container.UpsertItemAsync(document, partitionKey, cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Cosmos DB seed data for Runtira demo tenants is ready.");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static IReadOnlyList<CosmosDocument> BuildSeedDocuments()
        {
            const string albertaTenantId = "aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb";
            const string ontarioTenantId = "abababab-1111-2222-3333-bcbcbcbcbcbc";
            const string texasTenantId = "acacacac-1111-2222-3333-bdbdbdbdbdbd";
            const string ownerUserId = "cccccccc-1111-2222-3333-dddddddddddd";

            var documents = new List<CosmosDocument>
            {
                CreateOrganizationDocument(albertaTenantId, "Runtira Demo Alberta", "demo-alberta", "michelfopa@gmail.com", "fr-CA", "CA", "AB", "America/Edmonton", "{\"jurisdiction\":\"CA-AB\",\"supports\":[\"fr-CA\",\"en-CA\",\"es-MX\"]}"),
                CreateOrganizationDocument(ontarioTenantId, "Runtira Demo Ontario", "demo-ontario", "michelfopa@gmail.com", "en-CA", "CA", "ON", "America/Toronto", "{\"jurisdiction\":\"CA-ON\",\"supports\":[\"en-CA\",\"fr-CA\"]}"),
                CreateOrganizationDocument(texasTenantId, "Runtira Demo Texas", "demo-texas", "michelfopa@gmail.com", "en-US", "US", "TX", "America/Chicago", "{\"jurisdiction\":\"US-TX\",\"supports\":[\"en-US\",\"es-MX\"]}"),
                CreateUserDocument(ownerUserId, "runtira_demo_owner", "michelfopa@gmail.com", "Michel Fopa", "fr-CA", true),
                CreateTenantDocument("membership", "eeeeeeee-1111-2222-3333-ffffffffffff", albertaTenantId, new Dictionary<string, object?> { ["userId"] = ownerUserId, ["role"] = "Owner", ["status"] = "Active", ["lastSelectedUtc"] = null, ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("membership", "efefefef-1111-2222-3333-f0f0f0f0f0f0", ontarioTenantId, new Dictionary<string, object?> { ["userId"] = ownerUserId, ["role"] = "Owner", ["status"] = "Active", ["lastSelectedUtc"] = null, ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("membership", "f1f1f1f1-1111-2222-3333-f2f2f2f2f2f2", texasTenantId, new Dictionary<string, object?> { ["userId"] = ownerUserId, ["role"] = "Owner", ["status"] = "Active", ["lastSelectedUtc"] = null, ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("asset", "11111111-aaaa-bbbb-cccc-222222222222", albertaTenantId, new Dictionary<string, object?> { ["name"] = "1180 17 Ave SW · Atlas 50", ["assetType"] = "Property", ["addressLine1"] = "1180 17 Ave SW", ["city"] = "Calgary", ["regionCode"] = "AB", ["countryCode"] = "CA", ["unitCount"] = 50, ["legalProfileJson"] = "{\"requiredQuestions\":[\"address\",\"period\",\"monthlyRent\"]}", ["additionalDataJson"] = "{\"source\":\"seed\",\"market\":\"Calgary\",\"supportsMultiUnit\":true,\"assetName\":\"1180 17 Ave SW · Atlas 50\",\"assetAddress\":\"1180 17 Ave SW\"}", ["workflowSummaryJson"] = "{\"status\":\"ready\"}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("asset", "13131313-aaaa-bbbb-cccc-242424242424", ontarioTenantId, new Dictionary<string, object?> { ["name"] = "25 Carlton Street", ["assetType"] = "Property", ["addressLine1"] = "25 Carlton Street", ["city"] = "Toronto", ["regionCode"] = "ON", ["countryCode"] = "CA", ["unitCount"] = 20, ["legalProfileJson"] = "{\"requiredQuestions\":[\"propertyAddress\",\"billingPeriod\",\"tenantName\",\"monthlyRent\"]}", ["additionalDataJson"] = "{\"source\":\"seed\",\"market\":\"Toronto\",\"supportsMultiUnit\":true,\"assetName\":\"25 Carlton Street\",\"assetAddress\":\"25 Carlton Street\"}", ["workflowSummaryJson"] = "{\"status\":\"ready\"}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("asset", "15151515-aaaa-bbbb-cccc-262626262626", texasTenantId, new Dictionary<string, object?> { ["name"] = "2400 McKinney Avenue", ["assetType"] = "Property", ["addressLine1"] = "2400 McKinney Avenue", ["city"] = "Dallas", ["regionCode"] = "TX", ["countryCode"] = "US", ["unitCount"] = 18, ["legalProfileJson"] = "{\"requiredQuestions\":[\"propertyAddress\",\"billingPeriod\",\"ownerName\",\"monthlyRent\"]}", ["additionalDataJson"] = "{\"source\":\"seed\",\"market\":\"Dallas\",\"supportsMultiUnit\":true,\"assetName\":\"2400 McKinney Avenue\",\"assetAddress\":\"2400 McKinney Avenue\"}", ["workflowSummaryJson"] = "{\"status\":\"ready\"}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("unit", "23232323-aaaa-bbbb-cccc-454545454545", ontarioTenantId, new Dictionary<string, object?> { ["assetId"] = "13131313-aaaa-bbbb-cccc-242424242424", ["unitCode"] = "1204", ["unitType"] = "Condo", ["status"] = "Occupied", ["marketRent"] = 3100m, ["additionalDataJson"] = "{\"bedrooms\":1}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("unit", "25252525-aaaa-bbbb-cccc-474747474747", texasTenantId, new Dictionary<string, object?> { ["assetId"] = "15151515-aaaa-bbbb-cccc-262626262626", ["unitCode"] = "8B", ["unitType"] = "Apartment", ["status"] = "Occupied", ["marketRent"] = 2800m, ["additionalDataJson"] = "{\"bedrooms\":2}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("resident", "27272727-aaaa-bbbb-cccc-494949494949", albertaTenantId, new Dictionary<string, object?> { ["fullName"] = "Amelie Gagnon", ["email"] = "amelie.gagnon@example.com", ["phoneNumber"] = "+1-403-555-0147", ["preferredLanguage"] = "fr-CA", ["status"] = "Active", ["notesJson"] = "{\"leaseIntent\":\"renewal\"}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("resident", "29292929-aaaa-bbbb-cccc-515151515151", ontarioTenantId, new Dictionary<string, object?> { ["fullName"] = "Lucas Martin", ["email"] = "lucas.martin@example.com", ["phoneNumber"] = "+1-416-555-0120", ["preferredLanguage"] = "en-CA", ["status"] = "Active", ["notesJson"] = "{\"preferredChannel\":\"email\"}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("resident", "31313131-aaaa-bbbb-cccc-535353535353", texasTenantId, new Dictionary<string, object?> { ["fullName"] = "Maya Rodriguez", ["email"] = "maya.rodriguez@example.com", ["phoneNumber"] = "+1-214-555-0191", ["preferredLanguage"] = "es-MX", ["status"] = "Active", ["notesJson"] = "{\"moveInWindow\":\"2026-08\"}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("lease", "41414141-aaaa-bbbb-cccc-616161616161", albertaTenantId, new Dictionary<string, object?> { ["assetId"] = "11111111-aaaa-bbbb-cccc-222222222222", ["unitId"] = "10000000-0000-0000-0000-000000000001", ["residentId"] = "27272727-aaaa-bbbb-cccc-494949494949", ["leaseStartUtc"] = "2026-01-01T00:00:00Z", ["leaseEndUtc"] = "2026-12-31T00:00:00Z", ["monthlyRent"] = 2450m, ["billingPeriod"] = "Monthly", ["status"] = "Active", ["termsJson"] = "{\"deposit\":2450}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("lease", "43434343-aaaa-bbbb-cccc-636363636363", ontarioTenantId, new Dictionary<string, object?> { ["assetId"] = "13131313-aaaa-bbbb-cccc-242424242424", ["unitId"] = "23232323-aaaa-bbbb-cccc-454545454545", ["residentId"] = "29292929-aaaa-bbbb-cccc-515151515151", ["leaseStartUtc"] = "2026-03-01T00:00:00Z", ["leaseEndUtc"] = "2027-02-28T00:00:00Z", ["monthlyRent"] = 3100m, ["billingPeriod"] = "Monthly", ["status"] = "Active", ["termsJson"] = "{\"noticeDays\":60}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("lease", "45454545-aaaa-bbbb-cccc-656565656565", texasTenantId, new Dictionary<string, object?> { ["assetId"] = "15151515-aaaa-bbbb-cccc-262626262626", ["unitId"] = "25252525-aaaa-bbbb-cccc-474747474747", ["residentId"] = "31313131-aaaa-bbbb-cccc-535353535353", ["leaseStartUtc"] = "2026-04-01T00:00:00Z", ["leaseEndUtc"] = "2027-03-31T00:00:00Z", ["monthlyRent"] = 2800m, ["billingPeriod"] = "Monthly", ["status"] = "Review", ["termsJson"] = "{\"noticeDays\":45}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("lead", "61616161-aaaa-bbbb-cccc-717171717171", albertaTenantId, new Dictionary<string, object?> { ["assetId"] = "11111111-aaaa-bbbb-cccc-222222222222", ["fullName"] = "Nora Bouchard", ["email"] = "nora.bouchard@example.com", ["phoneNumber"] = "+1-403-555-0181", ["source"] = "Manual", ["status"] = "Qualified", ["preferredLanguage"] = "fr-CA", ["qualificationScore"] = 88, ["summary"] = "Lead qualifié pour visite et facture mensuelle en Alberta.", ["notesJson"] = "{\"source\":\"seed\",\"market\":\"CA-AB\",\"language\":\"fr-CA\",\"fields\":{\"targetAsset\":\"11111111-aaaa-bbbb-cccc-222222222222\",\"billingPeriod\":\"2026-07\"}}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("lead", "62626262-aaaa-bbbb-cccc-727272727272", albertaTenantId, new Dictionary<string, object?> { ["assetId"] = "11111111-aaaa-bbbb-cccc-222222222222", ["fullName"] = "Olivier Tremblay", ["email"] = "olivier.tremblay@example.com", ["phoneNumber"] = "+1-403-555-0182", ["source"] = "InboxImport", ["status"] = "New", ["preferredLanguage"] = "en-CA", ["qualificationScore"] = 71, ["summary"] = "Imported lead from classified inbox message.", ["notesJson"] = "{\"source\":\"seed\",\"market\":\"CA-AB\",\"language\":\"en-CA\",\"fields\":{\"targetAsset\":\"11111111-aaaa-bbbb-cccc-222222222222\",\"preferredMoveIn\":\"2026-08\"}}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("lead", "63636363-aaaa-bbbb-cccc-737373737373", ontarioTenantId, new Dictionary<string, object?> { ["assetId"] = "13131313-aaaa-bbbb-cccc-242424242424", ["fullName"] = "Sophie Nguyen", ["email"] = "sophie.nguyen@example.com", ["phoneNumber"] = "+1-416-555-0183", ["source"] = "Manual", ["status"] = "Qualified", ["preferredLanguage"] = "en-CA", ["qualificationScore"] = 93, ["summary"] = "Top Ontario prospect ready for lease conversion.", ["notesJson"] = "{\"source\":\"seed\",\"market\":\"CA-ON\",\"language\":\"en-CA\",\"fields\":{\"targetAsset\":\"13131313-aaaa-bbbb-cccc-242424242424\",\"parking\":\"1\"}}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("lead", "64646464-aaaa-bbbb-cccc-747474747474", texasTenantId, new Dictionary<string, object?> { ["assetId"] = "15151515-aaaa-bbbb-cccc-262626262626", ["fullName"] = "Carlos Herrera", ["email"] = "carlos.herrera@example.com", ["phoneNumber"] = "+1-214-555-0184", ["source"] = "AIImport", ["status"] = "Archived", ["preferredLanguage"] = "es-MX", ["qualificationScore"] = 64, ["summary"] = "Archived after duplicate check.", ["notesJson"] = "{\"source\":\"seed\",\"market\":\"US-TX\",\"language\":\"es-MX\",\"fields\":{\"targetAsset\":\"15151515-aaaa-bbbb-cccc-262626262626\",\"ownerName\":\"Runtira Demo Texas\"}}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null })
            };

            documents.AddRange(
            [
                CreateTenantDocument("inboxMessage", "62626262-aaaa-bbbb-cccc-848484848484", albertaTenantId, new Dictionary<string, object?> { ["externalMessageId"] = "mock-ab-001", ["provider"] = "MockMicrosoft365", ["fromEmail"] = "prospect.ab@example.com", ["subject"] = "Disponibilité pour août", ["previewText"] = "Bonjour, je cherche un 2 chambres disponible en août à Calgary.", ["receivedUtc"] = "2026-07-03T14:00:00Z", ["status"] = "Classified", ["category"] = "Lead", ["relatedEntityType"] = "Lead", ["relatedEntityId"] = "47474747-aaaa-bbbb-cccc-676767676767", ["hasAttachments"] = true, ["classificationJson"] = "{\"confidence\":0.92,\"suggestedAction\":\"CallBack\"}", ["createdUtc"] = "2026-07-03T14:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("inboxMessage", "64646464-aaaa-bbbb-cccc-868686868686", ontarioTenantId, new Dictionary<string, object?> { ["externalMessageId"] = "mock-on-001", ["provider"] = "MockMicrosoft365", ["fromEmail"] = "resident.on@example.com", ["subject"] = "Need invoice copy for July", ["previewText"] = "Can you resend the July invoice for unit 1204?", ["receivedUtc"] = "2026-07-03T15:00:00Z", ["status"] = "Classified", ["category"] = "Invoice", ["relatedEntityType"] = "Lease", ["relatedEntityId"] = "43434343-aaaa-bbbb-cccc-636363636363", ["hasAttachments"] = false, ["classificationJson"] = "{\"confidence\":0.88,\"suggestedAction\":\"SendInvoice\"}", ["createdUtc"] = "2026-07-03T15:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("inboxMessage", "66666666-aaaa-bbbb-cccc-888888888889", texasTenantId, new Dictionary<string, object?> { ["externalMessageId"] = "mock-tx-001", ["provider"] = "MockMicrosoft365", ["fromEmail"] = "owner.tx@example.com", ["subject"] = "Please archive this vendor quote", ["previewText"] = "Attaching a quote for the next lease renewal package.", ["receivedUtc"] = "2026-07-03T16:00:00Z", ["status"] = "Classified", ["category"] = "Document", ["relatedEntityType"] = "Asset", ["relatedEntityId"] = "15151515-aaaa-bbbb-cccc-262626262626", ["hasAttachments"] = true, ["classificationJson"] = "{\"confidence\":0.81,\"suggestedAction\":\"ArchiveDocument\"}", ["createdUtc"] = "2026-07-03T16:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("attachment", "68686868-aaaa-bbbb-cccc-909090909091", albertaTenantId, new Dictionary<string, object?> { ["inboxMessageId"] = "62626262-aaaa-bbbb-cccc-848484848484", ["fileName"] = "budget-range.txt", ["contentType"] = "text/plain", ["sizeBytes"] = 256, ["category"] = "LeadDocument", ["metadataJson"] = "{\"source\":\"mock-inbox\"}", ["createdUtc"] = "2026-07-03T14:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("attachment", "70707070-aaaa-bbbb-cccc-929292929293", texasTenantId, new Dictionary<string, object?> { ["inboxMessageId"] = "66666666-aaaa-bbbb-cccc-888888888889", ["fileName"] = "vendor-quote.pdf", ["contentType"] = "application/pdf", ["sizeBytes"] = 40960, ["category"] = "VendorQuote", ["metadataJson"] = "{\"source\":\"mock-inbox\"}", ["createdUtc"] = "2026-07-03T16:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("jurisdictionProfile", "12121212-aaaa-bbbb-cccc-343434343434", albertaTenantId, new Dictionary<string, object?> { ["countryCode"] = "CA", ["regionCode"] = "AB", ["supportedLanguagesJson"] = "[\"fr-CA\",\"en-CA\",\"es-MX\"]", ["requiredQuestionsJson"] = "[\"propertyAddress\",\"billingPeriod\",\"monthlyRent\"]", ["validationRulesJson"] = "{\"billingPeriod\":{\"required\":true}}", ["invoiceRulesJson"] = "{\"generatePdf\":true,\"addAutomaticSalesTax\":false}", ["assetRulesJson"] = "{\"supportsMultiUnit\":true}", ["maintenanceRulesJson"] = "{\"supportInboxClassification\":true}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("jurisdictionProfile", "14141414-aaaa-bbbb-cccc-363636363636", ontarioTenantId, new Dictionary<string, object?> { ["countryCode"] = "CA", ["regionCode"] = "ON", ["supportedLanguagesJson"] = "[\"en-CA\",\"fr-CA\"]", ["requiredQuestionsJson"] = "[\"propertyAddress\",\"billingPeriod\",\"tenantName\",\"monthlyRent\"]", ["validationRulesJson"] = "{\"billingPeriod\":{\"required\":true},\"tenantName\":{\"required\":true}}", ["invoiceRulesJson"] = "{\"generatePdf\":true,\"addAutomaticSalesTax\":false}", ["assetRulesJson"] = "{\"supportsMultiUnit\":true}", ["maintenanceRulesJson"] = "{\"supportInboxClassification\":true}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("jurisdictionProfile", "16161616-aaaa-bbbb-cccc-383838383838", texasTenantId, new Dictionary<string, object?> { ["countryCode"] = "US", ["regionCode"] = "TX", ["supportedLanguagesJson"] = "[\"en-US\",\"es-MX\"]", ["requiredQuestionsJson"] = "[\"propertyAddress\",\"billingPeriod\",\"ownerName\",\"monthlyRent\"]", ["validationRulesJson"] = "{\"billingPeriod\":{\"required\":true},\"ownerName\":{\"required\":true}}", ["invoiceRulesJson"] = "{\"generatePdf\":true,\"addAutomaticSalesTax\":false}", ["assetRulesJson"] = "{\"supportsMultiUnit\":true}", ["maintenanceRulesJson"] = "{\"supportInboxClassification\":true}", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("quotaPolicy", "56565656-aaaa-bbbb-cccc-787878787878", albertaTenantId, new Dictionary<string, object?> { ["maxAssets"] = 100, ["maxDocuments"] = 1000, ["maxMonthlyAiRequests"] = 5000, ["maxBlobStorageMb"] = 2048, ["maxActiveWorkflows"] = 50, ["enforceHardLimit"] = true, ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("quotaPolicy", "58585858-aaaa-bbbb-cccc-808080808080", ontarioTenantId, new Dictionary<string, object?> { ["maxAssets"] = 100, ["maxDocuments"] = 1000, ["maxMonthlyAiRequests"] = 5000, ["maxBlobStorageMb"] = 2048, ["maxActiveWorkflows"] = 50, ["enforceHardLimit"] = true, ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("quotaPolicy", "60606060-aaaa-bbbb-cccc-828282828282", texasTenantId, new Dictionary<string, object?> { ["maxAssets"] = 100, ["maxDocuments"] = 1000, ["maxMonthlyAiRequests"] = 5000, ["maxBlobStorageMb"] = 2048, ["maxActiveWorkflows"] = 50, ["enforceHardLimit"] = true, ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("blobArchive", "99999999-aaaa-bbbb-cccc-000000000000", albertaTenantId, new Dictionary<string, object?> { ["blobPath"] = "demo-alberta/invoices/2026/07/invoice-july.json", ["contentType"] = "application/json", ["category"] = "InvoiceDraft", ["metadataJson"] = "{\"period\":\"2026-07\"}", ["sizeBytes"] = 512, ["sourceSystem"] = "seed", ["hash"] = "seed-demo-alberta-invoice", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("blobArchive", "a1a1a1a1-aaaa-bbbb-cccc-020202020202", ontarioTenantId, new Dictionary<string, object?> { ["blobPath"] = "demo-ontario/invoices/2026/07/invoice-july.json", ["contentType"] = "application/json", ["category"] = "InvoiceDraft", ["metadataJson"] = "{\"period\":\"2026-07\"}", ["sizeBytes"] = 544, ["sourceSystem"] = "seed", ["hash"] = "seed-demo-ontario-invoice", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null }),
                CreateTenantDocument("blobArchive", "a3a3a3a3-aaaa-bbbb-cccc-040404040404", texasTenantId, new Dictionary<string, object?> { ["blobPath"] = "demo-texas/invoices/2026/07/invoice-july.json", ["contentType"] = "application/json", ["category"] = "InvoiceDraft", ["metadataJson"] = "{\"period\":\"2026-07\"}", ["sizeBytes"] = 536, ["sourceSystem"] = "seed", ["hash"] = "seed-demo-texas-invoice", ["createdUtc"] = "2026-07-03T00:00:00Z", ["modifiedUtc"] = null })
            ]);

            documents.AddRange(Enumerable.Range(1, 50).Select(index =>
                CreateTenantDocument("unit", $"10000000-0000-0000-0000-{index:D12}", albertaTenantId, new Dictionary<string, object?>
                {
                    ["assetId"] = "11111111-aaaa-bbbb-cccc-222222222222",
                    ["unitCode"] = $"A-{100 + index}",
                    ["unitType"] = index % 5 == 0 ? "Studio" : index % 2 == 0 ? "Apartment" : "Loft",
                    ["status"] = index == 1 ? "Occupied" : index % 11 == 0 ? "Maintenance" : index % 3 == 0 ? "Reserved" : "Available",
                    ["marketRent"] = 1750m + (index * 22m),
                    ["additionalDataJson"] = $"{{\"bedrooms\":{(index % 3) + 1},\"floor\":{((index - 1) / 10) + 1}}}",
                    ["createdUtc"] = "2026-07-03T00:00:00Z",
                    ["modifiedUtc"] = null
                })));

            return documents;
        }
    }

    internal sealed class CosmosLeadWorkspaceStore : IRuntiraLeadWorkspaceStore
    {
        private readonly CosmosClient _cosmosClient;
        private readonly CosmosOptions _options;

        public CosmosLeadWorkspaceStore(CosmosClient cosmosClient, CosmosOptions options)
        {
            _cosmosClient = cosmosClient;
            _options = options;
        }

        public async Task<IReadOnlyList<Runtira.Application.Features.RuntiraLeadSummaryDto>> GetLeadsAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var container = GetTenantCoreContainer();
            var leads = await QueryManyAsync(container, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'lead' ORDER BY c.data.qualificationScore DESC, c.data.fullName", cancellationToken);
            var assets = await QueryManyAsync(container, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'asset'", cancellationToken);
            var assetNames = assets.ToDictionary(x => x.id, x => GetString(x, "name"), StringComparer.OrdinalIgnoreCase);

            return leads.Select(lead => new Runtira.Application.Features.RuntiraLeadSummaryDto
            {
                Id = ParseGuid(lead.id),
                FullName = GetString(lead, "fullName"),
                Email = GetString(lead, "email"),
                Status = GetString(lead, "status"),
                Source = GetString(lead, "source"),
                PreferredLanguage = GetString(lead, "preferredLanguage"),
                AssetName = assetNames.TryGetValue(GetString(lead, "assetId"), out var assetName) ? assetName : string.Empty,
                QualificationScore = GetInt(lead, "qualificationScore"),
                Summary = GetString(lead, "summary"),
                ContextDataJson = GetString(lead, "notesJson", "{}"),
                ContextData = Runtira.Application.Common.RuntiraJson.Deserialize<Runtira.Application.Features.RuntiraLeadContextData>(GetString(lead, "notesJson", "{}"))
            }).ToList();
        }

        public async Task<IReadOnlyList<Runtira.Application.Features.RuntiraLeadConversionCandidateDto>> GetLeadConversionCandidatesAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var container = GetTenantCoreContainer();
            var leads = await QueryManyAsync(container, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'lead' ORDER BY c.data.qualificationScore DESC, c.data.fullName", cancellationToken);
            if (leads.Count == 0)
            {
                return Array.Empty<Runtira.Application.Features.RuntiraLeadConversionCandidateDto>();
            }

            var assets = await QueryManyAsync(container, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'asset'", cancellationToken);
            var units = await QueryManyAsync(container, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'unit'", cancellationToken);
            var residents = await QueryManyAsync(container, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'resident'", cancellationToken);

            var assetById = assets.ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);

            return leads.Take(5).Select(lead =>
            {
                var leadAssetId = GetString(lead, "assetId");
                var suggestedUnit = units
                    .Where(x => string.Equals(GetString(x, "assetId"), leadAssetId, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => string.Equals(GetString(x, "status"), "Available", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenBy(x => GetString(x, "unitCode"))
                    .FirstOrDefault();
                var matchedResident = residents.FirstOrDefault(x =>
                    string.Equals(GetString(x, "email"), GetString(lead, "email"), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(GetString(x, "fullName"), GetString(lead, "fullName"), StringComparison.OrdinalIgnoreCase));
                var nextAction = matchedResident is not null
                    ? "ExistingResidentReview"
                    : suggestedUnit is null
                        ? "AssignAsset"
                        : string.Equals(GetString(suggestedUnit, "status"), "Available", StringComparison.OrdinalIgnoreCase)
                            ? "CreateResidentLease"
                            : "PrepareWaitlist";

                return new Runtira.Application.Features.RuntiraLeadConversionCandidateDto
                {
                    LeadId = ParseGuid(lead.id),
                    LeadName = GetString(lead, "fullName"),
                    AssetName = assetById.TryGetValue(leadAssetId, out var asset) ? GetString(asset, "name") : "—",
                    PreferredLanguage = GetString(lead, "preferredLanguage"),
                    QualificationScore = GetInt(lead, "qualificationScore"),
                    SuggestedUnitCode = suggestedUnit is null ? "—" : GetString(suggestedUnit, "unitCode"),
                    SuggestedRent = suggestedUnit is null ? 0m : GetDecimal(suggestedUnit, "marketRent"),
                    ResidentName = matchedResident is null ? "New resident" : GetString(matchedResident, "fullName"),
                    NextAction = nextAction
                };
            }).ToList();
        }

        public async Task<Runtira.Application.Features.RuntiraLeadFormContextDto> GetLeadFormContextAsync(Guid tenantId, string preferredLanguage, string countryCode, string regionCode, Runtira.Application.Features.RuntiraLegislationProfileDto? profile, CancellationToken cancellationToken = default)
        {
            var container = GetTenantCoreContainer();
            var assets = await QueryManyAsync(container, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'asset' ORDER BY c.data.name", cancellationToken);
            var supportedLanguages = profile is null
                ? new List<string> { preferredLanguage }
                : ParseList(profile.SupportedLanguagesJson).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (supportedLanguages.Count == 0)
            {
                supportedLanguages.Add(preferredLanguage);
            }

            var fields = ParseContextFormDefinition(profile?.AssetRulesJson, "leadForm", ["fullName", "email", "phoneNumber", "preferredLanguage", "targetAsset", "summary"], ["fullName", "email"]);

            return new Runtira.Application.Features.RuntiraLeadFormContextDto
            {
                JurisdictionCode = profile?.JurisdictionCode ?? $"{countryCode}-{regionCode}",
                JurisdictionDisplayName = profile?.DisplayName ?? $"{countryCode}-{regionCode}",
                PreferredLanguage = preferredLanguage,
                SupportedLanguages = supportedLanguages,
                Fields = fields.VisibleFields.Select(x => new Runtira.Application.Features.RuntiraLeadFormFieldDto
                {
                    Key = x,
                    Required = fields.RequiredFields.Contains(x),
                    SuggestedValue = x.Equals("preferredLanguage", StringComparison.OrdinalIgnoreCase) ? preferredLanguage : string.Empty
                }).ToList(),
                Assets = assets.Select(x => new Runtira.Application.Features.RuntiraLeadAssetOptionDto
                {
                    Id = ParseGuid(x.id),
                    Name = GetString(x, "name")
                }).ToList(),
                AssetRulesJson = profile?.AssetRulesJson ?? "{}"
            };
        }

        public async Task<Runtira.Application.Features.RuntiraLeaseConversionFormContextDto?> GetLeaseConversionFormContextAsync(Guid tenantId, Guid leadId, string organizationName, string countryCode, string regionCode, Runtira.Application.Features.RuntiraLegislationProfileDto? profile, CancellationToken cancellationToken = default)
        {
            var container = GetTenantCoreContainer();
            var lead = await ReadItemAsync(container, leadId, tenantId, cancellationToken);
            if (lead is null)
            {
                return null;
            }

            var asset = await ReadItemAsync(container, GetGuid(lead, "assetId"), tenantId, cancellationToken);
            var units = await QueryManyAsync(container, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'unit'", cancellationToken);
            var suggestedUnit = units
                .Where(x => asset is null || string.Equals(GetString(x, "assetId"), asset.id, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => string.Equals(GetString(x, "status"), "Available", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(x => GetString(x, "unitCode"))
                .FirstOrDefault();

            var formDefinition = ParseContextFormDefinition(profile?.AssetRulesJson, "leaseConversionForm", ["residentName", "unitCode", "leaseStartDate", "monthlyRent", "billingPeriod"], ["residentName", "unitCode", "leaseStartDate", "monthlyRent"]);
            var suggestedValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["residentName"] = GetString(lead, "fullName"),
                ["preferredLanguage"] = GetString(lead, "preferredLanguage"),
                ["unitCode"] = suggestedUnit is null ? string.Empty : GetString(suggestedUnit, "unitCode"),
                ["leaseStartDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["monthlyRent"] = (suggestedUnit is null ? 0m : GetDecimal(suggestedUnit, "marketRent")).ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["billingPeriod"] = "Monthly",
                ["propertyAddress"] = asset is null ? string.Empty : GetString(asset, "addressLine1"),
                ["tenantName"] = GetString(lead, "fullName"),
                ["ownerName"] = organizationName
            };

            return new Runtira.Application.Features.RuntiraLeaseConversionFormContextDto
            {
                LeadId = leadId,
                LeadName = GetString(lead, "fullName"),
                JurisdictionDisplayName = profile?.DisplayName ?? $"{countryCode}-{regionCode}",
                Fields = formDefinition.VisibleFields.Select(x => new Runtira.Application.Features.RuntiraLeadFormFieldDto
                {
                    Key = x,
                    Required = formDefinition.RequiredFields.Contains(x),
                    SuggestedValue = suggestedValues.TryGetValue(x, out var value) ? value : string.Empty
                }).ToList()
            };
        }

        public async Task<Runtira.Application.Features.RuntiraCreateLeadResultDto> CreateLeadAsync(Guid tenantId, string organizationName, string preferredLanguage, IReadOnlyList<string> supportedLanguages, Runtira.Application.Features.RuntiraCreateLeadRequestDto request, Func<Runtira.Domain.Entities.RuntiraAsset?, Dictionary<string, string>?, Dictionary<string, string>?, Dictionary<string, string>?, string, Runtira.Application.Features.RuntiraFlexibleDataStrategyDto> flexibleDataBuilder, CancellationToken cancellationToken = default)
        {
            var container = GetTenantCoreContainer();
            var assetDocument = request.AssetId.HasValue ? await ReadItemAsync(container, request.AssetId.Value, tenantId, cancellationToken) : null;
            if (request.AssetId.HasValue && assetDocument is null)
            {
                return new Runtira.Application.Features.RuntiraCreateLeadResultDto { ResultCode = "InvalidAsset" };
            }

            var lead = new Runtira.Domain.Entities.RuntiraLead
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AssetId = request.AssetId,
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                PhoneNumber = request.PhoneNumber.Trim(),
                Source = "ManualContextForm",
                Status = "New",
                PreferredLanguage = supportedLanguages.Contains(request.PreferredLanguage, StringComparer.OrdinalIgnoreCase) ? request.PreferredLanguage : supportedLanguages.FirstOrDefault() ?? preferredLanguage,
                QualificationScore = 55,
                Summary = string.IsNullOrWhiteSpace(request.Summary) ? $"Manual lead created for {GetString(assetDocument ?? new CosmosDocument(), "name", organizationName)}." : request.Summary.Trim(),
                CreatedUtc = DateTime.UtcNow
            };

            var assetEntity = assetDocument is null ? null : new Runtira.Domain.Entities.RuntiraAsset
            {
                Id = ParseGuid(assetDocument.id),
                TenantId = tenantId,
                Name = GetString(assetDocument, "name"),
                AddressLine1 = GetString(assetDocument, "addressLine1")
            };
            var flexibleData = flexibleDataBuilder(assetEntity, request.DynamicFields, null, request.DynamicFields, "ManualContextForm");
            lead.NotesJson = flexibleData.LeadContextDataJson;

            await container.UpsertItemAsync(ToLeadDocument(lead), new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);
            return new Runtira.Application.Features.RuntiraCreateLeadResultDto { Success = true, ResultCode = "Created", LeadName = lead.FullName };
        }

        public async Task<Runtira.Application.Features.RuntiraLeadActionResultDto> ArchiveLeadAsync(Guid tenantId, Guid leadId, CancellationToken cancellationToken = default)
        {
            var container = GetTenantCoreContainer();
            var lead = await ReadItemAsync(container, leadId, tenantId, cancellationToken);
            if (lead is null)
            {
                return new Runtira.Application.Features.RuntiraLeadActionResultDto { ResultCode = "LeadNotFound" };
            }

            if (string.Equals(GetString(lead, "status"), "Archived", StringComparison.OrdinalIgnoreCase))
            {
                return new Runtira.Application.Features.RuntiraLeadActionResultDto { Success = true, ResultCode = "AlreadyArchived", LeadName = GetString(lead, "fullName") };
            }

            SetValue(lead, "status", "Archived");
            SetValue(lead, "modifiedUtc", DateTime.UtcNow);
            await container.UpsertItemAsync(lead, new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);
            return new Runtira.Application.Features.RuntiraLeadActionResultDto { Success = true, ResultCode = "Archived", LeadName = GetString(lead, "fullName") };
        }

        public async Task<Runtira.Application.Features.RuntiraLeadActionResultDto> DeleteLeadAsync(Guid tenantId, Guid leadId, CancellationToken cancellationToken = default)
        {
            var container = GetTenantCoreContainer();
            var lead = await ReadItemAsync(container, leadId, tenantId, cancellationToken);
            if (lead is null)
            {
                return new Runtira.Application.Features.RuntiraLeadActionResultDto { ResultCode = "LeadNotFound" };
            }

            var leadName = GetString(lead, "fullName");
            await container.DeleteItemAsync<CosmosDocument>(leadId.ToString(), new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);
            return new Runtira.Application.Features.RuntiraLeadActionResultDto { Success = true, ResultCode = "Deleted", LeadName = leadName };
        }

        public async Task<Runtira.Application.Features.RuntiraLeadConversionResultDto> ConvertLeadAsync(Guid tenantId, string organizationName, string preferredLanguage, Dictionary<string, string>? contextFields, Guid leadId, Func<Runtira.Domain.Entities.RuntiraAsset?, Dictionary<string, string>?, Dictionary<string, string>?, Dictionary<string, string>?, string, Runtira.Application.Features.RuntiraFlexibleDataStrategyDto> flexibleDataBuilder, CancellationToken cancellationToken = default)
        {
            var container = GetTenantCoreContainer();
            var lead = await ReadItemAsync(container, leadId, tenantId, cancellationToken);
            if (lead is null)
            {
                return new Runtira.Application.Features.RuntiraLeadConversionResultDto { ResultCode = "LeadNotFound" };
            }

            if (string.Equals(GetString(lead, "status"), "Converted", StringComparison.OrdinalIgnoreCase))
            {
                return new Runtira.Application.Features.RuntiraLeadConversionResultDto { ResultCode = "AlreadyConverted", LeadName = GetString(lead, "fullName") };
            }

            var normalizedContext = new Dictionary<string, string>(contextFields ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
            foreach (var field in new[] { "residentName", "unitCode", "leaseStartDate", "monthlyRent" })
            {
                if (!normalizedContext.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    return new Runtira.Application.Features.RuntiraLeadConversionResultDto { ResultCode = "MissingRequiredField", LeadName = GetString(lead, "fullName"), UnitCode = field };
                }
            }

            var assetId = GetString(lead, "assetId");
            var units = await QueryManyAsync(container, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'unit'", cancellationToken);
            var unit = units
                .Where(x => string.IsNullOrWhiteSpace(assetId) || string.Equals(GetString(x, "assetId"), assetId, StringComparison.OrdinalIgnoreCase))
                .Where(x => !normalizedContext.TryGetValue("unitCode", out var unitCode) || string.Equals(GetString(x, "unitCode"), unitCode, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => string.Equals(GetString(x, "status"), "Available", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(x => GetString(x, "unitCode"))
                .FirstOrDefault();

            if (unit is null)
            {
                return new Runtira.Application.Features.RuntiraLeadConversionResultDto { ResultCode = "UnitNotFound", LeadName = GetString(lead, "fullName") };
            }

            var residents = await QueryManyAsync(container, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'resident'", cancellationToken);
            var resident = residents.FirstOrDefault(x =>
                string.Equals(GetString(x, "email"), GetString(lead, "email"), StringComparison.OrdinalIgnoreCase)
                || string.Equals(GetString(x, "fullName"), GetString(lead, "fullName"), StringComparison.OrdinalIgnoreCase));

            if (resident is null)
            {
                var residentId = Guid.NewGuid();
                resident = CreateTenantDocument("resident", residentId.ToString(), tenantId.ToString(), new Dictionary<string, object?>
                {
                    ["fullName"] = normalizedContext.TryGetValue("residentName", out var residentName) && !string.IsNullOrWhiteSpace(residentName) ? residentName : GetString(lead, "fullName"),
                    ["email"] = GetString(lead, "email"),
                    ["phoneNumber"] = GetString(lead, "phoneNumber"),
                    ["preferredLanguage"] = normalizedContext.TryGetValue("preferredLanguage", out var contextPreferredLanguage) && !string.IsNullOrWhiteSpace(contextPreferredLanguage) ? contextPreferredLanguage : GetString(lead, "preferredLanguage", preferredLanguage),
                    ["status"] = "Active",
                    ["notesJson"] = JsonSerializer.Serialize(normalizedContext),
                    ["createdUtc"] = DateTime.UtcNow,
                    ["modifiedUtc"] = null
                });
                await container.UpsertItemAsync(resident, new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);
            }

            var monthlyRent = normalizedContext.TryGetValue("monthlyRent", out var monthlyRentValue) && decimal.TryParse(monthlyRentValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedMonthlyRent)
                ? parsedMonthlyRent
                : GetDecimal(unit, "marketRent");
            var leaseStartUtc = normalizedContext.TryGetValue("leaseStartDate", out var leaseStartValue) && DateTime.TryParse(leaseStartValue, out var parsedLeaseStart)
                ? parsedLeaseStart.Date
                : DateTime.UtcNow.Date;
            var billingPeriod = normalizedContext.TryGetValue("billingPeriod", out var billingPeriodValue) && !string.IsNullOrWhiteSpace(billingPeriodValue) ? billingPeriodValue : "Monthly";
            var asset = string.IsNullOrWhiteSpace(assetId) ? null : await ReadItemAsync(container, ParseGuid(assetId), tenantId, cancellationToken);
            var assetEntity = asset is null ? null : new Runtira.Domain.Entities.RuntiraAsset { Id = ParseGuid(asset.id), TenantId = tenantId, Name = GetString(asset, "name"), AddressLine1 = GetString(asset, "addressLine1") };
            var flexibleData = flexibleDataBuilder(assetEntity, null, normalizedContext, normalizedContext, "MockLeadConversion");

            var existingLeases = await QueryManyAsync(container, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'lease' AND c.data.unitId = @unitId AND c.data.residentId = @residentId", cancellationToken, ("@unitId", (object)unit.id), ("@residentId", (object)resident.id));
            var leaseStatus = string.Equals(GetString(unit, "status"), "Available", StringComparison.OrdinalIgnoreCase) ? "Active" : "Pending";
            var lease = existingLeases.FirstOrDefault();
            if (lease is null)
            {
                lease = CreateTenantDocument("lease", Guid.NewGuid().ToString(), tenantId.ToString(), new Dictionary<string, object?>
                {
                    ["assetId"] = GetString(unit, "assetId"),
                    ["unitId"] = unit.id,
                    ["residentId"] = resident.id,
                    ["leaseStartUtc"] = leaseStartUtc,
                    ["leaseEndUtc"] = leaseStartUtc.AddYears(1).AddDays(-1),
                    ["monthlyRent"] = monthlyRent,
                    ["billingPeriod"] = billingPeriod,
                    ["status"] = leaseStatus,
                    ["termsJson"] = flexibleData.LeaseComplianceDataJson,
                    ["createdUtc"] = DateTime.UtcNow,
                    ["modifiedUtc"] = null
                });
            }
            else
            {
                leaseStatus = GetString(lease, "status");
                SetValue(lease, "termsJson", flexibleData.LeaseComplianceDataJson);
                SetValue(lease, "modifiedUtc", DateTime.UtcNow);
            }

            await container.UpsertItemAsync(lease, new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);

            if (string.Equals(GetString(unit, "status"), "Available", StringComparison.OrdinalIgnoreCase))
            {
                SetValue(unit, "status", "Occupied");
                SetValue(unit, "modifiedUtc", DateTime.UtcNow);
                await container.UpsertItemAsync(unit, new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);
            }

            SetValue(lead, "status", "Converted");
            SetValue(lead, "summary", string.IsNullOrWhiteSpace(GetString(lead, "summary")) ? $"Converted to resident {GetString(resident, "fullName")} and unit {GetString(unit, "unitCode")}." : $"{GetString(lead, "summary")} Converted to resident {GetString(resident, "fullName")} and unit {GetString(unit, "unitCode")}." );
            SetValue(lead, "notesJson", flexibleData.LeadContextDataJson);
            SetValue(lead, "modifiedUtc", DateTime.UtcNow);
            await container.UpsertItemAsync(lead, new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);

            return new Runtira.Application.Features.RuntiraLeadConversionResultDto
            {
                Success = true,
                ResultCode = "Converted",
                LeadName = GetString(lead, "fullName"),
                ResidentName = GetString(resident, "fullName"),
                UnitCode = GetString(unit, "unitCode"),
                LeaseStatus = leaseStatus
            };
        }

        private Container GetTenantCoreContainer()
            => _cosmosClient.GetDatabase(_options.DatabaseName).GetContainer("TenantCore");

        private static CosmosDocument ToLeadDocument(Runtira.Domain.Entities.RuntiraLead lead)
            => CreateTenantDocument("lead", lead.Id.ToString(), lead.TenantId.ToString(), new Dictionary<string, object?>
            {
                ["assetId"] = lead.AssetId?.ToString(),
                ["fullName"] = lead.FullName,
                ["email"] = lead.Email,
                ["phoneNumber"] = lead.PhoneNumber,
                ["source"] = lead.Source,
                ["status"] = lead.Status,
                ["preferredLanguage"] = lead.PreferredLanguage,
                ["qualificationScore"] = lead.QualificationScore,
                ["summary"] = lead.Summary,
                ["notesJson"] = lead.NotesJson,
                ["createdUtc"] = lead.CreatedUtc,
                ["modifiedUtc"] = lead.ModifiedUtc
            });

    }

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
            var effectiveRegion = !string.IsNullOrWhiteSpace(regionClaim) ? regionClaim : GetString(organization, "regionCode", "AB");
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

    internal sealed class CosmosProvisioningService : IHostedService
    {
        private readonly CosmosClient? _cosmosClient;
        private readonly CosmosOptions _options;
        private readonly ILogger<CosmosProvisioningService> _logger;

        public CosmosProvisioningService(CosmosClient? cosmosClient, CosmosOptions options, ILogger<CosmosProvisioningService> logger)
        {
            _cosmosClient = cosmosClient;
            _options = options;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_cosmosClient is null || !_options.Enabled)
            {
                return;
            }

            var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(
                _options.DatabaseName,
                ThroughputProperties.CreateAutoscaleThroughput(_options.SharedAutoscaleMaxThroughput),
                cancellationToken: cancellationToken);

            var database = databaseResponse.Database;

            foreach (var containerDefinition in CosmosSchemaDefinition.Build(_options))
            {
                await database.CreateContainerIfNotExistsAsync(
                    new ContainerProperties(containerDefinition.Name, containerDefinition.PartitionKeyPath),
                    cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Cosmos DB database {DatabaseName} and compact Runtira containers are ready.", _options.DatabaseName);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}

namespace Runtira.Infrastructure.Services
{
    using System.Globalization;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Text.Json.Serialization;
    using Runtira.Application.Common;
    using Runtira.Application.Features;
    using Runtira.Infrastructure.Data;
    using Runtira.Infrastructure.Options;

    internal sealed class ResendEmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly ResendOptions _options;
        private readonly ILogger<ResendEmailService> _logger;

        public ResendEmailService(HttpClient httpClient, Microsoft.Extensions.Options.IOptions<ResendOptions> options, ILogger<ResendEmailService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string html, string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogWarning("Resend API key is not configured. Email to {Recipient} was skipped.", to);
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
            {
                Content = JsonContent.Create(new ResendEmailRequest
                {
                    From = string.IsNullOrWhiteSpace(_options.FromName) ? _options.FromEmail : $"{_options.FromName} <{_options.FromEmail}>",
                    To = new[] { to },
                    Subject = subject,
                    Html = html,
                    Text = text
                })
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Resend email failed with status {StatusCode}: {Body}", response.StatusCode, body);
            }
        }


        private sealed class ResendEmailRequest
        {
            [JsonPropertyName("from")]
            public string From { get; set; } = string.Empty;

            [JsonPropertyName("to")]
            public string[] To { get; set; } = Array.Empty<string>();

            [JsonPropertyName("subject")]
            public string Subject { get; set; } = string.Empty;

            [JsonPropertyName("html")]
            public string Html { get; set; } = string.Empty;

            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;
        }
    }

    public sealed class StripePlanPriceDto
    {
        public string Plan { get; set; } = string.Empty;
        public string PriceId { get; set; } = string.Empty;
        public string Currency { get; set; } = "usd";
        public decimal UnitAmount { get; set; }
        public string Interval { get; set; } = "month";
        public string DisplayPrice { get; set; } = string.Empty;
    }

    public sealed class RuntiraBillingPlanDefinition
    {
        public string Plan { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public long UnitAmount { get; set; }
        public string Currency { get; set; } = "usd";
        public string Interval { get; set; } = "month";
    }

    public sealed class StripeBillingService
    {
        private readonly StripeOptions _options;
        private readonly HttpClient _httpClient;
        private readonly CosmosClient? _cosmosClient;
        private readonly CosmosOptions _cosmosOptions;
        private readonly ILogger<StripeBillingService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private static readonly IReadOnlyList<RuntiraBillingPlanDefinition> Plans = new[]
        {
            new RuntiraBillingPlanDefinition { Plan = "Starter", ProductName = "Runtira Starter", ProductDescription = "Solo AI-first workspace", UnitAmount = 4900, Currency = "usd", Interval = "month" },
            new RuntiraBillingPlanDefinition { Plan = "Growth", ProductName = "Runtira Growth", ProductDescription = "Growing multi-tenant workspace", UnitAmount = 12900, Currency = "usd", Interval = "month" },
            new RuntiraBillingPlanDefinition { Plan = "Pro", ProductName = "Runtira Pro", ProductDescription = "Advanced operations and admin coverage", UnitAmount = 24900, Currency = "usd", Interval = "month" }
        };

        public StripeBillingService(HttpClient httpClient, StripeOptions options, CosmosClient? cosmosClient, CosmosOptions cosmosOptions, ILogger<StripeBillingService> logger)
        {
            _httpClient = httpClient;
            _options = options;
            _cosmosClient = cosmosClient;
            _cosmosOptions = cosmosOptions;
            _logger = logger;
        }

        public async Task<List<StripePlanPriceDto>> GetPlanPricesAsync()
        {
            var results = new List<StripePlanPriceDto>();
            foreach (var plan in Plans)
            {
                var priceId = await EnsurePriceAsync(plan);
                results.Add(new StripePlanPriceDto
                {
                    Plan = plan.Plan,
                    PriceId = priceId,
                    Currency = plan.Currency,
                    UnitAmount = plan.UnitAmount / 100m,
                    Interval = plan.Interval,
                    DisplayPrice = FormatDisplayPrice(plan.UnitAmount, plan.Currency, plan.Interval)
                });
            }

            return results;
        }

        public async Task<string> CreateCheckoutSessionAsync(Guid organizationId, string ownerEmail, string plan, string successUrl, string cancelUrl)
        {
            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                return cancelUrl;
            }

            var planDefinition = Plans.FirstOrDefault(x => string.Equals(x.Plan, plan, StringComparison.OrdinalIgnoreCase)) ?? Plans[1];
            var priceId = await EnsurePriceAsync(planDefinition);
            if (string.IsNullOrWhiteSpace(priceId))
            {
                return cancelUrl;
            }

            using var request = CreateRequest(HttpMethod.Post, "checkout/sessions");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["mode"] = "subscription",
                ["success_url"] = successUrl,
                ["cancel_url"] = cancelUrl,
                ["client_reference_id"] = organizationId.ToString(),
                ["customer_email"] = ownerEmail,
                ["metadata[tenantId]"] = organizationId.ToString(),
                ["metadata[plan]"] = planDefinition.Plan,
                ["line_items[0][price]"] = priceId,
                ["line_items[0][quantity]"] = "1"
            });

            using var response = await _httpClient.SendAsync(request);
            var session = await ReadAsync<StripeCheckoutSessionResponse>(response);
            return string.IsNullOrWhiteSpace(session.Url) ? cancelUrl : session.Url;
        }

        public async Task<string> CreateBillingPortalAsync(string customerId, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(_options.SecretKey) || string.IsNullOrWhiteSpace(customerId))
            {
                return returnUrl;
            }

            using var request = CreateRequest(HttpMethod.Post, "billing_portal/sessions");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["customer"] = customerId,
                ["return_url"] = returnUrl
            });

            using var response = await _httpClient.SendAsync(request);
            var session = await ReadAsync<StripePortalSessionResponse>(response);
            return string.IsNullOrWhiteSpace(session.Url) ? returnUrl : session.Url;
        }

        public async Task HandleWebhookAsync(string json, string? stripeSignature)
        {
            _logger.LogInformation("Received Runtira Stripe webhook. Signature present: {HasSignature}", !string.IsNullOrWhiteSpace(stripeSignature));
            var stripeEvent = JsonSerializer.Deserialize<StripeWebhookEvent>(json, JsonOptions);
            if (stripeEvent is null)
            {
                return;
            }

            var organization = await ResolveOrganizationForEventAsync(stripeEvent);
            if (organization is null)
            {
                return;
            }

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    if (stripeEvent.Data?.Object?.Deserialize<StripeCheckoutSessionResponse>() is { } checkoutSession)
                    {
                        organization.StripeCustomerId = checkoutSession.Customer ?? organization.StripeCustomerId;
                        organization.StripeSubscriptionId = checkoutSession.Subscription ?? organization.StripeSubscriptionId;
                        organization.BillingPlan = checkoutSession.Metadata?.TryGetValue("plan", out var checkoutPlan) == true ? checkoutPlan : organization.BillingPlan;
                    }
                    break;

                case "customer.subscription.created":
                case "customer.subscription.updated":
                    if (stripeEvent.Data?.Object?.Deserialize<StripeSubscriptionResponse>() is { } subscription)
                    {
                        organization.StripeCustomerId = subscription.Customer ?? organization.StripeCustomerId;
                        organization.StripeSubscriptionId = subscription.Id ?? organization.StripeSubscriptionId;
                        organization.BillingPlan = ResolvePlanFromPriceId(subscription.Items?.Data?.FirstOrDefault()?.Price?.Id);
                        organization.IsActive = subscription.Status is "active" or "trialing" or "past_due";
                    }
                    break;

                case "customer.subscription.deleted":
                    organization.StripeSubscriptionId = string.Empty;
                    organization.BillingPlan = "Trial";
                    break;
            }

            await SaveOrganizationAsync(organization);
        }

        private async Task<string> EnsurePriceAsync(RuntiraBillingPlanDefinition plan)
        {
            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                return string.Empty;
            }

            var configuredPriceId = GetConfiguredPriceId(plan.Plan);
            if (!string.IsNullOrWhiteSpace(configuredPriceId))
            {
                return configuredPriceId;
            }

            var productId = await EnsureProductAsync(plan);
            if (string.IsNullOrWhiteSpace(productId))
            {
                return string.Empty;
            }

            using var request = CreateRequest(HttpMethod.Post, "prices");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["currency"] = plan.Currency,
                ["unit_amount"] = plan.UnitAmount.ToString(CultureInfo.InvariantCulture),
                ["product"] = productId,
                ["recurring[interval]"] = plan.Interval,
                ["metadata[plan]"] = plan.Plan,
                ["nickname"] = $"{plan.Plan} monthly"
            });

            using var response = await _httpClient.SendAsync(request);
            var price = await ReadAsync<StripePriceResponse>(response);
            return price.Id ?? string.Empty;
        }

        private async Task<string> EnsureProductAsync(RuntiraBillingPlanDefinition plan)
        {
            using var request = CreateRequest(HttpMethod.Post, "products");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["name"] = plan.ProductName,
                ["description"] = plan.ProductDescription,
                ["metadata[plan]"] = plan.Plan
            });

            using var response = await _httpClient.SendAsync(request);
            var product = await ReadAsync<StripeProductResponse>(response);
            return product.Id ?? string.Empty;
        }

        private string GetConfiguredPriceId(string plan)
            => plan.ToLowerInvariant() switch
            {
                "starter" => _options.StarterPriceId,
                "growth" => _options.GrowthPriceId,
                "pro" => _options.ProPriceId,
                _ => string.Empty
            };

        private async Task<Runtira.Domain.Entities.RuntiraOrganization?> ResolveOrganizationForEventAsync(StripeWebhookEvent stripeEvent)
        {
            return stripeEvent.Type switch
            {
                "checkout.session.completed" when stripeEvent.Data?.Object?.Deserialize<StripeCheckoutSessionResponse>() is { } checkoutSession
                    => await ResolveOrganizationAsync(checkoutSession.Customer, checkoutSession.Subscription, checkoutSession.ClientReferenceId),
                "customer.subscription.created" or "customer.subscription.updated" or "customer.subscription.deleted" when stripeEvent.Data?.Object?.Deserialize<StripeSubscriptionResponse>() is { } subscription
                    => await ResolveOrganizationAsync(subscription.Customer, subscription.Id, null),
                _ => null
            };
        }

        private async Task<Runtira.Domain.Entities.RuntiraOrganization?> ResolveOrganizationAsync(string? customerId, string? subscriptionId, string? tenantReference)
        {
            if (_cosmosClient is null || !_cosmosOptions.Enabled)
            {
                return null;
            }

            var organizations = await QueryGlobalOrganizationsAsync();
            if (Guid.TryParse(tenantReference, out var tenantId))
            {
                var byTenant = organizations.FirstOrDefault(x => x.Id == tenantId);
                if (byTenant is not null)
                {
                    return byTenant;
                }
            }

            if (!string.IsNullOrWhiteSpace(subscriptionId))
            {
                var bySubscription = organizations.FirstOrDefault(x => x.StripeSubscriptionId == subscriptionId);
                if (bySubscription is not null)
                {
                    return bySubscription;
                }
            }

            return string.IsNullOrWhiteSpace(customerId)
                ? null
                : organizations.FirstOrDefault(x => x.StripeCustomerId == customerId);
        }

        private async Task<List<Runtira.Domain.Entities.RuntiraOrganization>> QueryGlobalOrganizationsAsync()
        {
            if (_cosmosClient is null)
            {
                return new List<Runtira.Domain.Entities.RuntiraOrganization>();
            }

            var container = _cosmosClient.GetDatabase(_cosmosOptions.DatabaseName).GetContainer("Organizations");
            var iterator = container.GetItemQueryIterator<Runtira.Infrastructure.Data.CosmosDocument>(new QueryDefinition("SELECT * FROM c"));
            var results = new List<Runtira.Domain.Entities.RuntiraOrganization>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Select(x => new Runtira.Domain.Entities.RuntiraOrganization
                {
                    Id = Guid.Parse(x.id),
                    Name = x.data.TryGetValue("name", out var name) ? name?.ToString() ?? string.Empty : string.Empty,
                    Slug = x.data.TryGetValue("slug", out var slug) ? slug?.ToString() ?? string.Empty : string.Empty,
                    OwnerEmail = x.data.TryGetValue("ownerEmail", out var ownerEmail) ? ownerEmail?.ToString() ?? string.Empty : string.Empty,
                    DefaultLocale = x.data.TryGetValue("defaultLocale", out var defaultLocale) ? defaultLocale?.ToString() ?? string.Empty : string.Empty,
                    CountryCode = x.data.TryGetValue("countryCode", out var countryCode) ? countryCode?.ToString() ?? string.Empty : string.Empty,
                    RegionCode = x.data.TryGetValue("regionCode", out var regionCode) ? regionCode?.ToString() ?? string.Empty : string.Empty,
                    StripeCustomerId = x.data.TryGetValue("stripeCustomerId", out var stripeCustomerId) ? stripeCustomerId?.ToString() ?? string.Empty : string.Empty,
                    StripeSubscriptionId = x.data.TryGetValue("stripeSubscriptionId", out var stripeSubscriptionId) ? stripeSubscriptionId?.ToString() ?? string.Empty : string.Empty,
                    BillingPlan = x.data.TryGetValue("billingPlan", out var billingPlan) ? billingPlan?.ToString() ?? string.Empty : string.Empty,
                    IsActive = !x.data.TryGetValue("isActive", out var isActive) || bool.TryParse(isActive?.ToString(), out var parsedIsActive) && parsedIsActive
                }));
            }

            return results;
        }

        private async Task SaveOrganizationAsync(Runtira.Domain.Entities.RuntiraOrganization organization)
        {
            if (_cosmosClient is null)
            {
                return;
            }

            var container = _cosmosClient.GetDatabase(_cosmosOptions.DatabaseName).GetContainer("Organizations");
            var document = new Runtira.Infrastructure.Data.CosmosDocument
            {
                id = organization.Id.ToString(),
                type = "organization",
                data = new Dictionary<string, object?>
                {
                    ["name"] = organization.Name,
                    ["slug"] = organization.Slug,
                    ["ownerEmail"] = organization.OwnerEmail,
                    ["defaultLocale"] = organization.DefaultLocale,
                    ["countryCode"] = organization.CountryCode,
                    ["regionCode"] = organization.RegionCode,
                    ["timeZone"] = organization.TimeZone,
                    ["legalProfileJson"] = organization.LegalProfileJson,
                    ["additionalSettingsJson"] = organization.AdditionalSettingsJson,
                    ["stripeCustomerId"] = organization.StripeCustomerId,
                    ["stripeSubscriptionId"] = organization.StripeSubscriptionId,
                    ["billingPlan"] = organization.BillingPlan,
                    ["isActive"] = organization.IsActive,
                    ["createdUtc"] = organization.CreatedUtc,
                    ["modifiedUtc"] = DateTime.UtcNow
                }
            };

            await container.UpsertItemAsync(document, new PartitionKey(document.id));
        }

        private static string ResolvePlanFromPriceId(string? priceId)
        {
            if (string.IsNullOrWhiteSpace(priceId))
            {
                return "Trial";
            }

            return Plans.FirstOrDefault(x => string.Equals(x.Plan, priceId, StringComparison.OrdinalIgnoreCase))?.Plan ?? "Paid";
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string path)
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
            return request;
        }

        private static async Task<T> ReadAsync<T>(HttpResponseMessage response) where T : class, new()
        {
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, JsonOptions) ?? new T();
        }

        private static string FormatDisplayPrice(long unitAmount, string currency, string interval)
            => $"{(unitAmount / 100m).ToString("C", CultureInfo.GetCultureInfo("en-US"))}/{interval}";

        private sealed class StripeCheckoutSessionResponse
        {
            public string? Url { get; set; }
            public string? Customer { get; set; }
            public string? Subscription { get; set; }
            public string? ClientReferenceId { get; set; }
            public Dictionary<string, string>? Metadata { get; set; }
        }

        private sealed class StripePortalSessionResponse
        {
            public string? Url { get; set; }
        }

        private sealed class StripePriceResponse
        {
            public string? Id { get; set; }
        }

        private sealed class StripeProductResponse
        {
            public string? Id { get; set; }
        }

        private sealed class StripeWebhookEvent
        {
            public string? Type { get; set; }
            public StripeEventData? Data { get; set; }
        }

        private sealed class StripeEventData
        {
            public JsonElement? Object { get; set; }
        }

        private sealed class StripeSubscriptionResponse
        {
            public string? Id { get; set; }
            public string? Customer { get; set; }
            public string? Status { get; set; }
            public StripeSubscriptionItems? Items { get; set; }
        }

        private sealed class StripeSubscriptionItems
        {
            public List<StripeSubscriptionItem> Data { get; set; } = new();
        }

        private sealed class StripeSubscriptionItem
        {
            public StripeSubscriptionPrice? Price { get; set; }
        }

        private sealed class StripeSubscriptionPrice
        {
            public string? Id { get; set; }
        }
    }

    public sealed class JsonLegislationCatalog : ILegislationCatalog
    {
        private readonly IReadOnlyDictionary<string, Runtira.Application.Features.RuntiraLegislationProfileDto> _profiles;

        public JsonLegislationCatalog(IConfiguration configuration)
        {
            var rootPath = configuration["Legislation:RootPath"];
            var basePath = AppContext.BaseDirectory;
            var resolvedRoot = string.IsNullOrWhiteSpace(rootPath)
                ? Path.Combine(basePath, "Legislation")
                : Path.IsPathRooted(rootPath) ? rootPath : Path.GetFullPath(Path.Combine(basePath, rootPath));

            var profiles = new Dictionary<string, Runtira.Application.Features.RuntiraLegislationProfileDto>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(resolvedRoot))
            {
                foreach (var file in Directory.EnumerateFiles(resolvedRoot, "*.json", SearchOption.AllDirectories))
                {
                    var content = File.ReadAllText(file);
                    var profile = JsonSerializer.Deserialize<Runtira.Application.Features.RuntiraLegislationProfileDto>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (profile is null || string.IsNullOrWhiteSpace(profile.CountryCode) || string.IsNullOrWhiteSpace(profile.RegionCode))
                    {
                        continue;
                    }

                    profiles[$"{profile.CountryCode}:{profile.RegionCode}"] = profile;
                }
            }

            _profiles = profiles;
        }

        public Runtira.Application.Features.RuntiraLegislationProfileDto? GetProfile(string countryCode, string regionCode)
            => _profiles.TryGetValue($"{countryCode}:{regionCode}", out var profile) ? profile : null;
    }

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRuntiraInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var clerkOptions = new ClerkOptions();
            configuration.GetSection("Clerk").Bind(clerkOptions);
            services.AddSingleton(clerkOptions);

            var stripeOptions = new StripeOptions();
            configuration.GetSection("Stripe").Bind(stripeOptions);
            services.AddSingleton(stripeOptions);

            var blobOptions = new AzureBlobOptions();
            configuration.GetSection("AzureBlob").Bind(blobOptions);
            services.AddSingleton(blobOptions);

            var microsoft365Options = new Microsoft365Options();
            configuration.GetSection("Microsoft365").Bind(microsoft365Options);
            services.AddSingleton(microsoft365Options);

            var aiOptions = new AiOptions();
            configuration.GetSection("AI").Bind(aiOptions);
            services.AddSingleton(aiOptions);

            var cosmosOptions = new CosmosOptions();
            configuration.GetSection("Cosmos").Bind(cosmosOptions);
            services.AddSingleton(cosmosOptions);

            if (cosmosOptions.Enabled
                && !string.IsNullOrWhiteSpace(cosmosOptions.Endpoint)
                && !string.IsNullOrWhiteSpace(cosmosOptions.Key))
            {
                services.AddSingleton(_ => new CosmosClient(cosmosOptions.Endpoint, cosmosOptions.Key, new CosmosClientOptions
                {
                    ApplicationName = "Runtira"
                }));
                services.AddHostedService<CosmosProvisioningService>();
                services.AddHostedService<CosmosSeedService>();
                services.AddSingleton<IRuntiraAssetWorkspaceStore, CosmosAssetWorkspaceStore>();
                services.AddSingleton<IRuntiraLeadWorkspaceStore, CosmosLeadWorkspaceStore>();
                services.AddSingleton<IRuntiraReadModelStore, CosmosReadModelStore>();
            }

            services.Configure<ResendOptions>(configuration.GetSection("Resend"));
            services.AddHttpClient<IEmailService, ResendEmailService>(client => client.BaseAddress = new Uri("https://api.resend.com/"));

            services.AddSingleton<ILegislationCatalog, JsonLegislationCatalog>();
            services.AddHttpClient<StripeBillingService>(client => client.BaseAddress = new Uri("https://api.stripe.com/v1/"));

            return services;
        }
    }
}
