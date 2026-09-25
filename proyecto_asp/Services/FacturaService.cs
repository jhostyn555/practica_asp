using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using proyecto_asp.Models;

namespace proyecto_asp.Services
{
    public class FacturaService
    {
        static FacturaService()
        {
            // Licencia Community de QuestPDF (gratuita)
            QuestPDF.Settings.License = LicenseType.Community;

            // QuestPDF 2026.9+: habilitar fuentes del sistema para usar fonts instaladas en el OS
            QuestPDF.Settings.UseSystemFonts = true;

            // Evitar crash si algún glifo o familia de fuente no está disponible
            QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
            QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;
        }

        public byte[] GenerarFacturaPdf(Pedido pedido)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // Encabezado
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("EMPANADAS PAMELITA").FontSize(22).Bold().FontColor(Colors.Orange.Darken2);
                            col.Item().Text("Comprobante de Venta Presencial - POS").FontSize(11).FontColor(Colors.Grey.Medium);
                            col.Item().Text("Atención en Local / Caja").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });

                        row.ConstantItem(180).Column(col =>
                        {
                            col.Item().Text($"FACTURA #{pedido.Id}").FontSize(16).Bold().AlignRight().FontColor(Colors.Orange.Darken3);
                            col.Item().Text($"Fecha: {pedido.FechaPedido.ToLocalTime():dd/MM/yyyy HH:mm}").FontSize(9).AlignRight();
                            col.Item().Text($"Estado: {pedido.Estado}").FontSize(9).Bold().AlignRight().FontColor(Colors.Green.Darken2);
                        });
                    });

                    // Contenido principal
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        // Bloque de Información de Cliente y Pago
                        col.Item().Background(Colors.Orange.Lighten5).Border(1).BorderColor(Colors.Orange.Lighten3).Padding(10).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t =>
                                {
                                    t.Span("Cliente / Ref: ").Bold();
                                    t.Span(string.IsNullOrWhiteSpace(pedido.User?.FullName) ? "Venta en Local (Presencial)" : pedido.User.FullName);
                                });
                                c.Item().Text(t =>
                                {
                                    t.Span("Método de Pago: ").Bold();
                                    t.Span(pedido.MetodoPago ?? "Efectivo");
                                });
                            });

                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t =>
                                {
                                    t.Span("Nro. Pedido: ").Bold();
                                    t.Span($"#{pedido.Id}");
                                });
                                c.Item().Text(t =>
                                {
                                    t.Span("Moneda: ").Bold();
                                    t.Span("Bolivianos (Bs.)");
                                });
                            });
                        });

                        // Tabla de Detalles
                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(35);
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("#");
                                header.Cell().Element(HeaderStyle).Text("Producto");
                                header.Cell().Element(HeaderStyle).AlignRight().Text("Precio Unit.");
                                header.Cell().Element(HeaderStyle).AlignCenter().Text("Cant.");
                                header.Cell().Element(HeaderStyle).AlignRight().Text("Subtotal");

                                static IContainer HeaderStyle(IContainer c)
                                {
                                    return c.Background(Colors.Orange.Darken1)
                                            .Padding(6)
                                            .DefaultTextStyle(x => x.Bold().FontColor(Colors.White));
                                }
                            });

                            int i = 1;
                            if (pedido.Detalles != null && pedido.Detalles.Any())
                            {
                                foreach (var d in pedido.Detalles)
                                {
                                    var bg = (i % 2 == 0) ? Colors.Grey.Lighten4 : Colors.White;
                                    decimal subtotal = d.Cantidad * d.PrecioUnitario;

                                    table.Cell().Element(c => CellStyle(c, bg)).Text(i.ToString());
                                    table.Cell().Element(c => CellStyle(c, bg)).Text(d.Product?.Name ?? "Producto");
                                    table.Cell().Element(c => CellStyle(c, bg)).AlignRight().Text($"Bs. {d.PrecioUnitario:F2}");
                                    table.Cell().Element(c => CellStyle(c, bg)).AlignCenter().Text(d.Cantidad.ToString());
                                    table.Cell().Element(c => CellStyle(c, bg)).AlignRight().Text($"Bs. {subtotal:F2}");
                                    i++;
                                }
                            }

                            static IContainer CellStyle(IContainer c, string bg)
                            {
                                return c.Background(bg).Padding(6).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
                            }
                        });

                        // Total
                        col.Item().PaddingTop(15).AlignRight().Text(t =>
                        {
                            t.Span("TOTAL GENERAL: ").FontSize(14).Bold();
                            t.Span($"Bs. {pedido.Total:F2}").FontSize(16).Bold().FontColor(Colors.Green.Darken2);
                        });
                    });

                    // Pie de página
                    page.Footer().AlignCenter().Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(5).Text("Empanadas Pamelita — Sistema de Gestión POS").FontSize(9).Bold().FontColor(Colors.Orange.Darken2);
                        col.Item().Text("¡Gracias por su compra y preferencia!").FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerarFacturaExcel(Pedido pedido)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Factura POS");

                // Estilo general
                worksheet.Style.Font.FontName = "Segoe UI";

                // Título principal
                worksheet.Cell("A1").Value = "EMPANADAS PAMELITA";
                worksheet.Cell("A1").Style.Font.Bold = true;
                worksheet.Cell("A1").Style.Font.FontSize = 18;
                worksheet.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#FF6B00");

                worksheet.Cell("A2").Value = "Comprobante / Factura de Venta POS";
                worksheet.Cell("A2").Style.Font.Italic = true;
                worksheet.Cell("A2").Style.Font.FontSize = 11;
                worksheet.Cell("A2").Style.Font.FontColor = XLColor.Gray;

                // Información del pedido
                worksheet.Cell("A4").Value = "Nro. Pedido:";
                worksheet.Cell("B4").Value = $"#{pedido.Id}";
                worksheet.Cell("A4").Style.Font.Bold = true;

                worksheet.Cell("A5").Value = "Fecha:";
                worksheet.Cell("B5").Value = pedido.FechaPedido.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell("A5").Style.Font.Bold = true;

                worksheet.Cell("D4").Value = "Método de Pago:";
                worksheet.Cell("E4").Value = pedido.MetodoPago ?? "Efectivo";
                worksheet.Cell("D4").Style.Font.Bold = true;

                worksheet.Cell("D5").Value = "Estado:";
                worksheet.Cell("E5").Value = pedido.Estado;
                worksheet.Cell("D5").Style.Font.Bold = true;

                // Encabezados de la tabla de detalles
                int startRow = 7;
                worksheet.Cell(startRow, 1).Value = "#";
                worksheet.Cell(startRow, 2).Value = "Producto";
                worksheet.Cell(startRow, 3).Value = "Precio Unitario (Bs.)";
                worksheet.Cell(startRow, 4).Value = "Cantidad";
                worksheet.Cell(startRow, 5).Value = "Subtotal (Bs.)";

                var headerRange = worksheet.Range(startRow, 1, startRow, 5);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FF6B00");
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int currentRow = startRow + 1;
                int itemIndex = 1;

                if (pedido.Detalles != null && pedido.Detalles.Any())
                {
                    foreach (var d in pedido.Detalles)
                    {
                        decimal subtotal = d.Cantidad * d.PrecioUnitario;
                        worksheet.Cell(currentRow, 1).Value = itemIndex++;
                        worksheet.Cell(currentRow, 2).Value = d.Product?.Name ?? "Producto";
                        worksheet.Cell(currentRow, 3).Value = d.PrecioUnitario;
                        worksheet.Cell(currentRow, 4).Value = d.Cantidad;
                        worksheet.Cell(currentRow, 5).Value = subtotal;

                        worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(currentRow, 3).Style.NumberFormat.Format = "#,##0.00";
                        worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0.00";

                        currentRow++;
                    }
                }

                // Fila de Total
                worksheet.Cell(currentRow, 4).Value = "TOTAL GENERAL:";
                worksheet.Cell(currentRow, 4).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell(currentRow, 5).Value = pedido.Total;
                worksheet.Cell(currentRow, 5).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 5).Style.Font.FontSize = 12;
                worksheet.Cell(currentRow, 5).Style.Font.FontColor = XLColor.FromHtml("#2E7D32");
                worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0.00";

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}
