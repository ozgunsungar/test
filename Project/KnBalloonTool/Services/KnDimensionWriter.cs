using System;
using NXOpen;
using NXOpen.Annotations;

namespace KnBalloonTool.Services
{
    public static class KnDimensionWriter
    {
        public const string AttrOwned = "KN_TOOL_OWNED";

        public static void WriteKn(Dimension dim, int knNumber)
        {
            if (dim == null) throw new ArgumentNullException(nameof(dim));

            string cleaned = ReadAfterTextCleaned(dim);
            string knBlock = KnFormat.Build(knNumber);
            string combined = string.IsNullOrEmpty(cleaned) ? knBlock : cleaned + " " + knBlock;

            dim.SetAppendedText(AppendedTextType.After, new[] { combined });
            TrySetOwnedAttribute(dim);
        }

        public static bool DeleteKn(Dimension dim)
        {
            if (dim == null) return false;

            string[] after = SafeGetAfter(dim);
            if (after == null || after.Length == 0) return false;

            bool changed = false;
            for (int i = 0; i < after.Length; i++)
            {
                string line = after[i] ?? string.Empty;
                if (!KnFormat.BlockPattern.IsMatch(line)) continue;
                after[i] = KnFormat.StripKnBlocks(line);
                changed = true;
            }

            if (!changed) return false;

            bool allEmpty = true;
            foreach (var l in after)
                if (!string.IsNullOrEmpty(l)) { allEmpty = false; break; }

            dim.SetAppendedText(AppendedTextType.After, allEmpty ? new string[0] : after);
            return true;
        }

        public static string ReadAfterTextCleaned(Dimension dim)
        {
            string[] after = SafeGetAfter(dim);
            if (after == null || after.Length == 0) return string.Empty;

            string joined = string.Join(" ", after);
            return KnFormat.StripKnBlocks(joined);
        }

        public static string[] SafeGetAfter(Dimension dim)
        {
            try
            {
                return dim.GetAppendedText(AppendedTextType.After) ?? new string[0];
            }
            catch (NXException)
            {
                return new string[0];
            }
        }

        private static void TrySetOwnedAttribute(NXObject obj)
        {
            try
            {
                obj.SetUserAttribute(AttrOwned, -1, "true", Update.Option.Now);
            }
            catch (NXException)
            {
                // Audit-only; ignore failures.
            }
        }
    }
}
