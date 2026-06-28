using PropertySaaS.Application.Common;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PropertySaaS.Web;

internal static class InvoicePdfBuilder
{
    public static byte[] Build(InvoiceDocumentDto invoice, DateTime exportedUtc)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var isFrench = string.Equals(invoice.PreferredLanguage, "fr-CA", StringComparison.OrdinalIgnoreCase)
            || string.Equals(invoice.PreferredLanguage, "fr", StringComparison.OrdinalIgnoreCase);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(32);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().Text(isFrench ? "Facture locative" : "Rental invoice").FontSize(20).Bold();
                    column.Item().Text($"{invoice.OrganizationName} · {(isFrench ? "Exporté" : "Exported")} {exportedUtc:u}").FontColor(Colors.Grey.Darken2);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(12);

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(section =>
                    {
                        section.Item().Text(isFrench ? "Résumé" : "Summary").Bold();
                        section.Item().Text($"{(isFrench ? "Facture" : "Invoice")}: {invoice.Number}");
                        section.Item().Text($"{(isFrench ? "Période" : "Period")}: {invoice.BillingPeriodLabel}");
                        section.Item().Text($"{(isFrench ? "Locataire" : "Tenant")}: {invoice.TenantName}");
                        section.Item().Text($"{(isFrench ? "Courriel" : "Email")}: {invoice.TenantEmail}");
                        section.Item().Text($"{(isFrench ? "Bien" : "Property")}: {invoice.PropertyName}");
                        section.Item().Text($"{(isFrench ? "Adresse" : "Address")}: {invoice.PropertyAddress}");
                        section.Item().Text($"{(isFrench ? "Unité" : "Unit")}: {invoice.UnitLabel}");
                    });

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(section =>
                    {
                        section.Item().Text(isFrench ? "Montants" : "Amounts").Bold();
                        section.Item().Text($"{(isFrench ? "Montant" : "Amount")}: {invoice.Amount:C}");
                        section.Item().Text($"{(isFrench ? "Solde" : "Balance")}: {invoice.Balance:C}");
                        section.Item().Text($"{(isFrench ? "Échéance" : "Due date")}: {invoice.DueDate}");
                        section.Item().Text($"{(isFrench ? "Statut" : "Status")}: {invoice.Status}");
                    });

                    if (!string.IsNullOrWhiteSpace(invoice.Notes))
                    {
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(section =>
                        {
                            section.Item().Text(isFrench ? "Notes" : "Notes").Bold();
                            section.Item().Text(invoice.Notes);
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(invoice.StandardInstructions))
                    {
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(section =>
                        {
                            section.Item().Text(isFrench ? "Instruction" : "Instruction").Bold();
                            section.Item().Text(invoice.StandardInstructions);
                        });
                    }
                });
            });
        }).GeneratePdf();
    }
}
