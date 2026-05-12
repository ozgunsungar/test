namespace KnBalloonTool.Models
{
    public sealed class KnBalloonRecord
    {
        public string KnNumber { get; set; }
        public string DimensionType { get; set; }
        public string LiveValueText { get; set; }
        public string SnapshotText { get; set; }
        public string UpperTolerance { get; set; }
        public string LowerTolerance { get; set; }
        public string OwningViewOrSheet { get; set; }
        public string DimensionJournalId { get; set; }
        public string Note { get; set; }
    }
}
