using Microsoft.Extensions.Logging;
using Runtira.Application.Abstractions;
using Runtira.Application.Features;

namespace Runtira.Infrastructure.Services
{
    internal sealed class LocalJsonRentInvoiceArchiveStore : IRentInvoiceArchiveStore
    {
        private readonly string _rootPath;
        private readonly ILogger<LocalJsonRentInvoiceArchiveStore> _logger;
        private static readonly System.Text.Json.JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };

        public LocalJsonRentInvoiceArchiveStore(ILogger<LocalJsonRentInvoiceArchiveStore> logger)
        {
            _logger = logger;
            _rootPath = System.IO.Path.Combine(AppContext.BaseDirectory, "App_Data", "invoices");
        }

        public Task RecordAsync(string tenantSlug, RuntiraRentInvoiceDto invoice, CancellationToken cancellationToken = default)
        {
            try
            {
                var leaseFolder = GetLeaseFolder(tenantSlug, invoice.LeaseId);
                System.IO.Directory.CreateDirectory(leaseFolder);

                var filePath = System.IO.Path.Combine(leaseFolder, $"{invoice.PeriodMonthUtc:yyyy-MM}.json");

                var record = new
                {
                    generatedUtc = DateTime.UtcNow,
                    invoice.LeaseId,
                    invoice.OrganizationName,
                    invoice.PropertyAddress,
                    invoice.ResidentName,
                    invoice.UnitCode,
                    invoice.MonthlyRent,
                    invoice.BillingPeriod,
                    invoice.LeaseStartUtc,
                    invoice.LeaseEndUtc,
                    invoice.PeriodMonthUtc,
                    invoice.JurisdictionDisplayName,
                    invoice.RegionCode,
                    invoice.AddAutomaticSalesTax
                };

                // Regenerating the same lease/month overwrites the previous JSON record; we never keep the rendered PDF itself.
                return System.IO.File.WriteAllTextAsync(filePath, System.Text.Json.JsonSerializer.Serialize(record, SerializerOptions), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record local rent invoice JSON archive for tenant {TenantSlug}.", tenantSlug);
                return Task.CompletedTask;
            }
        }

        public Task<IReadOnlyList<DateTime>> GetGeneratedMonthsAsync(string tenantSlug, Guid leaseId, CancellationToken cancellationToken = default)
        {
            try
            {
                var leaseFolder = GetLeaseFolder(tenantSlug, leaseId);
                if (!System.IO.Directory.Exists(leaseFolder))
                {
                    return Task.FromResult<IReadOnlyList<DateTime>>(Array.Empty<DateTime>());
                }

                var months = System.IO.Directory.GetFiles(leaseFolder, "*.json")
                    .Select(path => System.IO.Path.GetFileNameWithoutExtension(path))
                    .Where(name => DateTime.TryParseExact(name, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out _))
                    .Select(name => DateTime.SpecifyKind(DateTime.ParseExact(name, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture), DateTimeKind.Utc))
                    .OrderByDescending(x => x)
                    .ToList();

                return Task.FromResult<IReadOnlyList<DateTime>>(months);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read local rent invoice JSON archive for tenant {TenantSlug} and lease {LeaseId}.", tenantSlug, leaseId);
                return Task.FromResult<IReadOnlyList<DateTime>>(Array.Empty<DateTime>());
            }
        }

        private string GetLeaseFolder(string tenantSlug, Guid leaseId)
        {
            var slug = string.IsNullOrWhiteSpace(tenantSlug) ? "workspace" : tenantSlug;
            return System.IO.Path.Combine(_rootPath, slug, leaseId.ToString());
        }
    }
}
