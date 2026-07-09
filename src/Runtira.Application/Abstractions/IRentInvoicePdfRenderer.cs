namespace Runtira.Application.Abstractions
{
    public interface IRentInvoicePdfRenderer
    {
        byte[] Render(Runtira.Application.Features.RuntiraRentInvoiceDto invoice);
    }
}
