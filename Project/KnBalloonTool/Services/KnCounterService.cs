using System;
using System.Text.RegularExpressions;
using NXOpen;
using NXOpen.Annotations;

namespace KnBalloonTool.Services
{
    public static class KnCounterService
    {
        private static readonly Regex KnPattern =
            new Regex(@"^KN(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public const string KnPrefix = "KN";
        public const int DigitCount = 3;

        public static int GetNextKnNumber(Part part)
        {
            if (part == null) return 1;
            int max = 0;

            foreach (IdSymbol sym in part.Annotations.IdSymbols)
                ProbeIdSymbol(sym, ref max);

            try
            {
                foreach (NXOpen.Annotations.PmiIdSymbol sym in part.PmiManager.PmiIdSymbols)
                    ProbeIdSymbol(sym, ref max);
            }
            catch (NXException)
            {
                // PMI not licensed or empty — ignore.
            }

            return max + 1;
        }

        public static string Format(int n)
        {
            return KnPrefix + n.ToString("D" + DigitCount);
        }

        private static void ProbeIdSymbol(IdSymbol sym, ref int max)
        {
            string upper = SafeUpperText(sym);
            if (string.IsNullOrEmpty(upper)) return;
            var m = KnPattern.Match(upper.Trim());
            if (!m.Success) return;
            if (int.TryParse(m.Groups[1].Value, out int n) && n > max) max = n;
        }

        private static void ProbeIdSymbol(NXOpen.Annotations.PmiIdSymbol sym, ref int max)
        {
            string upper = SafeUpperText(sym);
            if (string.IsNullOrEmpty(upper)) return;
            var m = KnPattern.Match(upper.Trim());
            if (!m.Success) return;
            if (int.TryParse(m.Groups[1].Value, out int n) && n > max) max = n;
        }

        private static string SafeUpperText(IdSymbol sym)
        {
            try
            {
                var style = sym.GetIdSymbolPreferences();
                return style.UpperText ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string SafeUpperText(NXOpen.Annotations.PmiIdSymbol sym)
        {
            try
            {
                var style = sym.GetIdSymbolPreferences();
                return style.UpperText ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
