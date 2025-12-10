using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Geom;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using System.IO;
using System;
using System.Collections.Generic;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Services.PDF
{
    public class PdfService : IPdfService
    {
        public byte[] GenerarReporteNomina(NominaDetalleDto data)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new PdfWriter(stream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf, PageSize.A4.Rotate()); // Horizontal para más espacio

                // Fuentes
                var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                // Título
                document.Add(new Paragraph(new Text($"Reporte de Nómina - {data.Estado}").SetFont(fontBold))
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(18));

                document.Add(new Paragraph($"Periodo: {data.FechaInicio:dd/MM/yyyy} - {data.FechaFinal:dd/MM/yyyy}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(12));

                document.Add(new Paragraph($"Generado el: {data.FechaGeneracion:dd/MM/yyyy HH:mm}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(10));

                document.Add(new Paragraph("\n"));

                // Tabla
                var table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 2, 1, 1, 1, 1, 1, 1 }));
                table.SetWidth(UnitValue.CreatePercentValue(100));

                // Encabezados
                table.AddHeaderCell(new Cell().Add(new Paragraph(new Text("Nombre").SetFont(fontBold))));
                table.AddHeaderCell(new Cell().Add(new Paragraph(new Text("Identificación").SetFont(fontBold))));
                table.AddHeaderCell(new Cell().Add(new Paragraph(new Text("Horas").SetFont(fontBold))));
                table.AddHeaderCell(new Cell().Add(new Paragraph(new Text("Extras").SetFont(fontBold))));
                table.AddHeaderCell(new Cell().Add(new Paragraph(new Text("Bonos").SetFont(fontBold))));
                table.AddHeaderCell(new Cell().Add(new Paragraph(new Text("Bruto").SetFont(fontBold))));
                table.AddHeaderCell(new Cell().Add(new Paragraph(new Text("Deducciones").SetFont(fontBold))));
                table.AddHeaderCell(new Cell().Add(new Paragraph(new Text("Neto").SetFont(fontBold))));

                foreach (var item in data.Detalles)
                {
                    table.AddCell(item.NombreUsuario);
                    table.AddCell(item.Identificacion);
                    table.AddCell(item.HorasTrabajadas.ToString());
                    table.AddCell(item.HorasExtras.ToString("N2"));
                    table.AddCell(item.Bonificaciones.ToString("N2"));
                    table.AddCell(item.SalarioBruto.ToString("N2"));
                    table.AddCell(item.Deducciones.ToString("N2"));
                    table.AddCell(item.SalarioNeto.ToString("N2"));
                }

                document.Add(table);

                // Totales
                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph(new Text($"Total Bruto: {data.TotalBruto:N2}").SetFont(fontBold)));
                document.Add(new Paragraph(new Text($"Total Deducciones: {data.TotalDeducciones:N2}").SetFont(fontBold)));
                document.Add(new Paragraph(new Text($"Total Neto: {data.TotalNeto:N2}").SetFont(fontBold).SetFontSize(14)));

                document.Close();
                return stream.ToArray();
            }
        }
        public byte[] GenerarFacturaPdf(FacturaDto facturaDto, object footer = null, bool esCopia = false)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new PdfWriter(stream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                // Fuentes
                var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                // Título
                string titulo = esCopia ? "FACTURA - COPIA" : "FACTURA";
                document.Add(new Paragraph(titulo)
                    .SetFont(fontBold)
                    .SetFontSize(20)
                    .SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph("\n"));

                // Información General
                float[] columnWidthsInfo = { 1, 3 };
                Table infoTable = new Table(UnitValue.CreatePercentArray(columnWidthsInfo)).UseAllAvailableWidth();
                
                infoTable.AddCell(new Cell().Add(new Paragraph("Factura #:").SetFont(fontBold)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                infoTable.AddCell(new Cell().Add(new Paragraph(facturaDto.NumeroFactura)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                
                infoTable.AddCell(new Cell().Add(new Paragraph("Fecha:").SetFont(fontBold)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                infoTable.AddCell(new Cell().Add(new Paragraph($"{facturaDto.FechaEmision:dd/MM/yyyy HH:mm}")).SetBorder(iText.Layout.Borders.Border.NO_BORDER));

                infoTable.AddCell(new Cell().Add(new Paragraph("Cliente:").SetFont(fontBold)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                infoTable.AddCell(new Cell().Add(new Paragraph(facturaDto.Cliente ?? "Consumidor Final")).SetBorder(iText.Layout.Borders.Border.NO_BORDER));

                infoTable.AddCell(new Cell().Add(new Paragraph("Estado:").SetFont(fontBold)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                infoTable.AddCell(new Cell().Add(new Paragraph(facturaDto.EstadoFactura.ToString())).SetBorder(iText.Layout.Borders.Border.NO_BORDER));

                document.Add(infoTable);
                document.Add(new Paragraph("\n"));

                // Tabla de Items
                var table = new Table(UnitValue.CreatePercentArray(new float[] { 4, 1, 2, 2 }));
                table.SetWidth(UnitValue.CreatePercentValue(100));

                table.AddHeaderCell(new Cell().Add(new Paragraph("Producto").SetFont(fontBold)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Cant").SetFont(fontBold)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Precio Unit").SetFont(fontBold)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Subtotal").SetFont(fontBold)));

                foreach (var item in facturaDto.Items)
                {
                    table.AddCell(new Paragraph(item.NombreProducto).SetFont(fontRegular));
                    table.AddCell(new Paragraph(item.Cantidad.ToString()).SetFont(fontRegular));
                    table.AddCell(new Paragraph(item.PrecioUnitario.ToString("C2")).SetFont(fontRegular));
                    table.AddCell(new Paragraph(item.Subtotal.ToString("C2")).SetFont(fontRegular));
                }

                document.Add(table);

                // Totales
                document.Add(new Paragraph("\n"));
                
                Table totalTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1 })).UseAllAvailableWidth();
                
                totalTable.AddCell(new Cell().Add(new Paragraph("Subtotal:").SetFont(fontBold).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                totalTable.AddCell(new Cell().Add(new Paragraph(facturaDto.Subtotal.ToString("C2")).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                
                totalTable.AddCell(new Cell().Add(new Paragraph("Impuestos:").SetFont(fontBold).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                totalTable.AddCell(new Cell().Add(new Paragraph(facturaDto.Impuestos.ToString("C2")).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));

                totalTable.AddCell(new Cell().Add(new Paragraph("TOTAL:").SetFont(fontBold).SetFontSize(14).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                totalTable.AddCell(new Cell().Add(new Paragraph(facturaDto.Total.ToString("C2")).SetFont(fontBold).SetFontSize(14).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));

                document.Add(totalTable);

                document.Close();
                return stream.ToArray();
            }
        }

        public byte[] GenerarFacturaPdf(FacturaDto facturaDto)
        {
             return GenerarFacturaPdf(facturaDto, null, false);
        }

        public byte[] GenerarHistorialMovimientosPdf(List<InventarioMovimientoDto> movimientos, string nombreInsumo)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new PdfWriter(stream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                // Fuentes
                var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                // Título
                document.Add(new Paragraph($"Historial de Movimientos - {nombreInsumo}")
                    .SetFont(fontBold)
                    .SetFontSize(18)
                    .SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph($"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph("\n"));

                // Tabla
                var table = new Table(UnitValue.CreatePercentArray(new float[] { 1, 2, 2, 2, 1 }));
                table.SetWidth(UnitValue.CreatePercentValue(100));

                // Encabezados
                table.AddHeaderCell(new Cell().Add(new Paragraph("ID").SetFont(fontBold)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Fecha").SetFont(fontBold)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Tipo").SetFont(fontBold)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Cantidad").SetFont(fontBold)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Usuario").SetFont(fontBold)));

                foreach (var item in movimientos)
                {
                    table.AddCell(new Paragraph(item.Id_Movimiento.ToString()).SetFont(fontRegular));
                    table.AddCell(new Paragraph(item.Fecha.ToString("dd/MM/yyyy HH:mm")).SetFont(fontRegular));
                    table.AddCell(new Paragraph(item.Tipo_Movimiento).SetFont(fontRegular));
                    table.AddCell(new Paragraph(item.Cantidad.ToString()).SetFont(fontRegular));
                    table.AddCell(new Paragraph(item.Id_Usuario.ToString()).SetFont(fontRegular));
                }

                document.Add(table);
                document.Close();
                return stream.ToArray();
            }
        }
    }
}