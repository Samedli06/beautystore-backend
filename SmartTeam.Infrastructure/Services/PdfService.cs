using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;

namespace SmartTeam.Infrastructure.Services;

public class PdfService : IPdfService
{
    public PdfService()
    {
        // Set QuestPDF license to Community
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateOrderReceiptAsync(OrderDto order, CancellationToken ct = default)
    {
        var document = CreateOrderDocument(new List<OrderDto> { order });
        return Task.FromResult(document.GeneratePdf());
    }

    public Task<byte[]> GenerateBulkOrderReceiptsAsync(List<OrderDto> orders, CancellationToken ct = default)
    {
        var document = CreateOrderDocument(orders);
        return Task.FromResult(document.GeneratePdf());
    }

    private IDocument CreateOrderDocument(List<OrderDto> orders)
    {
        return Document.Create(container =>
        {
            foreach (var order in orders)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    page.Header().Element(h => ComposeHeader(h, order));
                    page.Content().Element(c => ComposeContent(c, order));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Səhifə ");
                        x.CurrentPageNumber();
                    });
                });
            }
        });
    }

    private void ComposeHeader(IContainer container, OrderDto order)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("avto027.az").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                col.Item().Text("Bakı şəhəri, Azərbaycan");
                col.Item().Text("Tel: +994 50 123 45 67");
                col.Item().Text("Email: info@avto.az");
            });

            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text("QAİMƏ").FontSize(24).SemiBold().FontColor(Colors.Grey.Darken2);
                col.Item().Text($"Sifariş No: #{order.OrderNumber}");
                col.Item().Text($"Tarix: {order.CreatedAt:dd.MM.yyyy HH:mm}");
            });
        });
    }

    private void ComposeContent(IContainer container, OrderDto order)
    {
        container.PaddingVertical(40).Column(column =>
        {
            column.Spacing(20);

            // Customer Info
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Sifarişçi Məlumatları:").SemiBold();
                    col.Item().PaddingTop(5).Text(order.CustomerName);
                    col.Item().Text(order.CustomerPhone);
                    col.Item().Text(order.ShippingAddress ?? "Ünvan qeyd edilməyib");
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("Sifariş Statusu:").SemiBold();
                    col.Item().PaddingTop(5).Text(order.Status).FontColor(
                        order.Status.ToLower() == "completed" ? Colors.Green.Medium :
                        order.Status.ToLower() == "cancelled" ? Colors.Red.Medium : Colors.Blue.Medium);
                });
            });

            // Items Table
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("#");
                    header.Cell().Element(CellStyle).Text("Məhsul Adı");
                    header.Cell().Element(CellStyle).AlignRight().Text("Say");
                    header.Cell().Element(CellStyle).AlignRight().Text("Vahid Qiymət");
                    header.Cell().Element(CellStyle).AlignRight().Text("Cəmi");

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold())
                                        .PaddingVertical(5)
                                        .Background(Colors.Grey.Lighten3)
                                        .BorderBottom(1)
                                        .BorderColor(Colors.Black);
                    }
                });

                // Rows
                int index = 1;
                foreach (var item in order.Items)
                {
                    table.Cell().Element(RowStyle).Text(index++.ToString());
                    table.Cell().Element(RowStyle).Text(item.ProductName);
                    table.Cell().Element(RowStyle).AlignRight().Text(item.Quantity.ToString());
                    table.Cell().Element(RowStyle).AlignRight().Text($"{item.UnitPrice:F2} AZN");
                    table.Cell().Element(RowStyle).AlignRight().Text($"{item.TotalPrice:F2} AZN");

                    static IContainer RowStyle(IContainer container)
                    {
                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                    }
                }
            });

            // Totals
            column.Item().AlignRight().Column(col =>
            {
                col.Spacing(5);
                
                if (order.DiscountAmount > 0)
                {
                    col.Item().Text($"Subtotal: {order.SubTotal:F2} AZN");
                    col.Item().Text($"Endirim: {order.DiscountAmount:F2} AZN").FontColor(Colors.Red.Medium);
                }

                col.Item().Text($"Yekun Məbləğ: {order.TotalAmount:F2} AZN").FontSize(14).SemiBold();
            });

            // Signatures
            column.Item().PaddingTop(50).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Təhvil verən (Satıcı):").SemiBold();
                    col.Item().PaddingTop(20).Text("_______________________");
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("Təhvil alan (Sifarişçi):").SemiBold();
                    col.Item().PaddingTop(20).Text("_______________________");
                });
            });

            // Notes
            if (!string.IsNullOrEmpty(order.Notes))
            {
                column.Item().PaddingTop(20).Column(col =>
                {
                    col.Item().Text("Qeydlər:").SemiBold();
                    col.Item().Text(order.Notes).Italic();
                });
            }
        });
    }
}
