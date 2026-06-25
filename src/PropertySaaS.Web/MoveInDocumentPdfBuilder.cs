using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PropertySaaS.Web;

internal static class MoveInDocumentPdfBuilder
{
    public static byte[] Build(
        string organizationName,
        string propertyName,
        string unitLabel,
        string tenantName,
        string documentTitle,
        string subtitle,
        IReadOnlyList<string> bulletPoints,
        DateTime exportedUtc)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(32);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().Text(documentTitle).FontSize(20).Bold();
                    column.Item().Text(subtitle).FontColor(Colors.Grey.Darken2);
                    column.Item().Text($"{organizationName} · Exported {exportedUtc:u}").FontColor(Colors.Grey.Darken2);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(12);

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(details =>
                    {
                        details.Item().Text("Lease onboarding context").Bold();
                        details.Item().Text($"Tenant: {tenantName}");
                        details.Item().Text($"Property: {propertyName}");
                        details.Item().Text($"Unit: {unitLabel}");
                    });

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(section =>
                    {
                        section.Item().Text("Checklist").Bold();
                        foreach (var bullet in bulletPoints)
                        {
                            section.Item().Text($"• {bullet}");
                        }
                    });

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(section =>
                    {
                        section.Item().Text("Operations note").Bold();
                        section.Item().Text("Use this internal document in demos to show that move-in follow-up, auditability and package readiness live in the same workflow as the official Ontario lease and notices.");
                    });
                });
            });
        }).GeneratePdf();
    }
}