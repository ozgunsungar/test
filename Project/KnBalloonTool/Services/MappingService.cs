using System;
using System.Collections.Generic;
using NXOpen;
using NXOpen.Annotations;
using KnBalloonTool.Models;

namespace KnBalloonTool.Services
{
    public static class MappingService
    {
        public const string AttrOwned = "KN_TOOL_OWNED";
        public const string AttrNumber = "KN_NUMBER";
        public const string AttrDimTag = "KN_DIM_TAG";
        public const string AttrDimSnapshot = "KN_DIM_VALUE";

        public static void MarkBalloonAsToolOwned(NXObject balloon, string knNumber, NXObject dim)
        {
            if (balloon == null) return;
            balloon.SetUserAttribute(AttrOwned, -1, "true", Update.Option.Now);
            balloon.SetUserAttribute(AttrNumber, -1, knNumber ?? string.Empty, Update.Option.Now);
            balloon.SetUserAttribute(AttrDimTag, -1, dim?.JournalIdentifier ?? string.Empty, Update.Option.Now);

            string snapshot = TryReadDimensionText(dim, 0);
            if (!string.IsNullOrEmpty(snapshot))
                balloon.SetUserAttribute(AttrDimSnapshot, -1, snapshot, Update.Option.Now);
        }

        public static void ManualPair(NXObject balloon, NXObject dim, string knNumber)
        {
            if (balloon == null || dim == null) return;
            balloon.SetUserAttribute(AttrOwned, -1, "manual", Update.Option.Now);
            if (!string.IsNullOrEmpty(knNumber))
                balloon.SetUserAttribute(AttrNumber, -1, knNumber, Update.Option.Now);
            balloon.SetUserAttribute(AttrDimTag, -1, dim.JournalIdentifier, Update.Option.Now);

            string snapshot = TryReadDimensionText(dim, 0);
            if (!string.IsNullOrEmpty(snapshot))
                balloon.SetUserAttribute(AttrDimSnapshot, -1, snapshot, Update.Option.Now);
        }

        public static List<KnBalloonRecord> CollectRecords(Part part)
        {
            var records = new List<KnBalloonRecord>();
            if (part == null) return records;

            Dictionary<string, Annotation> dimsByJournal = BuildDimensionLookup(part);

            foreach (IdSymbol sym in part.Annotations.IdSymbols)
                records.Add(BuildRecord(part, sym, "Drafting", dimsByJournal));

            try
            {
                foreach (PmiIdSymbol sym in part.PmiManager.PmiIdSymbols)
                    records.Add(BuildRecord(part, sym, "PMI", dimsByJournal));
            }
            catch (NXException)
            {
                // PMI not licensed.
            }

            records.Sort((a, b) => string.Compare(a.KnNumber, b.KnNumber, StringComparison.Ordinal));
            return records;
        }

        private static Dictionary<string, Annotation> BuildDimensionLookup(Part part)
        {
            var dict = new Dictionary<string, Annotation>(StringComparer.Ordinal);

            foreach (Dimension d in part.Annotations.Dimensions)
            {
                string id = SafeJournalId(d);
                if (!string.IsNullOrEmpty(id)) dict[id] = d;
            }

            try
            {
                foreach (PmiDimension d in part.PmiManager.PmiDimensions)
                {
                    string id = SafeJournalId(d);
                    if (!string.IsNullOrEmpty(id)) dict[id] = d;
                }
            }
            catch (NXException)
            {
                // ignore
            }
            return dict;
        }

        private static KnBalloonRecord BuildRecord(Part part, IdSymbol sym, string source,
            IDictionary<string, Annotation> dimsByJournal)
        {
            return BuildRecordCore(part, sym, source, dimsByJournal, KnCounterService.ReadUpperText(part, sym));
        }

        private static KnBalloonRecord BuildRecord(Part part, PmiIdSymbol sym, string source,
            IDictionary<string, Annotation> dimsByJournal)
        {
            return BuildRecordCore(part, sym, source, dimsByJournal, KnCounterService.ReadUpperText(part, sym));
        }

        private static KnBalloonRecord BuildRecordCore(Part part, NXObject sym, string source,
            IDictionary<string, Annotation> dimsByJournal, string knText)
        {
            var record = new KnBalloonRecord
            {
                KnNumber = (knText ?? string.Empty).Trim(),
                DimensionType = source,
                DimensionJournalId = GetAttr(sym, AttrDimTag),
                SnapshotText = GetAttr(sym, AttrDimSnapshot),
                Note = ResolveBaseNote(sym),
            };

            if (!string.IsNullOrEmpty(record.DimensionJournalId)
                && dimsByJournal.TryGetValue(record.DimensionJournalId, out Annotation liveDim))
            {
                string[] lines = SafeAnnotationText(liveDim);
                record.LiveValueText = (lines != null && lines.Length > 0) ? lines[0] : string.Empty;
                record.UpperTolerance = (lines != null && lines.Length > 1) ? lines[1] : string.Empty;
                record.LowerTolerance = (lines != null && lines.Length > 2) ? lines[2] : string.Empty;
                record.OwningViewOrSheet = SafeViewName(liveDim);
            }
            else if (!string.IsNullOrEmpty(record.DimensionJournalId))
            {
                if (string.IsNullOrEmpty(record.Note))
                    record.Note = "ÖLÇÜ BULUNAMADI (silinmiş ya da journal id değişmiş)";
            }

            return record;
        }

        private static string ResolveBaseNote(NXObject sym)
        {
            string owned = GetAttr(sym, AttrOwned);
            if (string.IsNullOrEmpty(owned)) return "MANUEL — eşleştirilmedi";
            if (owned == "manual") return "Manuel eşleştirildi";
            return string.Empty;
        }

        private static string GetAttr(NXObject obj, string title)
        {
            try { return obj.GetUserAttributeAsString(title, NXObject.AttributeType.String, -1); }
            catch { return string.Empty; }
        }

        private static string SafeJournalId(NXObject obj)
        {
            try { return obj.JournalIdentifier ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static string[] SafeAnnotationText(Annotation ann)
        {
            try { return ann.GetAnnotationText(); }
            catch { return null; }
        }

        private static string SafeViewName(Annotation ann)
        {
            try
            {
                var view = ann.OwningView;
                if (view == null) return string.Empty;
                if (view is NXOpen.Drawings.DraftingView dv && dv.OwningSheet != null)
                    return dv.OwningSheet.Name + " / " + view.Name;
                return view.Name;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string TryReadDimensionText(NXObject dim, int lineIndex)
        {
            try
            {
                if (dim is Annotation ann)
                {
                    string[] txt = ann.GetAnnotationText();
                    return (txt != null && txt.Length > lineIndex) ? txt[lineIndex] : string.Empty;
                }
            }
            catch
            {
                // ignore
            }
            return string.Empty;
        }
    }
}
