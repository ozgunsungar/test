using System;
using System.Collections.Generic;
using NXOpen;
using NXOpen.BlockStyler;
using NXOpen.UF;
using KnBalloonTool.Services;

namespace KnBalloonTool.Dialog
{
    public sealed class KnBalloonDialog : IDisposable
    {
        private const string DlxFileName = "KnBalloonDialog.dlx";

        private readonly Session _session;
        private readonly UI _ui;
        private readonly Part _workPart;
        private BlockDialog _dialog;

        private Enumeration _radioScope;
        private SelectObject _selectionDims;
        private SelectObject _selectionCells;
        private IntegerBlock _intStartKn;
        private Button _buttonWrite;
        private Button _buttonDelete;
        private StringBlock _stringFilePath;
        private Button _buttonExport;

        public KnBalloonDialog()
        {
            _session = Session.GetSession();
            _ui = UI.GetUI();
            _workPart = _session.Parts.Work;

            _dialog = _ui.CreateDialog(DlxFileName);

            _dialog.AddApplyHandler(new BlockDialog.Apply(ApplyCb));
            _dialog.AddOkHandler(new BlockDialog.Ok(OkCb));
            _dialog.AddUpdateHandler(new BlockDialog.Update(UpdateCb));
            _dialog.AddInitializeHandler(new BlockDialog.Initialize(InitializeCb));
        }

        public void Show() => _dialog.Show();

        public void Dispose()
        {
            if (_dialog != null)
            {
                _dialog.Dispose();
                _dialog = null;
            }
        }

        private void InitializeCb()
        {
            _radioScope = (Enumeration)_dialog.TopBlock.FindBlock("radioScope");
            _selectionDims = (SelectObject)_dialog.TopBlock.FindBlock("selectionDims");
            _selectionCells = (SelectObject)_dialog.TopBlock.FindBlock("selectionCells");
            _intStartKn = (IntegerBlock)_dialog.TopBlock.FindBlock("intStartKn");
            _buttonWrite = (Button)_dialog.TopBlock.FindBlock("buttonWrite");
            _buttonDelete = (Button)_dialog.TopBlock.FindBlock("buttonDelete");
            _stringFilePath = (StringBlock)_dialog.TopBlock.FindBlock("stringFilePath");
            _buttonExport = (Button)_dialog.TopBlock.FindBlock("buttonExport");

            ConfigureDimensionFilter(_selectionDims);
            ConfigureCellFilter(_selectionCells);

            _intStartKn.Value = KnCounterService.GetNextKnNumber(_workPart);
            _stringFilePath.Value = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "kn_listesi.xlsx");

            ApplyScopeEnableState();
        }

        private int UpdateCb(BlockDialog dialog, UIBlock block)
        {
            if (block == _radioScope)
            {
                ApplyScopeEnableState();
                return 0;
            }
            if (block == _buttonWrite) return HandleWrite();
            if (block == _buttonDelete) return HandleDelete();
            if (block == _buttonExport) return HandleExport();
            return 0;
        }

        private int ApplyCb(BlockDialog dialog) => 0;
        private int OkCb(BlockDialog dialog) => 0;

        private KnScope CurrentScope()
        {
            switch (_radioScope.GetProperties().GetEnum("Value"))
            {
                case 0: return KnScope.AllDimensions;
                case 1: return KnScope.SelectedDimensions;
                case 2: return KnScope.SelectedCells;
                default: return KnScope.SelectedDimensions;
            }
        }

        private void ApplyScopeEnableState()
        {
            KnScope scope = CurrentScope();
            _selectionDims.Enable = scope == KnScope.SelectedDimensions;
            _selectionCells.Enable = scope == KnScope.SelectedCells;
        }

        private int HandleWrite()
        {
            KnScope scope = CurrentScope();
            IList<TaggedObject> dims = ToList(_selectionDims.GetSelectedObjects());
            IList<TaggedObject> cells = ToList(_selectionCells.GetSelectedObjects());

            if (scope == KnScope.SelectedDimensions && dims.Count == 0)
            {
                Inform("En az bir ölçü seçin.");
                return 0;
            }
            if (scope == KnScope.SelectedCells && cells.Count == 0)
            {
                Inform("En az bir hücre seçin.");
                return 0;
            }

            int startKn = _intStartKn.Value;
            KnExecutionResult result = KnScopeExecutor.Write(_workPart, scope, startKn, dims, cells);

            Inform("KN Yaz tamam: " + result.Processed + " yazıldı, " + result.Skipped + " atlandı.");
            foreach (string msg in result.Messages) Inform(msg);

            _intStartKn.Value = KnCounterService.GetNextKnNumber(_workPart);
            _selectionDims.SetSelectedObjects(new TaggedObject[0]);
            _selectionCells.SetSelectedObjects(new TaggedObject[0]);
            return 0;
        }

