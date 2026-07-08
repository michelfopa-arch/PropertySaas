using Microsoft.Azure.Cosmos;
using Runtira.Application.Abstractions;
using Runtira.Infrastructure.Options;
using static Runtira.Infrastructure.Data.CosmosDocumentHelpers;

namespace Runtira.Infrastructure.Data
{
    internal sealed class CosmosAssetWorkspaceStore : IRuntiraAssetWorkspaceStore
    {
        private readonly CosmosClient _cosmosClient;
        private readonly CosmosOptions _options;

        public CosmosAssetWorkspaceStore(CosmosClient cosmosClient, CosmosOptions options)
        {
            _cosmosClient = cosmosClient;
            _options = options;
        }

        public async Task<Runtira.Application.Features.RuntiraAssetWorkspaceDto?> GetAssetWorkspaceAsync(Guid tenantId, string? propertySlug = null, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var coreContainer = database.GetContainer("TenantCore");

            var assets = await QueryManyAsync(coreContainer, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'asset' ORDER BY c.data.name", cancellationToken);
            if (assets.Count == 0)
            {
                return null;
            }

            var assetDocument = string.IsNullOrWhiteSpace(propertySlug)
                ? assets[0]
                : assets.FirstOrDefault(x => string.Equals(Runtira.Application.Common.RuntiraSlug.Slugify(GetString(x, "name")), propertySlug, StringComparison.OrdinalIgnoreCase))
                    ?? assets[0];

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
                        LeaseStartUtc = GetDateTime(lease, "leaseStartUtc"),
                        LeaseEndUtc = GetDateTimeOrNull(lease, "leaseEndUtc"),
                        ComplianceDataJson = complianceDataJson,
                        ComplianceData = Runtira.Application.Common.RuntiraJson.Deserialize<Runtira.Application.Features.RuntiraLeaseComplianceData>(complianceDataJson)
                    };
                }).ToList()
            };
        }

        public async Task<IReadOnlyList<Runtira.Application.Features.RuntiraAssetSummaryDto>> GetAssetsAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var coreContainer = database.GetContainer("TenantCore");

            var assets = await QueryManyAsync(coreContainer, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'asset' ORDER BY c.data.name", cancellationToken);
            if (assets.Count == 0)
            {
                return Array.Empty<Runtira.Application.Features.RuntiraAssetSummaryDto>();
            }

            var units = await QueryManyAsync(coreContainer, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'unit'", cancellationToken);
            var leases = await QueryManyAsync(coreContainer, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'lease'", cancellationToken);
            var residents = await QueryManyAsync(coreContainer, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'resident'", cancellationToken);
            var activeResidentIds = residents
                .Where(x => string.Equals(GetString(x, "status"), "Active", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return assets.Select(asset =>
            {
                var assetIdText = asset.id;
                var assetUnits = units.Where(x => string.Equals(GetString(x, "assetId"), assetIdText, StringComparison.OrdinalIgnoreCase)).ToList();
                var assetLeases = leases.Where(x => string.Equals(GetString(x, "assetId"), assetIdText, StringComparison.OrdinalIgnoreCase)).ToList();
                var activeLeases = assetLeases.Where(x => string.Equals(GetString(x, "status"), "Active", StringComparison.OrdinalIgnoreCase)).ToList();
                var activeResidentCount = activeLeases
                    .Select(x => GetString(x, "residentId"))
                    .Where(x => activeResidentIds.Contains(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                return new Runtira.Application.Features.RuntiraAssetSummaryDto
                {
                    AssetId = ParseGuid(asset.id),
                    AssetName = GetString(asset, "name"),
                    AssetAddress = GetString(asset, "addressLine1"),
                    AssetType = GetString(asset, "assetType"),
                    PropertySlug = Runtira.Application.Common.RuntiraSlug.Slugify(GetString(asset, "name")),
                    UnitCount = assetUnits.Count > 0 ? assetUnits.Count : GetInt(asset, "unitCount"),
                    OccupiedUnitCount = assetUnits.Count(x => string.Equals(GetString(x, "status"), "Occupied", StringComparison.OrdinalIgnoreCase)),
                    ActiveLeaseCount = activeLeases.Count,
                    ActiveResidentCount = activeResidentCount,
                    MonthlyRevenue = activeLeases.Sum(x => GetDecimal(x, "monthlyRent"))
                };
            }).ToList();
        }

        public async Task<IReadOnlyList<Runtira.Application.Features.RuntiraLeaseInvoiceEntryDto>> GetLeaseInvoiceEntriesAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var coreContainer = database.GetContainer("TenantCore");

            var assets = await QueryManyAsync(coreContainer, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'asset'", cancellationToken);
            if (assets.Count == 0)
            {
                return Array.Empty<Runtira.Application.Features.RuntiraLeaseInvoiceEntryDto>();
            }

            var units = await QueryManyAsync(coreContainer, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'unit'", cancellationToken);
            var leases = await QueryManyAsync(coreContainer, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'lease' ORDER BY c.data.leaseStartUtc DESC", cancellationToken);
            var residents = await QueryManyAsync(coreContainer, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'resident'", cancellationToken);

            var assetById = assets.ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            var unitById = units.ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);
            var residentById = residents.ToDictionary(x => x.id, StringComparer.OrdinalIgnoreCase);

            return leases.Select(lease =>
            {
                var assetIdText = GetString(lease, "assetId");
                assetById.TryGetValue(assetIdText, out var assetDocument);
                var unitIdText = GetString(lease, "unitId");
                unitById.TryGetValue(unitIdText, out var unitDocument);
                var residentIdText = GetString(lease, "residentId");
                residentById.TryGetValue(residentIdText, out var residentDocument);

                return new Runtira.Application.Features.RuntiraLeaseInvoiceEntryDto
                {
                    LeaseId = ParseGuid(lease.id),
                    PropertyName = assetDocument is null ? string.Empty : GetString(assetDocument, "name"),
                    PropertyAddress = assetDocument is null ? string.Empty : GetString(assetDocument, "addressLine1"),
                    PropertySlug = assetDocument is null ? string.Empty : Runtira.Application.Common.RuntiraSlug.Slugify(GetString(assetDocument, "name")),
                    UnitCode = unitDocument is null ? string.Empty : GetString(unitDocument, "unitCode"),
                    ResidentName = residentDocument is null ? string.Empty : GetString(residentDocument, "fullName"),
                    MonthlyRent = GetDecimal(lease, "monthlyRent"),
                    BillingPeriod = GetString(lease, "billingPeriod"),
                    LeaseStatus = GetString(lease, "status"),
                    LeaseStartUtc = GetDateTime(lease, "leaseStartUtc"),
                    LeaseEndUtc = GetDateTimeOrNull(lease, "leaseEndUtc")
                };
            }).ToList();
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

        public async Task<Runtira.Application.Features.RuntiraRentLedgerDto?> GetRentLedgerAsync(Guid tenantId, Guid leaseId, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var coreContainer = database.GetContainer("TenantCore");
            var leaseDocument = await ReadItemAsync(coreContainer, leaseId, tenantId, cancellationToken);
            if (leaseDocument is null)
            {
                return null;
            }

            var monthlyRent = GetDecimal(leaseDocument, "monthlyRent");
            var leaseStartUtc = GetDateTime(leaseDocument, "leaseStartUtc");
            var payments = await QueryManyAsync(coreContainer, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'payment' AND c.data.leaseId = @leaseId", cancellationToken, ("@leaseId", (object)leaseId.ToString()));
            var paymentByMonth = payments.ToDictionary(x => new DateTime(GetDateTime(x, "dueUtc").Year, GetDateTime(x, "dueUtc").Month, 1, 0, 0, 0, DateTimeKind.Utc));

            var today = DateTime.UtcNow;
            var currentMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var firstMonth = new DateTime(leaseStartUtc.Year, leaseStartUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var entries = new List<Runtira.Application.Features.RuntiraRentLedgerEntryDto>();
            for (var month = firstMonth; month <= currentMonth; month = month.AddMonths(1))
            {
                var hasPayment = paymentByMonth.TryGetValue(month, out var paymentDocument);
                var status = hasPayment
                    ? GetString(paymentDocument!, "status", "Paid")
                    : month < currentMonth ? "Late" : "Pending";

                entries.Add(new Runtira.Application.Features.RuntiraRentLedgerEntryDto
                {
                    PeriodMonthUtc = month,
                    AmountDue = monthlyRent,
                    Status = status,
                    PaidUtc = hasPayment ? GetDateTimeOrNull(paymentDocument!, "paidUtc") : null
                });
            }

            entries.Reverse();

            return new Runtira.Application.Features.RuntiraRentLedgerDto
            {
                LeaseId = leaseId,
                MonthlyRent = monthlyRent,
                PaidCount = entries.Count(x => string.Equals(x.Status, "Paid", StringComparison.OrdinalIgnoreCase)),
                LateCount = entries.Count(x => string.Equals(x.Status, "Late", StringComparison.OrdinalIgnoreCase)),
                PendingCount = entries.Count(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
                OutstandingBalance = entries.Where(x => !string.Equals(x.Status, "Paid", StringComparison.OrdinalIgnoreCase)).Sum(x => x.AmountDue),
                Entries = entries
            };
        }

        public async Task<Runtira.Application.Features.RuntiraRentLedgerActionResultDto> MarkRentPaymentAsync(Guid tenantId, Guid leaseId, DateTime periodMonthUtc, string action, CancellationToken cancellationToken = default)
        {
            var database = _cosmosClient.GetDatabase(_options.DatabaseName);
            var coreContainer = database.GetContainer("TenantCore");
            var leaseDocument = await ReadItemAsync(coreContainer, leaseId, tenantId, cancellationToken);
            if (leaseDocument is null)
            {
                return new Runtira.Application.Features.RuntiraRentLedgerActionResultDto { ResultCode = "LeaseNotFound" };
            }

            var normalizedAction = action?.Trim().ToLowerInvariant() ?? string.Empty;
            var month = new DateTime(periodMonthUtc.Year, periodMonthUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var payments = await QueryManyAsync(coreContainer, tenantId, "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.type = 'payment' AND c.data.leaseId = @leaseId", cancellationToken, ("@leaseId", (object)leaseId.ToString()));
            var existingPayment = payments.FirstOrDefault(x => new DateTime(GetDateTime(x, "dueUtc").Year, GetDateTime(x, "dueUtc").Month, 1) == month);

            switch (normalizedAction)
            {
                case "markpaid":
                    if (existingPayment is null)
                    {
                        existingPayment = CreateTenantDocument("payment", Guid.NewGuid().ToString(), tenantId.ToString(), new Dictionary<string, object?>
                        {
                            ["leaseId"] = leaseId.ToString(),
                            ["assetId"] = GetString(leaseDocument, "assetId"),
                            ["amount"] = GetDecimal(leaseDocument, "monthlyRent"),
                            ["dueUtc"] = month,
                            ["createdUtc"] = DateTime.UtcNow,
                            ["modifiedUtc"] = null
                        });
                    }

                    SetValue(existingPayment, "status", "Paid");
                    SetValue(existingPayment, "paidUtc", DateTime.UtcNow);
                    SetValue(existingPayment, "modifiedUtc", DateTime.UtcNow);
                    await coreContainer.UpsertItemAsync(existingPayment, new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);
                    return new Runtira.Application.Features.RuntiraRentLedgerActionResultDto { Success = true, ResultCode = "Updated", Status = "Paid" };

                case "markunpaid":
                    if (existingPayment is not null)
                    {
                        await coreContainer.DeleteItemAsync<CosmosDocument>(existingPayment.id, new PartitionKey(tenantId.ToString()), cancellationToken: cancellationToken);
                    }

                    return new Runtira.Application.Features.RuntiraRentLedgerActionResultDto { Success = true, ResultCode = "Updated", Status = month < DateTime.UtcNow ? "Late" : "Pending" };

                default:
                    return new Runtira.Application.Features.RuntiraRentLedgerActionResultDto { ResultCode = "UnsupportedAction" };
            }
        }
    }
}
