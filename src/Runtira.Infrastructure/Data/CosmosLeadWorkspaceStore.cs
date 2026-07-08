using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Runtira.Application.Abstractions;
using Runtira.Infrastructure.Options;
using static Runtira.Infrastructure.Data.CosmosDocumentHelpers;

namespace Runtira.Infrastructure.Data
{
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
}
