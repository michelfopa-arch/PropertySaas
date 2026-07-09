namespace Runtira.Application.Abstractions
{
    public interface IRentInvoiceArchiveStore
    {
        Task RecordAsync(string tenantSlug, Runtira.Application.Features.RuntiraRentInvoiceDto invoice, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DateTime>> GetGeneratedMonthsAsync(string tenantSlug, Guid leaseId, CancellationToken cancellationToken = default);
    }
}
