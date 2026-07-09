namespace Runtira.Application.Features
{
    public sealed partial class RuntiraWorkspaceService
    {
        public async Task<IReadOnlyList<RuntiraLeaseInvoiceEntryDto>> GetLeaseInvoiceEntriesAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = ResolveTenantId();
            if (!tenantId.HasValue || _assetWorkspaceStore is null)
            {
                return Array.Empty<RuntiraLeaseInvoiceEntryDto>();
            }

            var entries = await _assetWorkspaceStore.GetLeaseInvoiceEntriesAsync(tenantId.Value, cancellationToken);
            if (_rentInvoiceArchiveStore is null)
            {
                return entries;
            }

            var slug = _currentOrganization.OrganizationSlug;
            var enriched = new List<RuntiraLeaseInvoiceEntryDto>(entries.Count);
            foreach (var entry in entries)
            {
                var months = await _rentInvoiceArchiveStore.GetGeneratedMonthsAsync(slug, entry.LeaseId, cancellationToken);
                enriched.Add(new RuntiraLeaseInvoiceEntryDto
                {
                    LeaseId = entry.LeaseId,
                    PropertyName = entry.PropertyName,
                    PropertyAddress = entry.PropertyAddress,
                    PropertySlug = entry.PropertySlug,
                    UnitCode = entry.UnitCode,
                    ResidentName = entry.ResidentName,
                    MonthlyRent = entry.MonthlyRent,
                    BillingPeriod = entry.BillingPeriod,
                    LeaseStatus = entry.LeaseStatus,
                    LeaseStartUtc = entry.LeaseStartUtc,
                    LeaseEndUtc = entry.LeaseEndUtc,
                    AvailableMonths = months
                });
            }

            return enriched;
        }

        public async Task<RuntiraExportFileDto?> GenerateRentInvoicePdfAsync(Guid leaseId, int monthOffset, CancellationToken cancellationToken = default)
        {
            if (_rentInvoicePdfRenderer is null || _assetWorkspaceStore is null)
            {
                return null;
            }

            var tenantId = ResolveTenantId();
            if (!tenantId.HasValue)
            {
                return null;
            }

            var entries = await _assetWorkspaceStore.GetLeaseInvoiceEntriesAsync(tenantId.Value, cancellationToken);
            var lease = entries.FirstOrDefault(x => x.LeaseId == leaseId);
            if (lease is null)
            {
                return null;
            }

            var today = DateTime.UtcNow;
            var periodMonthUtc = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(monthOffset);

            var invoice = new RuntiraRentInvoiceDto
            {
                LeaseId = lease.LeaseId,
                OrganizationName = _currentOrganization.OrganizationName,
                PropertyAddress = lease.PropertyAddress,
                ResidentName = lease.ResidentName,
                UnitCode = lease.UnitCode,
                MonthlyRent = lease.MonthlyRent,
                BillingPeriod = lease.BillingPeriod,
                LeaseStartUtc = lease.LeaseStartUtc,
                LeaseEndUtc = lease.LeaseEndUtc,
                PeriodMonthUtc = periodMonthUtc,
                JurisdictionDisplayName = $"{_currentOrganization.CountryCode}-{_currentOrganization.Province}",
                RegionCode = _currentOrganization.Province,
                AddAutomaticSalesTax = false
            };

            var content = _rentInvoicePdfRenderer.Render(invoice);

            if (_rentInvoiceArchiveStore is not null)
            {
                await _rentInvoiceArchiveStore.RecordAsync(_currentOrganization.OrganizationSlug, invoice, cancellationToken);
            }

            var slug = string.IsNullOrWhiteSpace(_currentOrganization.OrganizationSlug) ? "workspace" : _currentOrganization.OrganizationSlug;
            var fileName = $"{slug}-invoice-{periodMonthUtc:yyyy-MM}-{(string.IsNullOrWhiteSpace(lease.UnitCode) ? lease.ResidentName : lease.UnitCode)}.pdf";

            return new RuntiraExportFileDto
            {
                FileName = SanitizeFileName(fileName),
                ContentType = "application/pdf",
                Content = content
            };
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Select(c => invalidChars.Contains(c) || c == ' ' ? '-' : c).ToArray());
            return sanitized.Replace("--", "-");
        }
    }
}
