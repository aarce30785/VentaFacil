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
             // TODO: Migrar lógica de iTextSharp a iText7
            throw new NotImplementedException("Método pendiente de migración a iText7");
        }

        public byte[] GenerarFacturaPdf(FacturaDto facturaDto)
        {
             return GenerarFacturaPdf(facturaDto, null, false);
        }

        public byte[] GenerarHistorialMovimientosPdf(List<InventarioMovimientoDto> movimientos, string nombreInsumo)
        {
            throw new NotImplementedException("Método pendiente de migración a iText7");
        }
    }
}