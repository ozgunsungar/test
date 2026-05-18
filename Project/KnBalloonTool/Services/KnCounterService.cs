using System;
using NXOpen;
using NXOpen.Annotations;

namespace KnBalloonTool.Services
{
    public static class KnCounterService
    {
        public static int GetNextKnNumber(Part part)
        {
            if (part == null) return 1;

            int max = 0;

            foreach (Dimension d in part.Annotations.Dimensions)
                ProbeDim(d, ref max);

            TryPmiDimensions(part, ref max);

            foreach (Table t in part.Annotations.Tables)
                ProbeTable(t, ref max);

            TryPmiTables(part, ref max);

            return max + 1;
        }

        public static string FormatBare(int n)
        {
            return KnFormat.FormatBareKn(n);
        }

        private static void TryPmiDimensions(Part part, ref int max)
        {
            try
            {
                foreach (PmiDimension d in part.PmiManager.PmiDimensions)
                    ProbeDim(d, ref max);
            }
            catch (NXException)
            {
                // PMI not licensed or unavailable.
            }
        }

        private static void TryPmiTables(Part part, ref int max)
        {
            try
            {
                foreach (Table t in part.PmiManager.PmiTables)
                    ProbeTable(t, ref max);
            }
            catch (NXException)
            {
                // PMI not licensed or no tables.
            }
        }

        private static void ProbeDim(Dimension d, ref int max)
        {
            foreach (string line in KnDimensionWriter.SafeGetAfter(d))
                ProbeText(line, ref max);
        }

        private static void ProbeTable(Table table, ref int max)
        {
            if (table == null) return;
            TableSection[] sections;
            try { sections = table.Sections; }
            catch (NXException) { return; }
            if (sections == null) return;

            foreach (TableSection sec in sections)
                ProbeSection(sec, ref max);
        }

        private static void ProbeSection(TableSection section, ref int max)
        {
            if (section == null) return;

            int rows = SafeRows(section);
            int cols = SafeCols(section);
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    ProbeText(KnCellWriter.SafeGetCellText(section, r, c), ref max);
                }
            }
        }

        private static int SafeRows(TableSection section)
        {
            try { return section.NumberOfRows; } catch (NXException) { return 0; }
        }

        private static int SafeCols(TableSection section)
        {
            try { return section.NumberOfColumns; } catch (NXException) { return 0; }
        }

        private static void ProbeText(string s, ref int max)
        {
            if (string.IsNullOrEmpty(s)) return;
            foreach (System.Text.RegularExpressions.Match m in KnFormat.BareNumberPattern.Matches(s))
            {
                if (int.TryParse(m.Groups[1].Value, out int n) && n > max) max = n;
            }
        }
    }
}
