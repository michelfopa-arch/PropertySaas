using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Runtira.Application.Abstractions;
using Runtira.Application.Features;

namespace Runtira.Infrastructure.Services
{
    internal sealed class AlbertaMinimalRentInvoicePdfRenderer : IRentInvoicePdfRenderer
    {
        static AlbertaMinimalRentInvoicePdfRenderer()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] Render(RuntiraRentInvoiceDto invoice)
        {
            var periodLabel = invoice.PeriodMonthUtc.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("en-CA"));
            var culture = CultureInfo.GetCultureInfo("en-CA");

            // The due date recurs every month on the lease's start day-of-month (e.g. a lease starting
            // July 15 is due on the 15th every month), clamped to the last valid day of shorter months.
            var daysInPeriodMonth = DateTime.DaysInMonth(invoice.PeriodMonthUtc.Year, invoice.PeriodMonthUtc.Month);
            var dueDay = Math.Min(invoice.LeaseStartUtc.Day, daysInPeriodMonth);
            var dueDate = new DateTime(invoice.PeriodMonthUtc.Year, invoice.PeriodMonthUtc.Month, dueDay);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Column(column =>
                    {
                        column.Item().Text("Rent Invoice").FontSize(20).Bold();
                        column.Item().Text(invoice.JurisdictionDisplayName).FontSize(10).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingVertical(15).Column(column =>
                    {
                        column.Spacing(12);

                        column.Item().AlignRight().Column(right =>
                        {
                            right.Item().AlignRight().Text("Invoice period").FontSize(9).FontColor(Colors.Grey.Darken1);
                            right.Item().AlignRight().Text(periodLabel).Bold();
                        });

                        column.Item().LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten1);

                        column.Item().Column(details =>
                        {
                            details.Item().Text("Rental property").FontSize(9).FontColor(Colors.Grey.Darken1);
                            details.Item().Text(invoice.PropertyAddress).Bold();
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text("Resident").FontSize(9).FontColor(Colors.Grey.Darken1);
                                left.Item().Text(invoice.ResidentName).Bold();
                            });

                            if (!string.IsNullOrWhiteSpace(invoice.UnitCode))
                            {
                                row.RelativeItem().Column(right =>
                                {
                                    right.Item().Text("Unit").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    right.Item().Text(invoice.UnitCode).Bold();
                                });
                            }
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text("Lease term").FontSize(9).FontColor(Colors.Grey.Darken1);
                                left.Item().Text(FormatLeaseTerm(invoice));
                            });

                            row.RelativeItem().Column(right =>
                            {
                                right.Item().Text("Billing period").FontSize(9).FontColor(Colors.Grey.Darken1);
                                right.Item().Text(invoice.BillingPeriod);
                            });
                        });

                        column.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(6).Text("Description").Bold();
                                header.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text("Amount").Bold();
                            });

                            table.Cell().Border(0.5f).Padding(6).Text($"Monthly rent — {periodLabel}");
                            table.Cell().Border(0.5f).Padding(6).AlignRight().Text(invoice.MonthlyRent.ToString("C", culture));

                            if (invoice.AddAutomaticSalesTax)
                            {
                                table.Cell().Border(0.5f).Padding(6).Text("GST");
                                table.Cell().Border(0.5f).Padding(6).AlignRight().Text((0m).ToString("C", culture));
                            }

                            table.Cell().Border(0.5f).Padding(6).Text("Total due").Bold();
                            table.Cell().Border(0.5f).Padding(6).AlignRight().Text(invoice.MonthlyRent.ToString("C", culture)).Bold();
                        });

                        column.Item().PaddingTop(6).Text($"Due date: {dueDate:MMMM d, yyyy}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span($"Generated {DateTime.UtcNow:yyyy-MM-dd}.").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static string FormatLeaseTerm(RuntiraRentInvoiceDto invoice)
        {
            var start = invoice.LeaseStartUtc.ToString("MMM d, yyyy");
            var end = invoice.LeaseEndUtc.HasValue ? invoice.LeaseEndUtc.Value.ToString("MMM d, yyyy") : "ongoing";
            return $"{start} – {end}";
        }
    }
}
