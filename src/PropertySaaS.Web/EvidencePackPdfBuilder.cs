using PropertySaaS.Application.Common;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PropertySaaS.Web;

internal static class EvidencePackPdfBuilder
{
    public static byte[] Build(
        MaintenanceEvidenceSummaryDto evidence,
        IReadOnlyList<MediaAssetSummaryDto> assets,
        IReadOnlyList<MaintenanceCommunicationSummaryDto> communications,
        string organizationName,
        DateTime exportedUtc,
        Func<string, string> categoryLabelResolver)
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
                    column.Item().Text("Tribunal-ready evidence pack").FontSize(20).Bold();
                    column.Item().Text($"{organizationName} · Exported {exportedUtc:u}").FontColor(Colors.Grey.Darken2);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(12);

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(ticket =>
                    {
                        ticket.Item().Text("Ticket").Bold();
                        ticket.Item().Text(evidence.Title);
                        ticket.Item().Text($"Dossier signature: {evidence.DossierSignature}");
                        ticket.Item().Text($"Property: {evidence.PropertyName} {(string.IsNullOrWhiteSpace(evidence.UnitLabel) ? string.Empty : $"- Unit {evidence.UnitLabel}")}");
                        ticket.Item().Text($"Status: {evidence.Status}");
                        ticket.Item().Text($"Opened: {evidence.RequestedDate}");
                        ticket.Item().Text($"Evidence items: {evidence.EvidenceCount}");
                    });

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(section =>
                    {
                        section.Item().Text("LTB-ready sections").Bold();
                        section.Item().Text("1. Issue summary and affected unit");
                        section.Item().Text("2. Chronology of visits, photos and updates");
                        section.Item().Text("3. Notices and resident communications");
                        section.Item().Text("4. Attached proof, before/after items and vendor evidence");
                    });

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(timeline =>
                    {
                        timeline.Item().Text("Timeline items").Bold();
                        if (assets.Count == 0)
                        {
                            timeline.Item().Text("No linked evidence assets yet.");
                        }
                        else
                        {
                            foreach (var asset in assets.OrderBy(x => x.SortOrder).ThenBy(x => x.CreatedUtc))
                            {
                                timeline.Item().Text($"{asset.CreatedUtc.ToLocalTime():g} - {categoryLabelResolver(asset.Category)} - {asset.FileName} - {asset.Caption}");
                            }
                        }
                    });

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(notice =>
                    {
                        notice.Item().Text("Notice and communication summary").Bold();
                        if (communications.Count == 0)
                        {
                            notice.Item().Text("Use this section to summarize notice delivery, resident updates and service attempts before presenting the pack externally.");
                        }
                        else
                        {
                            foreach (var message in communications.OrderBy(x => x.SentUtc))
                            {
                                var direction = message.IsIncoming ? "Incoming" : "Outgoing";
                                var delivered = message.DeliveredUtc.HasValue ? message.DeliveredUtc.Value.ToLocalTime().ToString("g") : "not marked delivered";
                                var method = string.IsNullOrWhiteSpace(message.DeliveryMethod) ? "logged" : message.DeliveryMethod;
                                var proof = string.IsNullOrWhiteSpace(message.DeliveryProof) ? string.Empty : $"; {message.DeliveryProof}";
                                notice.Item().Text($"{message.SentUtc.ToLocalTime():g} - {direction} - {message.Body} ({method}, {delivered}{proof})");
                            }
                        }
                    });

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(summary =>
                    {
                        summary.Item().Text("Evidence pack preview").Bold();
                        summary.Item().Text(evidence.EvidencePackSummary);
                    });
                });
            });
        }).GeneratePdf();
    }
}
