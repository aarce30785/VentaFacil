using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using VentaFacil.web.Data;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Services.PDF
{
    public class PdfService : IPdfService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PdfService> _logger;

        public PdfService(IWebHostEnvironment environment, ApplicationDbContext context, ILogger<PdfService> logger)
        {
            _environment = environment;
            _context = context;
            _logger = logger;
        }

        public byte[] GenerarFacturaPdf(FacturaDto facturaDto)
        {
            using (var memoryStream = new MemoryStream())
            {
               
                var document = new Document(PageSize.A4, 50, 50, 50, 50);
                var writer = PdfWriter.GetInstance(document, memoryStream);

                document.Open();

                
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                
                var title = new Paragraph("FACTURA", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20f
                };
                document.Add(title);

                
                var empresaTable = new PdfPTable(2)
                {
                    WidthPercentage = 100,
                    SpacingAfter = 20f
                };
                empresaTable.SetWidths(new float[] { 1, 1 });

                
                var empresaCell = new PdfPCell(new Phrase("VENTA FÁCIL\nDirección: Tu Dirección\nTeléfono: (123) 456-7890\nEmail: info@ventafacil.com", normalFont))
                {
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_LEFT
                };
                empresaTable.AddCell(empresaCell);

                
                var facturaInfo = $"Factura #: {facturaDto.NumeroFactura}\n" +
                                $"Fecha: {facturaDto.FechaEmision:dd/MM/yyyy}\n" +
                                $"Estado: {facturaDto.EstadoFactura}";
                var facturaCell = new PdfPCell(new Phrase(facturaInfo, normalFont))
                {
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                };
                empresaTable.AddCell(facturaCell);

                document.Add(empresaTable);

                
                if (!string.IsNullOrEmpty(facturaDto.Cliente))
                {
                    var clienteSection = new Paragraph("INFORMACIÓN DEL CLIENTE", headerFont)
                    {
                        SpacingBefore = 10f,
                        SpacingAfter = 10f
                    };
                    document.Add(clienteSection);

                    var clienteInfo = $"Cliente: {facturaDto.Cliente}";
                    var clienteParagraph = new Paragraph(clienteInfo, normalFont)
                    {
                        SpacingAfter = 20f
                    };
                    document.Add(clienteParagraph);
                }

               
                var itemsSection = new Paragraph("DETALLES DEL PEDIDO", headerFont)
                {
                    SpacingBefore = 10f,
                    SpacingAfter = 10f
                };
                document.Add(itemsSection);

                if (facturaDto.Items != null && facturaDto.Items.Any())
                {
                    var columnCount = facturaDto.Items.Any(i => i.Descuento.HasValue && i.Descuento.Value > 0) ? 5 : 4;
                    var itemsTable = new PdfPTable(columnCount)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 20f
                    };

                    
                    var widths = columnCount == 5 ?
                        new float[] { 3, 1, 1, 1, 1 } :
                        new float[] { 3, 1, 1, 1 };

                    itemsTable.SetWidths(widths);

                   
                    itemsTable.AddCell(new PdfPCell(new Phrase("Producto", headerFont)));
                    itemsTable.AddCell(new PdfPCell(new Phrase("Cantidad", headerFont)));
                    itemsTable.AddCell(new PdfPCell(new Phrase("Precio Unit.", headerFont)));

                    if (columnCount == 5)
                    {
                        itemsTable.AddCell(new PdfPCell(new Phrase("Desc.", headerFont)));
                    }

                    itemsTable.AddCell(new PdfPCell(new Phrase("Subtotal", headerFont)));

                    
                    foreach (var item in facturaDto.Items)
                    {
                        itemsTable.AddCell(new PdfPCell(new Phrase(item.NombreProducto ?? "N/A", normalFont)));
                        itemsTable.AddCell(new PdfPCell(new Phrase(item.Cantidad.ToString(), normalFont)));
                        itemsTable.AddCell(new PdfPCell(new Phrase(item.PrecioUnitario.ToString("C"), normalFont)));

                        if (columnCount == 5)
                        {
                            var descuento = item.Descuento?.ToString("C") ?? "-";
                            itemsTable.AddCell(new PdfPCell(new Phrase(descuento, normalFont)));
                        }

                        itemsTable.AddCell(new PdfPCell(new Phrase(item.Subtotal.ToString("C"), normalFont)));
                    }

                    document.Add(itemsTable);
                }

                
                var totalesTable = new PdfPTable(2)
                {
                    WidthPercentage = 50,
                    HorizontalAlignment = Element.ALIGN_RIGHT,
                    SpacingAfter = 20f
                };
                totalesTable.SetWidths(new float[] { 2, 1 });

                totalesTable.AddCell(new PdfPCell(new Phrase("Subtotal:", normalFont)) { Border = Rectangle.NO_BORDER });
                totalesTable.AddCell(new PdfPCell(new Phrase(facturaDto.Subtotal.ToString("C"), normalFont))
                {
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });

                if (facturaDto.Impuestos > 0)
                {
                    totalesTable.AddCell(new PdfPCell(new Phrase("Impuestos:", normalFont)) { Border = Rectangle.NO_BORDER });
                    totalesTable.AddCell(new PdfPCell(new Phrase(facturaDto.Impuestos.ToString("C"), normalFont))
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_RIGHT
                    });
                }

                totalesTable.AddCell(new PdfPCell(new Phrase("Total:", headerFont)) { Border = Rectangle.TOP_BORDER });
                totalesTable.AddCell(new PdfPCell(new Phrase(facturaDto.Total.ToString("C"), headerFont))
                {
                    Border = Rectangle.TOP_BORDER,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                });

                document.Add(totalesTable);

                
                var pagoSection = new Paragraph("INFORMACIÓN DE PAGO", headerFont)
                {
                    SpacingBefore = 10f,
                    SpacingAfter = 10f
                };
                document.Add(pagoSection);

                var pagoInfo = $"Método: {facturaDto.MetodoPago}\n" +
                              $"Monto: {facturaDto.MontoPagado.ToString("C")}\n" +
                              $"Moneda: {facturaDto.Moneda}\n" +
                              $"Cambio: {facturaDto.Cambio.ToString("C")}\n" +
                              $"Fecha: {facturaDto.FechaEmision:dd/MM/yyyy HH:mm}";

                var pagoParagraph = new Paragraph(pagoInfo, normalFont);
                document.Add(pagoParagraph);

                
                var footer = new Paragraph("Gracias por su compra", normalFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 30f
                };
                document.Add(footer);

                document.Close();

                var pdfBytes = memoryStream.ToArray();

               
                GuardarCopiaEnArchivo(facturaDto, pdfBytes);
                GuardarEnBaseDeDatos(facturaDto, pdfBytes);

                return pdfBytes;
            }
        }

        private void GuardarCopiaEnArchivo(FacturaDto facturaDto, byte[] pdfBytes)
        {
            try
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "facturas");

                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var fileName = $"Factura-{facturaDto.NumeroFactura}-{DateTime.Now:yyyyMMddHHmmss}.pdf";
                var filePath = Path.Combine(uploadsPath, fileName);

                File.WriteAllBytes(filePath, pdfBytes);

                _logger.LogInformation($"PDF guardado en: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar copia del PDF en el servidor");
            }
        }

        private void GuardarEnBaseDeDatos(FacturaDto facturaDto, byte[] pdfBytes)
        {
            try
            {
                var factura = _context.Factura.Find(facturaDto.Id);
                if (factura != null)
                {
                    factura.PdfData = pdfBytes;
                    factura.PdfFileName = $"Factura-{facturaDto.NumeroFactura}.pdf";
                    _context.SaveChanges();

                    _logger.LogInformation($"PDF guardado en BD para factura: {facturaDto.NumeroFactura}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar PDF en base de datos");
            }
        }
    }
}