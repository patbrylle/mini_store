using System.Globalization;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace mini_store.Services
{
    public static class ReportExporter
    {
        // If the peso sign prints as an empty square in the PDF, change this to "PHP ".
        private const string PdfPeso = "₱";

        // =====================================================
        // EXCEL
        // =====================================================

        public static byte[] Excel(ReportTable t, string store, string by)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(SheetName(t.Title));
            var cols = t.Columns.Count;
            var generated = DateTime.Now.ToString("MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);

            ws.Cell(1, 1).Value = store;
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#C95F1D");

            ws.Cell(2, 1).Value = t.Title;
            ws.Cell(2, 1).Style.Font.Bold = true;
            ws.Cell(2, 1).Style.Font.FontSize = 16;

            ws.Cell(3, 1).Value = t.Subtitle;
            ws.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#777777");

            ws.Cell(4, 1).Value = $"Generated {generated} by {by}";
            ws.Cell(4, 1).Style.Font.FontSize = 9;
            ws.Cell(4, 1).Style.Font.FontColor = XLColor.FromHtml("#777777");

            var row = 6;
            foreach (var s in t.Stats)
            {
                ws.Cell(row, 1).Value = s.Label;
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 2).Value = s.Value;
                ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                row++;
            }
            if (t.Stats.Count > 0) row++;

            // ---- header ----
            var headerRow = row;
            for (var c = 0; c < cols; c++) ws.Cell(headerRow, c + 1).Value = t.Columns[c].Header;

            var header = ws.Range(headerRow, 1, headerRow, cols);
            header.Style.Font.Bold = true;
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#E4772E");

            // ---- data ----
            foreach (var data in t.Rows)
            {
                row++;
                for (var c = 0; c < cols; c++)
                {
                    var cell = ws.Cell(row, c + 1);
                    switch (data[c])
                    {
                        case null:
                            break;
                        case decimal d:
                            cell.Value = (double)d;
                            cell.Style.NumberFormat.Format = "\"₱\"#,##0.00";
                            break;
                        case int n:
                            cell.Value = (double)n;
                            cell.Style.NumberFormat.Format = "#,##0";
                            break;
                        case DateTime dt:
                            cell.Value = dt;
                            cell.Style.NumberFormat.Format = t.Columns[c].Kind == CellKind.Date
                                ? "mmm d, yyyy"
                                : "mmm d, yyyy h:mm AM/PM";
                            break;
                        default:
                            cell.Value = data[c]!.ToString() ?? "";
                            break;
                    }
                }
            }

            var firstData = headerRow + 1;
            var lastData = row;

            // ---- totals (SUBTOTAL, so Excel's filter buttons update it) ----
            if (t.Totals != null && t.Rows.Count > 0)
            {
                row++;
                for (var c = 0; c < cols; c++)
                {
                    var cell = ws.Cell(row, c + 1);
                    var v = t.Totals[c];

                    if (v is string text) cell.Value = text;
                    else if (v is decimal || v is int)
                    {
                        var letter = ws.Column(c + 1).ColumnLetter();
                        cell.FormulaA1 = $"SUBTOTAL(109,{letter}{firstData}:{letter}{lastData})";
                        cell.Style.NumberFormat.Format = v is decimal ? "\"₱\"#,##0.00" : "#,##0";
                    }
                }

                var totals = ws.Range(row, 1, row, cols);
                totals.Style.Font.Bold = true;
                totals.Style.Fill.BackgroundColor = XLColor.FromHtml("#F4EFE9");
                totals.Style.Border.TopBorder = XLBorderStyleValues.Medium;
            }

            // ---- look and feel ----
            var body = ws.Range(headerRow, 1, lastData, cols);
            body.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            body.Style.Border.OutsideBorderColor = XLColor.FromHtml("#DDDDDD");
            body.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            body.Style.Border.InsideBorderColor = XLColor.FromHtml("#DDDDDD");

            for (var c = 0; c < cols; c++)
                if (t.Columns[c].Right)
                    ws.Range(headerRow, c + 1, row, c + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            if (t.Rows.Count > 0) ws.Range(headerRow, 1, lastData, cols).SetAutoFilter();
            ws.SheetView.FreezeRows(headerRow);

            for (var c = 1; c <= cols; c++)
            {
                var col = ws.Column(c);
                col.AdjustToContents(headerRow, row);
                col.Width = Math.Clamp(col.Width + 2, 11, 48);
            }

            ws.PageSetup.PageOrientation = cols > 6 ? XLPageOrientation.Landscape : XLPageOrientation.Portrait;
            ws.PageSetup.FitToPages(1, 0);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private static string SheetName(string title)
        {
            var clean = new string(title.Where(ch => !"[]:*?/\\".Contains(ch)).ToArray());
            return clean.Length > 31 ? clean[..31] : clean;
        }

        // =====================================================
        // PDF
        // =====================================================

        private static IContainer Align(IContainer c, ReportColumn col) => col.Right ? c.AlignRight() : c.AlignLeft();

        public static byte[] Pdf(ReportTable t, string store, string by)
        {
            var wide = t.Columns.Count > 6;
            var generated = DateTime.Now.ToString("MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);

            return Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(wide ? PageSizes.A4.Landscape() : PageSizes.A4);
                    page.Margin(26);
                    page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor("#222222"));

                    // ---------- header ----------
                    page.Header().PaddingBottom(8).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(store).FontSize(10).SemiBold().FontColor("#c95f1d");
                                c.Item().Text(t.Title).FontSize(17).Bold();
                                c.Item().Text(t.Subtitle).FontSize(8.5f).FontColor("#777777");
                            });

                            row.ConstantItem(190).Column(c =>
                            {
                                c.Item().AlignRight().Text("Generated").FontSize(7).FontColor("#777777");
                                c.Item().AlignRight().Text(generated).FontSize(8.5f);
                                c.Item().AlignRight().Text("By " + by).FontSize(8.5f);
                            });
                        });

                        col.Item().PaddingTop(6).Height(2).Background("#e4772e");
                    });

                    // ---------- content ----------
                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        if (t.Stats.Count > 0)
                        {
                            col.Item().Row(row =>
                            {
                                row.Spacing(8);
                                foreach (var s in t.Stats)
                                {
                                    row.RelativeItem().Border(0.6f).BorderColor("#e5e5e5")
                                        .Background("#faf7f4").Padding(7).Column(c =>
                                        {
                                            c.Item().Text(s.Label.ToUpperInvariant()).FontSize(6.5f).SemiBold().FontColor("#777777");
                                            c.Item().Text(s.Value).FontSize(12).Bold();
                                        });
                                }
                            });
                        }

                        if (t.Rows.Count == 0)
                        {
                            col.Item().PaddingTop(30).AlignCenter()
                                .Text("No records for the selected filters.").FontColor("#777777");
                            return;
                        }

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                foreach (var cd in t.Columns) c.RelativeColumn(cd.Weight);
                            });

                            table.Header(h =>
                            {
                                foreach (var cd in t.Columns)
                                {
                                    h.Cell().Background("#e4772e").PaddingVertical(5).PaddingHorizontal(4)
                                        .Element(e => Align(e, cd))
                                        .Text(cd.Header).FontColor("#ffffff").SemiBold().FontSize(8);
                                }
                            });

                            var n = 0;
                            foreach (var data in t.Rows)
                            {
                                var bg = n++ % 2 == 0 ? "#ffffff" : "#faf7f4";

                                for (var c = 0; c < t.Columns.Count; c++)
                                {
                                    var cd = t.Columns[c];
                                    var text = ReportFormat.Cell(data[c], cd.Kind, PdfPeso);
                                    var color = cd.Kind == CellKind.Status ? ReportFormat.StatusColor(text) : "#222222";

                                    table.Cell().Background(bg).BorderBottom(0.4f).BorderColor("#eeeeee")
                                        .PaddingVertical(4).PaddingHorizontal(4)
                                        .Element(e => Align(e, cd))
                                        .Text(text).FontSize(8).FontColor(color);
                                }
                            }

                            if (t.Totals != null)
                            {
                                for (var c = 0; c < t.Columns.Count; c++)
                                {
                                    var cd = t.Columns[c];
                                    var text = ReportFormat.Cell(t.Totals[c], cd.Kind, PdfPeso);

                                    table.Cell().Background("#f4efe9").BorderTop(1).BorderColor("#c95f1d")
                                        .PaddingVertical(5).PaddingHorizontal(4)
                                        .Element(e => Align(e, cd))
                                        .Text(text).Bold().FontSize(8.5f);
                                }
                            }
                        });
                    });

                    // ---------- footer ----------
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text($"{store} - {t.Title}").FontSize(7.5f).FontColor("#999999");

                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.DefaultTextStyle(s => s.FontSize(7.5f).FontColor("#999999"));
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });
            }).GeneratePdf();
        }
    }
}