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
                Probe(part, sym, ref max);

            try
            {
                foreach (PmiIdSymbol sym in part.PmiManager.PmiIdSymbols)
                    Probe(part, sym, ref max);
            }
            catch (NXException)
            {
                // PMI not licensed or no PMI manager — ignore.
            }

            return max + 1;
        }

        public static string Format(int n)
        {
            return KnPrefix + n.ToString("D" + DigitCount);
        }

        private static void Probe(Part part, IdSymbol sym, ref int max)
        {
            string upper = ReadUpperText(part, sym);
            if (string.IsNullOrEmpty(upper)) return;
            var m = KnPattern.Match(upper.Trim());
            if (!m.Success) return;
            if (int.TryParse(m.Groups[1].Value, out int n) && n > max) max = n;
        }

        private static void Probe(Part part, PmiIdSymbol sym, ref int max)
        {
            string upper = ReadUpperText(part, sym);
            if (string.IsNullOrEmpty(upper)) return;
            var m = KnPattern.Match(upper.Trim());
            if (!m.Success) return;
            if (int.TryParse(m.Groups[1].Value, out int n) && n > max) max = n;
        }

        internal static string ReadUpperText(Part part, IdSymbol sym)
        {
            IdSymbolBuilder b = null;
            try
            {
                b = part.Annotations.IdSymbols.CreateIdSymbolBuilder(sym);
                return b.UpperText ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                if (b != null) b.Destroy();
            }
        }

        internal static string ReadUpperText(Part part, PmiIdSymbol sym)
        {
            PmiIdSymbolBuilder b = null;
            try
            {
                b = part.PmiManager.PmiIdSymbols.CreatePmiIdSymbolBuilder(sym);
                return b.UpperText ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                if (b != null) b.Destroy();
            }
        }
    }
}
