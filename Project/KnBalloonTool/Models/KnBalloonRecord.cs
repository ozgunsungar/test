namespace KnBalloonTool.Models
{
    public enum KnSourceKind
    {
        DraftingDimension,
        PmiDimension,
        DraftingCell,
        PmiCell,
    }

    public sealed class KnBalloonRecord
    {
        public string KnNumber { get; set; }
        public KnSourceKind SourceKind { get; set; }
        public string SourceLabel { get; set; }
        public string LiveValueText { get; set; }
        public string UpperTolerance { get; set; }
        public string LowerTolerance { get; set; }
        public string Location { get; set; }
        public string JournalId { get; set; }
        public string Note { get; set; }
    }
}
