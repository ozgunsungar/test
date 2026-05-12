using System;
using NXOpen;
using NXOpen.Annotations;
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

        private SelectObject _selectionDims;
        private Toggle _toggleAutoKn;
        private IntegerBlock _intKnNumber;
        private Button _buttonApply;

        private SelectObject _selectionBalloon;
        private SelectObject _selectionPairDim;
        private Button _buttonPair;

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
            _dialog.AddDialogShownHandler(new BlockDialog.DialogShown(DialogShownCb));
        }

        public void Show()
        {
            _dialog.Show();
        }

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
            _selectionDims = (SelectObject)_dialog.TopBlock.FindBlock("selectionDims");
            _toggleAutoKn = (Toggle)_dialog.TopBlock.FindBlock("toggleAutoKn");
            _intKnNumber = (IntegerBlock)_dialog.TopBlock.FindBlock("intKnNumber");
            _buttonApply = (Button)_dialog.TopBlock.FindBlock("buttonApply");

            _selectionBalloon = (SelectObject)_dialog.TopBlock.FindBlock("selectionBalloon");
            _selectionPairDim = (SelectObject)_dialog.TopBlock.FindBlock("selectionPairDim");
            _buttonPair = (Button)_dialog.TopBlock.FindBlock("buttonPair");

            _stringFilePath = (StringBlock)_dialog.TopBlock.FindBlock("stringFilePath");
            _buttonExport = (Button)_dialog.TopBlock.FindBlock("buttonExport");

            ConfigureDimensionFilter(_selectionDims);
            ConfigureBalloonFilter(_selectionBalloon);
            ConfigureDimensionFilter(_selectionPairDim);

            _intKnNumber.Value = KnCounterService.GetNextKnNumber(_workPart);
            _stringFilePath.Value = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "kn_balloons.xlsx");
        }

        private void DialogShownCb()
        {
            // Available for late-stage UI tweaks.
        }

        private int UpdateCb(BlockDialog dialog, UIBlock block)
        {
            if (block == _buttonApply) return HandleApply();
            if (block == _buttonPair) return HandlePair();
            if (block == _buttonExport) return HandleExport();
            return 0;
        }

        private int ApplyCb(BlockDialog dialog) => 0;
        private int OkCb(BlockDialog dialog) => 0;

        private int HandleApply()
        {
            TaggedObject[] picks = _selectionDims.GetSelectedObjects();
            if (picks == null || picks.Length == 0)
            {
                Inform("Lütfen en az bir ölçü seçin.");
                return 0;
            }

            int current = _intKnNumber.Value;
            bool autoIncrement = _toggleAutoKn.Value;

            foreach (TaggedObject obj in picks)
            {
                string kn = KnCounterService.Format(current);
                try
                {
                    if (obj is Dimension d)
                        BalloonService.CreateDraftingBalloon(_workPart, d, kn);
                    else if (obj is PmiDimension p)
                        BalloonService.CreatePmiBalloon(_workPart, p, kn);
                    else
                    {
                        Inform("Desteklenmeyen tip seçildi: " + obj.GetType().Name);
                        continue;
                    }

                    if (autoIncrement) current++;
                }
                catch (NXException ex)
                {
                    Inform("Balon oluşturulamadı (" + kn + "): " + ex.Message);
                }
            }

            _intKnNumber.Value = autoIncrement
                ? current
                : KnCounterService.GetNextKnNumber(_workPart);
            _selectionDims.SetSelectedObjects(new TaggedObject[0]);
            return 0;
        }

        private int HandlePair()
        {
            TaggedObject[] balloons = _selectionBalloon.GetSelectedObjects();
            TaggedObject[] dims = _selectionPairDim.GetSelectedObjects();
            if (balloons == null || balloons.Length == 0 || dims == null || dims.Length == 0)
            {
                Inform("Balon ve ölçü seçimi gerekli.");
                return 0;
            }

            string kn = null;
            if (balloons[0] is IdSymbol idSym)
                kn = KnCounterService.ReadUpperText(_workPart, idSym);
            else if (balloons[0] is PmiIdSymbol pmiSym)
                kn = KnCounterService.ReadUpperText(_workPart, pmiSym);

            MappingService.ManualPair((NXObject)balloons[0], (NXObject)dims[0], kn);
            Inform("Eşleştirildi: " + (kn ?? "(KN yok)"));
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
                var records = MappingService.CollectRecords(_workPart);
                ExcelExportService.Export(path, records);
                Inform("Excel yazıldı: " + path);
            }
            catch (Exception ex)
            {
                Inform("Excel hatası: " + ex.Message);
            }
            return 0;
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

        private static void ConfigureBalloonFilter(SelectObject block)
        {
            var mask = new Selection.MaskTriple[]
            {
                new Selection.MaskTriple
                {
                    Type = UFConstants.UF_drafting_entity_type,
                    Subtype = UFConstants.UF_id_symbol_subtype,
                    SolidBodySubtype = 0,
                },
                new Selection.MaskTriple
                {
                    Type = UFConstants.UF_pmi_entity_type,
                    Subtype = UFConstants.UF_pmi_id_symbol_subtype,
                    SolidBodySubtype = 0,
                },
            };
            block.SetSelectionFilter(Selection.SelectionAction.ClearAndEnableSpecific, mask);
        }

        private void Inform(string message)
        {
            var lw = _session.ListingWindow;
            if (!lw.IsOpen) lw.Open();
            lw.WriteLine("[KnBalloon] " + message);
        }
    }
}
