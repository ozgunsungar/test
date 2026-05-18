using System;
using System.Collections.Generic;
using NXOpen;
using NXOpen.Annotations;
using KnBalloonTool.Models;

namespace KnBalloonTool.Services
{
    public static class KnRecordCollector
    {
        public static List<KnBalloonRecord> Collect(Part part)
        {
            var records = new List<KnBalloonRecord>();
            if (part == null) return records;

            foreach (Dimension d in part.Annotations.Dimensions)
                AppendDimRecords(d, KnSourceKind.DraftingDimension, records);

            try
            {
                foreach (PmiDimension d in part.PmiManager.PmiDimensions)
                    AppendDimRecords(d, KnSourceKind.PmiDimension, records);
            }
            catch (NXException) { /* PMI not licensed */ }

            foreach (Table t in part.Annotations.Tables)
                AppendTableRecords(t, KnSourceKind.DraftingCell, records);

            try
            {
                foreach (Table t in part.PmiManager.PmiTables)
                    AppendTableRecords(t, KnSourceKind.PmiCell, records);
            }
            catch (NXException) { /* PMI not licensed */ }

            records.Sort((a, b) => string.Compare(a.KnNumber, b.KnNumber, StringComparison.Ordinal));
            return records;
        }

        private static void AppendDimRecords(Dimension dim, KnSourceKind kind, List<KnBalloonRecord> records)
        {
            foreach (string line in KnDimensionWriter.SafeGetAfter(dim))
            {
                if (string.IsNullOrEmpty(line)) continue;
                foreach (System.Text.RegularExpressions.Match m in KnFormat.BareNumberPattern.Matches(line))
                {
                    var rec = BuildDimRecord(dim, kind, m.Groups[0].Value);
                    records.Add(rec);
                }
            }
        }

        private static KnBalloonRecord BuildDimRecord(Dimension dim, KnSourceKind kind, string knText)
        {
            var rec = new KnBalloonRecord
            {
                KnNumber = knText,
                SourceKind = kind,
                SourceLabel = kind == KnSourceKind.PmiDimension ? "PMI Dim" : "Drafting Dim",
                LiveValueText = ReadLiveValue(dim),
                Location = ReadLocation(dim),
                JournalId = SafeJournalId(dim),
            };

            ReadTolerances(dim, rec);
            return rec;
        }

        private static void AppendTableRecords(Table table, KnSourceKind kind, List<KnBalloonRecord> records)
        {
            if (table == null) return;
            TableSection[] sections;
            try { sections = table.Sections; }
            catch (NXException) { return; }
            if (sections == null) return;

            string tableName = SafeTableName(table);

            foreach (TableSection sec in sections)
            {
                int rows = 0, cols = 0;
                try { rows = sec.NumberOfRows; cols = sec.NumberOfColumns; }
                catch (NXException) { continue; }

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        string text = KnCellWriter.SafeGetCellText(sec, r, c);
                        if (string.IsNullOrEmpty(text)) continue;

                        foreach (System.Text.RegularExpressions.Match m in KnFormat.BareNumberPattern.Matches(text))
                        {
                            records.Add(new KnBalloonRecord
                            {
                                KnNumber = m.Groups[0].Value,
                                SourceKind = kind,
                                SourceLabel = kind == KnSourceKind.PmiCell ? "PMI Cell" : "Drafting Cell",
                                LiveValueText = KnFormat.StripKnBlocks(text),
                                Location = tableName + "[" + r + "," + c + "]",
                                JournalId = SafeJournalId(table),
                            });
                        }
                    }
                }
            }
        }

        private static string ReadLiveValue(Dimension dim)
        {
            try
            {
                string[] txt = dim.GetAnnotationText();
                if (txt == null || txt.Length == 0) return string.Empty;
                return KnFormat.StripKnBlocks(txt[0]);
            }
            catch (NXException)
            {
                return string.Empty;
            }
        }

        private static void ReadTolerances(Dimension dim, KnBalloonRecord rec)
        {
            try
            {
                string[] txt = dim.GetAnnotationText();
                if (txt == null) return;
                if (txt.Length > 1) rec.UpperTolerance = txt[1];
                if (txt.Length > 2) rec.LowerTolerance = txt[2];
            }
            catch (NXException)
            {
                // tolerance not available
            }
        }

        private static string ReadLocation(Dimension dim)
        {
            try
            {
                var view = dim.OwningView;
                if (view == null) return string.Empty;
                if (view is NXOpen.Drawings.DraftingView dv && dv.OwningSheet != null)
                    return dv.OwningSheet.Name + " / " + view.Name;
                return view.Name ?? string.Empty;
            }
            catch (NXException)
            {
                return string.Empty;
            }
        }

        private static string SafeJournalId(NXObject obj)
        {
            try { return obj?.JournalIdentifier ?? string.Empty; }
            catch (NXException) { return string.Empty; }
        }

        private static string SafeTableName(Table table)
        {
            try { return table.Name ?? "Table"; }
            catch (NXException) { return "Table"; }
        }
    }
}
