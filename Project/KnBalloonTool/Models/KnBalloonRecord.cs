namespace KnBalloonTool.Models
{
    public sealed class KnBalloonRecord
    {
        public string KnNumber { get; set; }
        public string DimensionType { get; set; }
        public string NominalText { get; set; }
        public double? NominalValue { get; set; }
        public string UpperTolerance { get; set; }
        public string LowerTolerance { get; set; }
        public string Unit { get; set; }
        public string ViewOrSheet { get; set; }
        public string DimensionJournalId { get; set; }
        public string Note { get; set; }
    }
}
