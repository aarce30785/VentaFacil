using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Geom;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using System.IO;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Services.PDF
{
    public class PdfService : IPdfService
    {
        private readonly Microsoft.Extensions.Logging.ILogger<PdfService> _logger;

        public PdfService(Microsoft.Extensions.Logging.ILogger<PdfService> logger)
        {
            _logger = logger;
        }

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

        public byte[] GenerarFacturaPdf(FacturaDto facturaDto, bool esCopia = false)
        {
            _logger.LogInformation("Inicio de generación de PDF para factura {FacturaId}", facturaDto?.NumeroFactura);
            try 
            {
                using (var stream = new MemoryStream())
                {
                    var writer = new PdfWriter(stream);
                    writer.SetCloseStream(false); // Evitar cerrar el MemoryStream al cerrar el documento
                    var pdf = new PdfDocument(writer);
                    var document = new Document(pdf, PageSize.A4);

                var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                if (esCopia)
                {
                     // Marca de agua si es copia (impl simple)
                     // En iText7 esto es más complejo con event handlers, por simplicidad agregamos un texto:
                    document.Add(new Paragraph("***** COPIA *****").SetFont(fontBold).SetFontSize(20).SetTextAlignment(TextAlignment.CENTER).SetFontColor(iText.Kernel.Colors.ColorConstants.GRAY));
                }

                // Encabezado
                document.Add(new Paragraph($"Factura #{facturaDto.NumeroFactura}")
                    .SetFont(fontBold).SetFontSize(18).SetTextAlignment(TextAlignment.CENTER));
                
                document.Add(new Paragraph($"Fecha: {facturaDto.FechaEmision:dd/MM/yyyy HH:mm}")
                    .SetFont(fontRegular).SetFontSize(12).SetTextAlignment(TextAlignment.RIGHT));

                document.Add(new Paragraph($"Cliente: {facturaDto.Cliente}")
                   .SetFont(fontRegular).SetFontSize(12));

                 // Tabla Items
                 document.Add(new Paragraph("\n"));
                 var table = new Table(UnitValue.CreatePercentArray(new float[] { 4, 1, 2, 2 }));
                 table.SetWidth(UnitValue.CreatePercentValue(100));

                 table.AddHeaderCell(new Cell().Add(new Paragraph("Producto").SetFont(fontBold)));
                 table.AddHeaderCell(new Cell().Add(new Paragraph("Cant").SetFont(fontBold)));
                 table.AddHeaderCell(new Cell().Add(new Paragraph("Precio").SetFont(fontBold)));
                 table.AddHeaderCell(new Cell().Add(new Paragraph("Total").SetFont(fontBold)));

                 foreach(var item in facturaDto.Items)
                 {
                     table.AddCell(new Paragraph(item.NombreProducto));
                     table.AddCell(new Paragraph(item.Cantidad.ToString()));
                     table.AddCell(new Paragraph(item.PrecioUnitario.ToString("N2")));
                     table.AddCell(new Paragraph(item.Subtotal.ToString("N2")));
                 }
                 document.Add(table);

                 // Totales
                 document.Add(new Paragraph("\n"));
                 document.Add(new Paragraph($"Subtotal: {facturaDto.Subtotal:N2}").SetTextAlignment(TextAlignment.RIGHT));
                 document.Add(new Paragraph($"Impuestos: {facturaDto.Impuestos:N2}").SetTextAlignment(TextAlignment.RIGHT));
                 document.Add(new Paragraph($"TOTAL: {facturaDto.Total:N2} {facturaDto.Moneda}").SetFont(fontBold).SetFontSize(14).SetTextAlignment(TextAlignment.RIGHT));

                document.Close();
                
                 var bytes = stream.ToArray();
                 _logger.LogInformation("PDF generado correctamente. Tamaño: {Bytes} bytes", bytes.Length);
                 return bytes;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fatal generando PDF");
                throw;
            }
        }

        public byte[] GenerarHistorialMovimientosPdf(List<InventarioMovimientoDto> movimientos, string nombreInsumo)
        {
            _logger.LogInformation("Inicio de generación de PDF Historial para {Insumo}", nombreInsumo);
            try
            {
                using (var stream = new MemoryStream())
                {
                    var writer = new PdfWriter(stream);
                    writer.SetCloseStream(false);
                    var pdf = new PdfDocument(writer);
                    var document = new Document(pdf, PageSize.A4);
                     var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                    document.Add(new Paragraph($"Historial de Movimientos - {nombreInsumo}")
                        .SetFont(fontBold).SetFontSize(16).SetTextAlignment(TextAlignment.CENTER));
                
                     document.Add(new Paragraph("\n"));

                     var table = new Table(UnitValue.CreatePercentArray(new float[] { 2, 2, 1 }));
                     table.SetWidth(UnitValue.CreatePercentValue(100));

                     table.AddHeaderCell("Fecha");
                     table.AddHeaderCell("Tipo");
                     table.AddHeaderCell("Cant");

                     foreach(var mov in movimientos)
                     {
                         table.AddCell(mov.Fecha.ToString("dd/MM/yyyy HH:mm"));
                         table.AddCell(mov.Tipo_Movimiento);
                         table.AddCell(mov.Cantidad.ToString());
                     }
                     document.Add(table);

                    document.Close();
                    var bytes = stream.ToArray();
                    _logger.LogInformation("PDF Historial generado correctamente. Tamaño: {Bytes} bytes", bytes.Length);
                    return bytes;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fatal generando PDF Historial");
                throw;
            }
        }
    }
}