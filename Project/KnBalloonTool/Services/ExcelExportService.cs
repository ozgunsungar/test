using System;
using System.Collections.Generic;
using ClosedXML.Excel;
using KnBalloonTool.Models;

namespace KnBalloonTool.Services
{
    public static class ExcelExportService
    {
        public static void Export(string path, IEnumerable<KnBalloonRecord> records)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path required");
            if (records == null) throw new ArgumentNullException(nameof(records));

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Balon Listesi");

                ws.Cell(1, 1).Value = "KN No";
                ws.Cell(1, 2).Value = "Tip";
                ws.Cell(1, 3).Value = "Anlık Değer";
                ws.Cell(1, 4).Value = "Üst Tol.";
                ws.Cell(1, 5).Value = "Alt Tol.";
                ws.Cell(1, 6).Value = "View / Sheet";
                ws.Cell(1, 7).Value = "Snapshot";
                ws.Cell(1, 8).Value = "Dim Journal ID";
                ws.Cell(1, 9).Value = "Not";

                var header = ws.Range(1, 1, 1, 9);
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.LightGray;

                int row = 2;
                foreach (var r in records)
                {
                    ws.Cell(row, 1).Value = r.KnNumber ?? string.Empty;
                    ws.Cell(row, 2).Value = r.DimensionType ?? string.Empty;
                    ws.Cell(row, 3).Value = r.LiveValueText ?? string.Empty;
                    ws.Cell(row, 4).Value = r.UpperTolerance ?? string.Empty;
                    ws.Cell(row, 5).Value = r.LowerTolerance ?? string.Empty;
                    ws.Cell(row, 6).Value = r.OwningViewOrSheet ?? string.Empty;
                    ws.Cell(row, 7).Value = r.SnapshotText ?? string.Empty;
                    ws.Cell(row, 8).Value = r.DimensionJournalId ?? string.Empty;
                    ws.Cell(row, 9).Value = r.Note ?? string.Empty;
                    row++;
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(path);
            }
        }
    }
}
