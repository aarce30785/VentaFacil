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
using System.Linq;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using ClosedXML.Excel;

namespace VentaFacil.web.Services.PDF
{
    public class PdfService : IPdfService
    {
        // =====================================================================
        // GenerarExcelNomina
        // =====================================================================
        public byte[] GenerarExcelNomina(NominaDetalleDto data)
        {
            using var wb = new XLWorkbook();

            // ─── Hoja 1: Detalle ─────────────────────────────────────────────
            var ws = wb.Worksheets.Add("Detalle");

            // Título
            ws.Cell(1, 1).Value = "Reporte de Nómina";
            ws.Range(1, 1, 1, 10).Merge();
            ws.Cell(1, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(14)
                .Font.SetFontColor(XLColor.FromHtml("#3659F5"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = $"Periodo: {data.FechaInicio:dd/MM/yyyy} – {data.FechaFinal:dd/MM/yyyy}   |   Estado: {data.Estado}   |   Generado: {data.FechaGeneracion:dd/MM/yyyy HH:mm}";
            ws.Range(2, 1, 2, 10).Merge();
            ws.Cell(2, 1).Style
                .Font.SetItalic(true)
                .Font.SetFontSize(9)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Encabezados de columna
            int headerRow = 4;
            var headers = new List<string>
            {
                "Colaborador", "Correo / ID", "Hrs Base", "Hrs Extras", "Bonos (₡)", "Salario Bruto (₡)"
            };
            foreach (var ded in data.DeduccionesAplicadas)
                headers.Add($"{ded.Nombre} ({ded.Porcentaje:0.##}%)");
            headers.Add("Salario Neto (₡)");
            headers.Add("Observaciones");

            for (int c = 0; c < headers.Count; c++)
            {
                var cell = ws.Cell(headerRow, c + 1);
                cell.Value = headers[c];
                cell.Style
                    .Font.SetBold(true)
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#3659F5"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetWrapText(true);
            }

            // Filas de datos
            int dataRow = headerRow + 1;
            bool alt = false;
            foreach (var item in data.Detalles)
            {
                var bg = alt ? XLColor.FromHtml("#F3F4FF") : XLColor.White;
                alt = !alt;
                int col = 1;

                IXLCell NextCell() { var c = ws.Cell(dataRow, col++); c.Style.Fill.SetBackgroundColor(bg); return c; }

                var cName = NextCell(); cName.Value = item.NombreUsuario; cName.Style.Font.SetBold(true);
                NextCell().Value = item.Identificacion ?? string.Empty;
                NextCell().Value = (double)item.HorasTrabajadas;
                NextCell().Value = (double)item.HorasExtras;
                var cBonos = NextCell(); cBonos.Value = item.Bonificaciones; cBonos.Style.NumberFormat.Format = "₡#,##0.00";
                var cBruto = NextCell(); cBruto.Value = item.SalarioBruto;   cBruto.Style.NumberFormat.Format = "₡#,##0.00";
                foreach (var dd in item.DeduccionesDetalle)
                {
                    var cDed = NextCell(); cDed.Value = dd.Monto; cDed.Style.NumberFormat.Format = "₡#,##0.00";
                }
                var cNeto = NextCell();
                cNeto.Value = item.SalarioNeto;
                cNeto.Style.Font.SetBold(true).Font.SetFontColor(XLColor.FromHtml("#16A34A"));
                cNeto.Style.NumberFormat.Format = "₡#,##0.00";
                NextCell().Value = item.Observaciones ?? string.Empty;

                dataRow++;
            }

            // Fila de totales
            int totRow = dataRow + 1;
            int dedOffset = 6;
            int netoCol   = dedOffset + data.DeduccionesAplicadas.Count + 1;

            ws.Cell(totRow, dedOffset - 1).Value = "TOTALES:";
            ws.Cell(totRow, dedOffset - 1).Style.Font.SetBold(true).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            ws.Cell(totRow, dedOffset).Value = data.TotalBruto;
            ws.Cell(totRow, dedOffset).Style.Font.SetBold(true).NumberFormat.Format = "₡#,##0.00";
            ws.Cell(totRow, netoCol).Value = data.TotalNeto;
            ws.Cell(totRow, netoCol).Style.Font.SetBold(true).Font.SetFontColor(XLColor.FromHtml("#16A34A")).NumberFormat.Format = "₡#,##0.00";

            ws.Columns().AdjustToContents();

            // ─── Hoja 2: Resumen ─────────────────────────────────────────────
            var ws2 = wb.Worksheets.Add("Resumen");

            void AddStr(int row, string label, string value, bool bold = false)
            {
                ws2.Cell(row, 1).Value = label; if (bold) ws2.Cell(row, 1).Style.Font.SetBold(true);
                ws2.Cell(row, 2).Value = value; if (bold) ws2.Cell(row, 2).Style.Font.SetBold(true);
            }
            void AddNum(int row, string label, decimal value, bool bold = false)
            {
                ws2.Cell(row, 1).Value = label; if (bold) ws2.Cell(row, 1).Style.Font.SetBold(true);
                ws2.Cell(row, 2).Value = value; 
                ws2.Cell(row, 2).Style.NumberFormat.Format = "₡#,##0.00";
                if (bold) ws2.Cell(row, 2).Style.Font.SetBold(true);
            }
            void AddInt(int row, string label, int value)
            {
                ws2.Cell(row, 1).Value = label;
                ws2.Cell(row, 2).Value = value;
            }

            ws2.Cell(1, 1).Value = "Resumen de Nómina";
            ws2.Range(1, 1, 1, 2).Merge();
            ws2.Cell(1, 1).Style.Font.SetBold(true).Font.SetFontSize(13).Font.SetFontColor(XLColor.FromHtml("#3659F5"));

            AddInt(3, "Número de Nómina:", data.Id_Nomina);
            AddStr(4, "Estado:",            data.Estado ?? string.Empty);
            AddStr(5, "Periodo Inicio:",    data.FechaInicio.ToString("dd/MM/yyyy"));
            AddStr(6, "Periodo Fin:",       data.FechaFinal.ToString("dd/MM/yyyy"));
            AddStr(7, "Fecha Generación:",  data.FechaGeneracion.ToString("dd/MM/yyyy HH:mm"));
            AddInt(8, "Nº Colaboradores:",  data.Detalles.Count);
            AddNum(10, "Total Bruto:",       data.TotalBruto,       bold: true);
            AddNum(11, "Total Deducciones:", data.TotalDeducciones, bold: true);
            AddNum(12, "Total Neto:",        data.TotalNeto,        bold: true);
            ws2.Cell(12, 2).Style.Font.SetFontColor(XLColor.FromHtml("#16A34A"));


            int sumRow = 14;
            ws2.Cell(sumRow, 1).Value = "Deducción";
            ws2.Cell(sumRow, 2).Value = "Porcentaje";
            ws2.Row(sumRow).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#3659F5")).Font.SetFontColor(XLColor.White);
            sumRow++;
            foreach (var ded in data.DeduccionesAplicadas)
            {
                ws2.Cell(sumRow, 1).Value = ded.Nombre;
                ws2.Cell(sumRow, 2).Value = ded.Porcentaje / 100m;
                ws2.Cell(sumRow, 2).Style.NumberFormat.Format = "0.##%";
                sumRow++;
            }

            ws2.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        public byte[] GenerarReporteNomina(NominaDetalleDto data)

        {
            using (var stream = new MemoryStream())
            {
                var writer = new PdfWriter(stream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf, PageSize.A4.Rotate());

                var fontBold    = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                var colorDark   = new iText.Kernel.Colors.DeviceRgb(30,  30,  30);
                var colorHeader = new iText.Kernel.Colors.DeviceRgb(54,  92, 245);
                var colorAlt    = new iText.Kernel.Colors.DeviceRgb(245, 247, 255);
                var colorWhite  = new iText.Kernel.Colors.DeviceRgb(255, 255, 255);

                // ─── Encabezado ───────────────────────────────────────────────
                document.Add(new Paragraph(new Text("Reporte de Nómina").SetFont(fontBold))
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(20)
                    .SetFontColor(colorHeader));

                document.Add(new Paragraph($"Periodo: {data.FechaInicio:dd/MM/yyyy} – {data.FechaFinal:dd/MM/yyyy}  |  " +
                                           $"Estado: {data.Estado}  |  " +
                                           $"Generado: {data.FechaGeneracion:dd/MM/yyyy HH:mm}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(10)
                    .SetFontColor(colorDark));

                if (!string.IsNullOrWhiteSpace(data.Observaciones))
                {
                    document.Add(new Paragraph($"Observaciones de anulación: {data.Observaciones}")
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontSize(9)
                        .SetFontColor(new iText.Kernel.Colors.DeviceRgb(180, 30, 30)));
                }

                document.Add(new Paragraph("\n"));

                // ─── Leyenda de Deducciones de Ley ────────────────────────────
                if (data.DeduccionesAplicadas.Count > 0)
                {
                    document.Add(new Paragraph(new Text("Deducciones de Ley Aplicadas").SetFont(fontBold))
                        .SetFontSize(11)
                        .SetFontColor(colorHeader));

                    var ledgerTable = new Table(UnitValue.CreatePercentArray(
                        Enumerable.Repeat(1f, data.DeduccionesAplicadas.Count).ToArray()))
                        .SetWidth(UnitValue.CreatePercentValue(60))
                        .SetHorizontalAlignment(HorizontalAlignment.LEFT);

                    foreach (var ded in data.DeduccionesAplicadas)
                        ledgerTable.AddHeaderCell(new Cell()
                            .SetBackgroundColor(colorHeader)
                            .Add(new Paragraph(new Text($"{ded.Nombre}\n{ded.Porcentaje:0.##}%")
                                .SetFont(fontBold).SetFontSize(8))
                                .SetFontColor(colorWhite)
                                .SetTextAlignment(TextAlignment.CENTER)));

                    // Dummy row to close the table nicely
                    foreach (var ded in data.DeduccionesAplicadas)
                        ledgerTable.AddCell(new Cell()
                            .Add(new Paragraph(new Text($"{ded.Porcentaje:0.##}% sobre bruto")
                                .SetFont(fontRegular).SetFontSize(8))
                                .SetTextAlignment(TextAlignment.CENTER)));

                    document.Add(ledgerTable);
                    document.Add(new Paragraph("\n"));
                }

                // ─── Tabla principal por empleado ─────────────────────────────
                // Columnas: Nombre | ID | Horas Base | Extras | Bonos | Bruto | [una col por deducción] | Neto
                int dedCols = data.DeduccionesAplicadas.Count;
                var colWidths = new List<float> { 3f, 2.5f, 1.2f, 1.2f, 1.5f, 1.8f };
                for (int i = 0; i < dedCols; i++) colWidths.Add(1.6f);
                colWidths.Add(1.8f);

                var mainTable = new Table(UnitValue.CreatePercentArray(colWidths.ToArray()))
                    .SetWidth(UnitValue.CreatePercentValue(100));

                // Encabezados
                Cell Hdr(string text) => new Cell()
                    .SetBackgroundColor(colorHeader)
                    .Add(new Paragraph(new Text(text).SetFont(fontBold).SetFontSize(8))
                        .SetFontColor(colorWhite).SetTextAlignment(TextAlignment.CENTER));

                mainTable.AddHeaderCell(Hdr("Colaborador"));
                mainTable.AddHeaderCell(Hdr("Correo / ID"));
                mainTable.AddHeaderCell(Hdr("Hrs Base"));
                mainTable.AddHeaderCell(Hdr("Hrs Extras"));
                mainTable.AddHeaderCell(Hdr("Bonos"));
                mainTable.AddHeaderCell(Hdr("Salario Bruto"));
                foreach (var d in data.DeduccionesAplicadas)
                    mainTable.AddHeaderCell(Hdr($"{d.Nombre}\n({d.Porcentaje:0.##}%)"));
                mainTable.AddHeaderCell(Hdr("Salario Neto"));

                bool alt = false;
                foreach (var item in data.Detalles)
                {
                    var bg = alt ? colorAlt : colorWhite;
                    alt = !alt;

                    Cell Cl(string text, bool bold = false) => new Cell()
                        .SetBackgroundColor(bg)
                        .Add(new Paragraph(new Text(text)
                            .SetFont(bold ? fontBold : fontRegular)
                            .SetFontSize(8))
                            .SetTextAlignment(TextAlignment.CENTER));

                    mainTable.AddCell(Cl(item.NombreUsuario, true));
                    mainTable.AddCell(Cl(item.Identificacion));
                    mainTable.AddCell(Cl(item.HorasTrabajadas.ToString("0.##")));
                    mainTable.AddCell(Cl(item.HorasExtras.ToString("0.##")));
                    mainTable.AddCell(Cl($"₡{item.Bonificaciones:N2}"));
                    mainTable.AddCell(Cl($"₡{item.SalarioBruto:N2}"));
                    foreach (var dd in item.DeduccionesDetalle)
                        mainTable.AddCell(Cl($"₡{dd.Monto:N2}"));
                    mainTable.AddCell(Cl($"₡{item.SalarioNeto:N2}", true));
                }

                document.Add(mainTable);

                // ─── Totales ──────────────────────────────────────────────────
                document.Add(new Paragraph("\n"));

                var totTable = new Table(UnitValue.CreatePercentArray(new float[] { 3f, 1.5f }))
                    .SetWidth(UnitValue.CreatePercentValue(35))
                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT);

                Cell TotLabel(string t) => new Cell().Add(
                    new Paragraph(new Text(t).SetFont(fontBold).SetFontSize(10))
                        .SetTextAlignment(TextAlignment.RIGHT))
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                Cell TotVal(string t, bool green = false) => new Cell().Add(
                    new Paragraph(new Text(t).SetFont(fontBold).SetFontSize(10))
                        .SetFontColor(green
                            ? new iText.Kernel.Colors.DeviceRgb(22, 163, 74)
                            : colorDark)
                        .SetTextAlignment(TextAlignment.RIGHT))
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER);

                totTable.AddCell(TotLabel("Total Bruto:"));
                totTable.AddCell(TotVal($"₡{data.TotalBruto:N2}"));
                totTable.AddCell(TotLabel("Total Deducciones:"));
                totTable.AddCell(TotVal($"₡{data.TotalDeducciones:N2}"));
                totTable.AddCell(TotLabel("Total Neto a Pagar:"));
                totTable.AddCell(TotVal($"₡{data.TotalNeto:N2}", true));

                document.Add(totTable);

                // ─── Detalle Diario por Empleado ─────────────────────────────
                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph(new Text("Detalle de Jornadas Laboradas por Colaborador").SetFont(fontBold))
                    .SetFontSize(12)
                    .SetFontColor(colorHeader));

                foreach (var item in data.Detalles)
                {
                    if (item.DiasLaborados == null || !item.DiasLaborados.Any()) continue;

                    document.Add(new Paragraph(new Text($"  {item.NombreUsuario}  ({item.Identificacion})")
                        .SetFont(fontBold).SetFontSize(9))
                        .SetFontColor(colorDark)
                        .SetMarginTop(8));

                    var diaTable = new Table(UnitValue.CreatePercentArray(new float[] { 2f, 1.4f, 1.4f, 1.4f, 1.4f, 1.2f, 1.2f, 1.5f }))
                        .SetWidth(UnitValue.CreatePercentValue(100));

                    Cell DH(string t) => new Cell()
                        .SetBackgroundColor(new iText.Kernel.Colors.DeviceRgb(230, 234, 255))
                        .Add(new Paragraph(new Text(t).SetFont(fontBold).SetFontSize(7))
                            .SetTextAlignment(TextAlignment.CENTER));
                    Cell DC(string t) => new Cell()
                        .Add(new Paragraph(new Text(t).SetFont(fontRegular).SetFontSize(7))
                            .SetTextAlignment(TextAlignment.CENTER));

                    diaTable.AddHeaderCell(DH("Fecha"));
                    diaTable.AddHeaderCell(DH("Entrada"));
                    diaTable.AddHeaderCell(DH("Inicio Pausa"));
                    diaTable.AddHeaderCell(DH("Fin Pausa"));
                    diaTable.AddHeaderCell(DH("Salida"));
                    diaTable.AddHeaderCell(DH("Hrs Base"));
                    diaTable.AddHeaderCell(DH("Hrs Extra"));
                    diaTable.AddHeaderCell(DH("Estado"));

                    foreach (var dia in item.DiasLaborados.OrderBy(d => d.FechaInicio))
                    {
                        diaTable.AddCell(DC(dia.FechaInicio.ToString("ddd dd/MM/yy")));
                        diaTable.AddCell(DC(dia.FechaInicio.ToString("HH:mm")));
                        diaTable.AddCell(DC(dia.HoraInicioPausa?.ToString("HH:mm") ?? "—"));
                        diaTable.AddCell(DC(dia.HoraFinPausa?.ToString("HH:mm") ?? "—"));
                        diaTable.AddCell(DC(dia.FechaFinal?.ToString("HH:mm") ?? "—"));
                        diaTable.AddCell(DC(dia.HorasTrabajadas.ToString("0.##") + " h"));
                        diaTable.AddCell(DC(dia.HorasExtras.ToString("0.##") + " h"));
                        diaTable.AddCell(DC(dia.EstadoRegistro));
                    }

                    document.Add(diaTable);
                }

                document.Close();
                return stream.ToArray();
            }
        }

        public byte[] GenerarFacturaPdf(FacturaDto facturaDto, object footer = null, bool esCopia = false)
        {
            var cultureCR = System.Globalization.CultureInfo.GetCultureInfo("es-CR");
            var numberFormat = (System.Globalization.NumberFormatInfo)cultureCR.NumberFormat.Clone();
            numberFormat.CurrencySymbol = "¢";

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
                    table.AddCell(new Paragraph(item.PrecioUnitario.ToString("C2", numberFormat)).SetFont(fontRegular));
                    table.AddCell(new Paragraph(item.Subtotal.ToString("C2", numberFormat)).SetFont(fontRegular));
                }

                document.Add(table);

                // Totales
                document.Add(new Paragraph("\n"));
                
                Table totalTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1 })).UseAllAvailableWidth();
                
                totalTable.AddCell(new Cell().Add(new Paragraph("Subtotal:").SetFont(fontBold).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                totalTable.AddCell(new Cell().Add(new Paragraph(facturaDto.Subtotal.ToString("C2", numberFormat)).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                
                totalTable.AddCell(new Cell().Add(new Paragraph("Impuestos:").SetFont(fontBold).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                totalTable.AddCell(new Cell().Add(new Paragraph(facturaDto.Impuestos.ToString("C2", numberFormat)).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));

                totalTable.AddCell(new Cell().Add(new Paragraph("TOTAL:").SetFont(fontBold).SetFontSize(14).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                totalTable.AddCell(new Cell().Add(new Paragraph(facturaDto.Total.ToString("C2", numberFormat)).SetFont(fontBold).SetFontSize(14).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));

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
                    table.AddCell(new Paragraph(item.Tipo_Movimiento ?? "—").SetFont(fontRegular));
                    table.AddCell(new Paragraph(item.Cantidad.ToString()).SetFont(fontRegular));
                    table.AddCell(new Paragraph(item.Nombre_Usuario ?? item.Id_Usuario.ToString()).SetFont(fontRegular));
                }

                document.Add(table);
                document.Close();
                return stream.ToArray();
            }
        }
    }
}