using System;
using NXOpen;
using NXOpen.Annotations;

namespace KnBalloonTool.Services
{
    public static class KnCellWriter
    {
        public static void WriteKn(Tag cellTag, int knNumber)
        {
            if (cellTag == null) throw new ArgumentNullException(nameof(cellTag));

            if (!TryResolveCell(cellTag, out TableSection section, out int row, out int col))
                throw new InvalidOperationException("Hücre koordinatı çözümlenemedi.");

            string current = SafeGetCellText(section, row, col);
            string cleaned = KnFormat.StripKnBlocks(current);
            string knBlock = KnFormat.Build(knNumber);
            string newText = string.IsNullOrEmpty(cleaned) ? knBlock : cleaned + " " + knBlock;

            section.SetCellText(row, col, newText);
            TrySetOwnedAttribute(cellTag);
        }

        public static bool DeleteKn(Tag cellTag)
        {
            if (cellTag == null) return false;
            if (!TryResolveCell(cellTag, out TableSection section, out int row, out int col)) return false;

            string current = SafeGetCellText(section, row, col);
            if (string.IsNullOrEmpty(current)) return false;
            if (!KnFormat.BlockPattern.IsMatch(current)) return false;

            string cleaned = KnFormat.StripKnBlocks(current);
            section.SetCellText(row, col, cleaned);
            return true;
        }

        public static string ReadCellTextCleaned(Tag cellTag)
        {
            if (cellTag == null) return string.Empty;
            if (!TryResolveCell(cellTag, out TableSection section, out int row, out int col)) return string.Empty;
            return KnFormat.StripKnBlocks(SafeGetCellText(section, row, col));
        }

        internal static bool TryResolveCell(Tag cellTag, out TableSection section, out int row, out int col)
        {
            section = null;
            row = -1;
            col = -1;
            try
            {
                section = cellTag.OwningSection;
                if (section == null) return false;
                section.GetCellCoordinates(cellTag, out row, out col);
                return row >= 0 && col >= 0;
            }
            catch (NXException)
            {
                return false;
            }
        }

        internal static string SafeGetCellText(TableSection section, int row, int col)
        {
            try
            {
                return section.GetCellText(row, col) ?? string.Empty;
            }
            catch (NXException)
            {
                return string.Empty;
            }
        }

        private static void TrySetOwnedAttribute(NXObject obj)
        {
            try
            {
                obj.SetUserAttribute(KnDimensionWriter.AttrOwned, -1, "true", Update.Option.Now);
            }
            catch (NXException)
            {
                // Audit-only; ignore failures.
            }
        }
    }
}
