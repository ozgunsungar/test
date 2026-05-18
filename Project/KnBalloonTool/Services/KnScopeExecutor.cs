using System;
using System.Collections.Generic;
using NXOpen;
using NXOpen.Annotations;

namespace KnBalloonTool.Services
{
    public enum KnScope
    {
        AllDimensions,
        SelectedDimensions,
        SelectedCells,
    }

    public sealed class KnExecutionResult
    {
        public int Processed { get; set; }
        public int Skipped { get; set; }
        public int LastKnNumber { get; set; }
        public List<string> Messages { get; } = new List<string>();
    }

    public static class KnScopeExecutor
    {
        public static KnExecutionResult Write(
            Part workPart,
            KnScope scope,
            int startKn,
            IList<TaggedObject> selectedDims,
            IList<TaggedObject> selectedCells)
        {
            var result = new KnExecutionResult { LastKnNumber = startKn - 1 };
            int current = startKn;

            switch (scope)
            {
                case KnScope.AllDimensions:
                    foreach (Dimension d in EnumerateAllDimensions(workPart))
                        current = TryWriteDim(d, current, result);
                    break;

                case KnScope.SelectedDimensions:
                    if (selectedDims == null) break;
                    foreach (var obj in selectedDims)
                    {
                        if (obj is Dimension d)
                            current = TryWriteDim(d, current, result);
                        else
                            result.Messages.Add("Desteklenmeyen tip atlandı: " + obj?.GetType().Name);
                    }
                    break;

                case KnScope.SelectedCells:
                    if (selectedCells == null) break;
                    foreach (var obj in selectedCells)
                    {
                        if (obj is Tag cellTag)
                            current = TryWriteCell(cellTag, current, result);
                        else
                            result.Messages.Add("Desteklenmeyen hücre tipi atlandı: " + obj?.GetType().Name);
                    }
                    break;
            }

            return result;
        }

        public static KnExecutionResult Delete(
            Part workPart,
            KnScope scope,
            IList<TaggedObject> selectedDims,
            IList<TaggedObject> selectedCells)
        {
            var result = new KnExecutionResult();

            switch (scope)
            {
                case KnScope.AllDimensions:
                    foreach (Dimension d in EnumerateAllDimensions(workPart))
                        TryDeleteDim(d, result);
                    break;

                case KnScope.SelectedDimensions:
                    if (selectedDims == null) break;
                    foreach (var obj in selectedDims)
                    {
                        if (obj is Dimension d) TryDeleteDim(d, result);
                        else result.Skipped++;
                    }
                    break;

                case KnScope.SelectedCells:
                    if (selectedCells == null) break;
                    foreach (var obj in selectedCells)
                    {
                        if (obj is Tag cellTag) TryDeleteCell(cellTag, result);
                        else result.Skipped++;
                    }
                    break;
            }

            return result;
        }

        private static int TryWriteDim(Dimension d, int kn, KnExecutionResult result)
        {
            try
            {
                KnDimensionWriter.WriteKn(d, kn);
                result.Processed++;
                result.LastKnNumber = kn;
                return kn + 1;
            }
            catch (NXException ex)
            {
                result.Skipped++;
                result.Messages.Add("Dim KN" + kn.ToString("D3") + " yazılamadı: " + ex.Message);
                return kn;
            }
        }

        private static int TryWriteCell(Tag cellTag, int kn, KnExecutionResult result)
        {
            try
            {
                KnCellWriter.WriteKn(cellTag, kn);
                result.Processed++;
                result.LastKnNumber = kn;
                return kn + 1;
            }
            catch (Exception ex)
            {
                result.Skipped++;
                result.Messages.Add("Cell KN" + kn.ToString("D3") + " yazılamadı: " + ex.Message);
                return kn;
            }
        }

        private static void TryDeleteDim(Dimension d, KnExecutionResult result)
        {
            try
            {
                if (KnDimensionWriter.DeleteKn(d)) result.Processed++;
                else result.Skipped++;
            }
            catch (NXException ex)
            {
                result.Skipped++;
                result.Messages.Add("Dim silme hatası: " + ex.Message);
            }
        }

        private static void TryDeleteCell(Tag cellTag, KnExecutionResult result)
        {
            try
            {
                if (KnCellWriter.DeleteKn(cellTag)) result.Processed++;
                else result.Skipped++;
            }
            catch (Exception ex)
            {
                result.Skipped++;
                result.Messages.Add("Cell silme hatası: " + ex.Message);
            }
        }

        public static IEnumerable<Dimension> EnumerateAllDimensions(Part part)
        {
            if (part == null) yield break;

            foreach (Dimension d in part.Annotations.Dimensions)
                yield return d;

            IEnumerable<Dimension> pmi = null;
            try { pmi = SafePmiDimensions(part); }
            catch (NXException) { pmi = null; }
            if (pmi == null) yield break;
            foreach (Dimension d in pmi) yield return d;
        }

        private static IEnumerable<Dimension> SafePmiDimensions(Part part)
        {
            var list = new List<Dimension>();
            foreach (PmiDimension d in part.PmiManager.PmiDimensions) list.Add(d);
            return list;
        }
    }
}
