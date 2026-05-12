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

            string snapshot = TryReadDimensionValue(dim);
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

            string snapshot = TryReadDimensionValue(dim);
            if (!string.IsNullOrEmpty(snapshot))
                balloon.SetUserAttribute(AttrDimSnapshot, -1, snapshot, Update.Option.Now);
        }

        public static List<KnBalloonRecord> CollectRecords(Part part)
        {
            var records = new List<KnBalloonRecord>();
            if (part == null) return records;

            foreach (IdSymbol sym in part.Annotations.IdSymbols)
                records.Add(BuildRecord(part, sym, "Drafting"));

            try
            {
                foreach (PmiIdSymbol sym in part.PmiManager.PmiIdSymbols)
                    records.Add(BuildRecord(part, sym, "PMI"));
            }
            catch (NXException)
            {
                // PMI not licensed.
            }

            records.Sort((a, b) => string.Compare(a.KnNumber, b.KnNumber, StringComparison.Ordinal));
            return records;
        }

        private static KnBalloonRecord BuildRecord(Part part, IdSymbol sym, string source)
        {
            string upper = KnCounterService.ReadUpperText(part, sym).Trim();
            return new KnBalloonRecord
            {
                KnNumber = upper,
                DimensionType = source,
                DimensionJournalId = GetAttr(sym, AttrDimTag),
                NominalText = GetAttr(sym, AttrDimSnapshot),
                Note = ResolveNote(sym),
            };
        }

        private static KnBalloonRecord BuildRecord(Part part, PmiIdSymbol sym, string source)
        {
            string upper = KnCounterService.ReadUpperText(part, sym).Trim();
            return new KnBalloonRecord
            {
                KnNumber = upper,
                DimensionType = source,
                DimensionJournalId = GetAttr(sym, AttrDimTag),
                NominalText = GetAttr(sym, AttrDimSnapshot),
                Note = ResolveNote(sym),
            };
        }

        private static string ResolveNote(NXObject sym)
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

        private static string TryReadDimensionValue(NXObject dim)
        {
            try
            {
                if (dim is Annotation ann)
                {
                    string[] txt = ann.GetAnnotationText();
                    return (txt != null && txt.Length > 0) ? txt[0] : string.Empty;
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
