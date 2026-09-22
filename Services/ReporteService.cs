using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using proyecto_asp.Models;
using System;
using System.Linq;
using System.Collections.Generic;

namespace proyecto_asp.Services
{
    public class ReporteService
    {
        static ReporteService()
        {
            // Validaciones QuestPDF heredadas de la configuración
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.UseSystemFonts = true;
            QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
            QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;
        }

        public byte[] GenerarReporteMensualPdf(List<Pedido> pedidos, DateTime mesActual)
        {
            var ingresos = pedidos.Sum(p => p.Total);
            var totalPedidos = pedidos.Count;
            var ticketPromedio = totalPedidos > 0 ? ingresos / totalPedidos : 0;

            // Agrupar por semana del mes
            var semanas = new decimal[4];
            foreach (var p in pedidos)
            {
                var dia = p.FechaPedido.Day;
                if (dia <= 7) semanas[0] += p.Total;
                else if (dia <= 14) semanas[1] += p.Total;
                else if (dia <= 21) semanas[2] += p.Total;
                else semanas[3] += p.Total;
            }

            var maxVentaSemana = semanas.Max();
            if (maxVentaSemana == 0) maxVentaSemana = 1; // evitar dividir por cero

            // Agrupar detalles por producto
            var detallesPorProducto = pedidos
                .Where(p => p.Detalles != null)
                .SelectMany(p => p.Detalles)
                .GroupBy(d => d.Product?.Name ?? "Producto Desconocido")
                .Select(g => new { 
                    Producto = g.Key, 
                    Unidades = g.Sum(x => x.Cantidad), 
                    Ingresos = g.Sum(x => x.Cantidad * x.PrecioUnitario) 
                })
                .OrderByDescending(x => x.Ingresos)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Segoe UI", "Arial"));

                    page.Content().Column(col =>
                    {
                        // Encabezado
                        col.Item().PaddingBottom(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Empanadas Pamelita").FontSize(22).Bold().FontColor(Colors.Indigo.Darken2);
                                c.Item().Text($"Reporte de ventas, 1 al {DateTime.DaysInMonth(mesActual.Year, mesActual.Month)} de {mesActual.ToString("MMMM")} de {mesActual.Year}").FontSize(14).FontColor(Colors.Grey.Medium);
                            });
                            row.ConstantItem(50).AlignRight().Text("M").FontSize(24).Bold().FontColor(Colors.White).BackgroundColor(Colors.Indigo.Medium);
                        });

                        col.Item().LineHorizontal(2).LineColor(Colors.Indigo.Lighten4);
                        col.Item().PaddingTop(10);

                        // Resumen
                        col.Item().Border(1).BorderColor(Colors.Indigo.Lighten3).Background(Colors.Grey.Lighten4).Padding(15).Row(row =>
                        {
                            row.RelativeItem().Column(c => {
                                c.Item().Text($"Bs. {ingresos:F2}").FontSize(20).Bold();
                                c.Item().Text("Ingresos").FontSize(12).FontColor(Colors.Grey.Darken1);
                            });
                            row.RelativeItem().Column(c => {
                                c.Item().Text(totalPedidos.ToString()).FontSize(20).Bold();
                                c.Item().Text("Pedidos").FontSize(12).FontColor(Colors.Grey.Darken1);
                            });
                            row.RelativeItem().Column(c => {
                                c.Item().Text($"Bs. {ticketPromedio:F2}").FontSize(20).Bold();
                                c.Item().Text("Ticket promedio").FontSize(12).FontColor(Colors.Grey.Darken1);
                            });
                        });

                        // Gráfico de Barras (Semanas)
                        col.Item().PaddingTop(15).Border(1).BorderColor(Colors.Indigo.Lighten3).Padding(15).Column(c =>
                        {
                            c.Item().PaddingBottom(10).Text("Evolución de ventas por semana").Bold().FontColor(Colors.Indigo.Darken2);
                            
                            c.Item().Height(150).Row(row =>
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    var alturaRelativa = (float)(semanas[i] / maxVentaSemana);
                                    if (alturaRelativa < 0.05f) alturaRelativa = 0.05f; // min height
                                    
                                    row.RelativeItem().PaddingHorizontal(10).AlignBottom().Column(colBar => {
                                        colBar.Item().AlignCenter().Text($"Bs. {semanas[i]:F0}").FontSize(9);
                                        colBar.Item().Height(alturaRelativa * 120).Background(Colors.Indigo.Medium);
                                        colBar.Item().PaddingTop(5).AlignCenter().Text($"Sem. {i+1}").FontSize(10).Bold();
                                    });
                                }
                            });
                        });

                        // Detalle de productos
                        col.Item().PaddingTop(15).Border(1).BorderColor(Colors.Green.Lighten3).Padding(10).Column(c =>
                        {
                            c.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderStyle).Text("Producto");
                                    header.Cell().Element(HeaderStyle).AlignCenter().Text("Unid.");
                                    header.Cell().Element(HeaderStyle).AlignRight().Text("Ingresos");

                                    static IContainer HeaderStyle(IContainer con) =>
                                        con.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Green.Darken1).DefaultTextStyle(x => x.Bold().FontColor(Colors.Green.Darken3));
                                });

                                foreach (var item in detallesPorProducto)
                                {
                                    table.Cell().Element(CellStyle).Text(item.Producto);
                                    table.Cell().Element(CellStyle).AlignCenter().Text(item.Unidades.ToString());
                                    table.Cell().Element(CellStyle).AlignRight().Text($"Bs. {item.Ingresos:F2}");

                                    static IContainer CellStyle(IContainer con) => con.PaddingVertical(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
                                }
                            });
                        });
                    });
                    
                    page.Footer().AlignCenter().Text(x => {
                        x.Span("Generado el ").FontSize(9);
                        x.Span($"{DateTime.Now.ToLocalTime():dd/MM/yyyy HH:mm}").FontSize(9).Bold();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
