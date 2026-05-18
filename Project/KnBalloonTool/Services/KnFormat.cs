using System.Text.RegularExpressions;

namespace KnBalloonTool.Services
{
    internal static class KnFormat
    {
        public const string Prefix = "KN";
        public const int DigitCount = 3;

        public const string SuffixFormat = "<&71><+> KN{0:D" + "3" + "} <+><&71>";

        public static readonly Regex BlockPattern =
            new Regex(@"<&71><\+>\s*KN(\d{3})\s*<\+><&71>", RegexOptions.Compiled);

        public static readonly Regex BareNumberPattern =
            new Regex(@"KN(\d{3})", RegexOptions.Compiled);

        public static string Build(int n)
        {
            return string.Format(SuffixFormat, n);
        }

        public static string FormatBareKn(int n)
        {
            return Prefix + n.ToString("D" + DigitCount);
        }

        public static string StripKnBlocks(string source)
        {
            if (string.IsNullOrEmpty(source)) return source ?? string.Empty;
            return BlockPattern.Replace(source, string.Empty).TrimEnd();
        }
    }
}