        private int HandleDelete()
        {
            KnScope scope = CurrentScope();

            if (scope == KnScope.AllDimensions && !ConfirmAllDelete())
            {
                Inform("Silme iptal edildi.");
                return 0;
            }

            IList<TaggedObject> dims = ToList(_selectionDims.GetSelectedObjects());
            IList<TaggedObject> cells = ToList(_selectionCells.GetSelectedObjects());

            if (scope == KnScope.SelectedDimensions && dims.Count == 0)
            {
                Inform("Silinecek ölçü seçin.");
                return 0;
            }
            if (scope == KnScope.SelectedCells && cells.Count == 0)
            {
                Inform("Silinecek hücre seçin.");
                return 0;
            }

            KnExecutionResult result = KnScopeExecutor.Delete(_workPart, scope, dims, cells);
            Inform("KN Sil tamam: " + result.Processed + " temizlendi, " + result.Skipped + " atlandı.");
            foreach (string msg in result.Messages) Inform(msg);

            _intStartKn.Value = KnCounterService.GetNextKnNumber(_workPart);
            _selectionDims.SetSelectedObjects(new TaggedObject[0]);
            _selectionCells.SetSelectedObjects(new TaggedObject[0]);
            return 0;
        }

        private int HandleExport()
        {
            string path = _stringFilePath.Value;
            if (string.IsNullOrWhiteSpace(path))
            {
                Inform("Çıktı yolu boş olamaz.");
                return 0;
            }

            try
            {
                var records = KnRecordCollector.Collect(_workPart);
                ExcelExportService.Export(path, records);
                Inform("Excel yazıldı (" + records.Count + " kayıt): " + path);
            }
            catch (Exception ex)
            {
                Inform("Excel hatası: " + ex.Message);
            }
            return 0;
        }

        private bool ConfirmAllDelete()
        {
            int choice = _ui.NXMessageBox.Show(
                "KN Sil — Onay",
                NXMessageBox.DialogType.Question,
                "Part'taki TÜM ölçü KN'leri silinecek. Devam edilsin mi?");
            // NXMessageBox: 1=Yes, 2=No, 3=Cancel (Question dialog)
            return choice == 1;
        }

        private static void ConfigureDimensionFilter(SelectObject block)
        {
            var mask = new Selection.MaskTriple[]
            {
                new Selection.MaskTriple
                {
                    Type = UFConstants.UF_drafting_entity_type,
                    Subtype = UFConstants.UF_dimension_subtype,
                    SolidBodySubtype = 0,
                },
                new Selection.MaskTriple
                {
                    Type = UFConstants.UF_pmi_entity_type,
                    Subtype = UFConstants.UF_pmi_dimension_subtype,
                    SolidBodySubtype = 0,
                },
            };
            block.SetSelectionFilter(Selection.SelectionAction.ClearAndEnableSpecific, mask);
        }

        private static void ConfigureCellFilter(SelectObject block)
        {
            var mask = new Selection.MaskTriple[]
            {
                new Selection.MaskTriple
                {
                    Type = UFConstants.UF_drafting_entity_type,
                    Subtype = UFConstants.UF_tabular_note_subtype,
                    SolidBodySubtype = 0,
                },
                new Selection.MaskTriple
                {
                    Type = UFConstants.UF_pmi_entity_type,
                    Subtype = UFConstants.UF_pmi_tabular_note_subtype,
                    SolidBodySubtype = 0,
                },
            };
            block.SetSelectionFilter(Selection.SelectionAction.ClearAndEnableSpecific, mask);
        }

        private static IList<TaggedObject> ToList(TaggedObject[] arr)
        {
            return arr == null ? new List<TaggedObject>() : new List<TaggedObject>(arr);
        }

        private void Inform(string message)
        {
            var lw = _session.ListingWindow;
            if (!lw.IsOpen) lw.Open();
            lw.WriteLine("[KnBalloon] " + message);
        }
    }
}
