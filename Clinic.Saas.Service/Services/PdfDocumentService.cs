using Clinic.Saas.Service.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Clinic.Saas.Service.Services;

public sealed class PdfDocumentService : IPdfDocumentService
{
    public byte[] Generate(string title, IEnumerable<(string Label, string Value)> fields, IEnumerable<string>? lines = null)
    {
        var fieldList = fields.ToList();
        var lineList = lines?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? [];
        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(35);
            page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));
            page.Header().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(12)
                .Text(title).FontSize(22).Bold().FontColor(Colors.Blue.Darken2).AlignRight();
            page.Content().ContentFromRightToLeft().PaddingVertical(18).Column(column =>
            {
                column.Spacing(9);
                foreach (var (label, value) in fieldList)
                    column.Item().Row(row =>
                    {
                        row.RelativeItem(3).Text(value ?? "-").AlignRight();
                        row.RelativeItem(1).Text(label).Bold().AlignRight();
                    });
                if (lineList.Count > 0)
                {
                    column.Item().PaddingTop(12).BorderTop(1).BorderColor(Colors.Grey.Lighten2);
                    foreach (var line in lineList)
                        column.Item().Padding(7).Background(Colors.Grey.Lighten4).Text(line).AlignRight();
                }
            });
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("ClinicFlow • ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        })).GeneratePdf();
    }
}
