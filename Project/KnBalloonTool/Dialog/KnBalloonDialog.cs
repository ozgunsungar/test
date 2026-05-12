using System;
using System.Collections.Generic;
using System.IO;
using NXOpen;
using NXOpen.Annotations;
using NXOpen.BlockStyler;
using KnBalloonTool.Services;

namespace KnBalloonTool.Dialog
{
    public sealed class KnBalloonDialog : IDisposable
    {
        private readonly Session _session;
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

        private const string DlxFileName = "KnBalloonDialog.dlx";

        public KnBalloonDialog()
        {
            _session = Session.GetSession();
            _workPart = _session.Parts.Work;

            string dlxPath = ResolveDlxPath();
            _dialog = _session.ResourceManager.CreateDialogFromTemplate(dlxPath);

            _dialog.AddApplyHandler(ApplyCb);
            _dialog.AddOkHandler(OkCb);
            _dialog.AddUpdateHandler(UpdateCb);
            _dialog.AddInitializeHandler(InitializeCb);
            _dialog.AddDialogShownHandler(DialogShownCb);
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

        private static string ResolveDlxPath()
        {
            string dllDir = Path.GetDirectoryName(typeof(KnBalloonDialog).Assembly.Location);
            string candidate = Path.Combine(dllDir, DlxFileName);
            if (File.Exists(candidate)) return candidate;

            string userDir = Environment.GetEnvironmentVariable("UGII_USER_DIR");
            if (!string.IsNullOrEmpty(userDir))
            {
                string atStartup = Path.Combine(userDir, "startup", DlxFileName);
                if (File.Exists(atStartup)) return atStartup;
            }
            throw new FileNotFoundException("KnBalloonDialog.dlx not found", DlxFileName);
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
            _stringFilePath.Value = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "kn_balloons.xlsx");
        }

        private void DialogShownCb()
        {
            // No-op placeholder; available for future focus / state setup.
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
            var picks = _selectionDims.GetSelectedObjects();
            if (picks == null || picks.Length == 0)
            {
                Inform("Lütfen en az bir ölçü seçin.");
                return 0;
            }

            int current = _intKnNumber.Value;
            bool autoIncrement = _toggleAutoKn.Value;

            foreach (var obj in picks)
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

            _intKnNumber.Value = autoIncrement ? current : KnCounterService.GetNextKnNumber(_workPart);
            _selectionDims.SetSelectedObjects(Array.Empty<TaggedObject>());
            return 0;
        }

        private int HandlePair()
        {
            var balloons = _selectionBalloon.GetSelectedObjects();
            var dims = _selectionPairDim.GetSelectedObjects();
            if (balloons == null || balloons.Length == 0 || dims == null || dims.Length == 0)
            {
                Inform("Balon ve ölçü seçimi gerekli.");
                return 0;
            }

            string kn = null;
            if (balloons[0] is IdSymbol idSym)
            {
                try { kn = idSym.GetIdSymbolPreferences().UpperText; } catch { }
            }
            else if (balloons[0] is PmiIdSymbol pmiSym)
            {
                try { kn = pmiSym.GetIdSymbolPreferences().UpperText; } catch { }
            }

            MappingService.ManualPair(balloons[0], dims[0], kn);
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
                    Type = 12, // UF_drafting_entity_type
                    Subtype = 4, // Dimensions
                    SolidBodySubtype = 0,
                },
                new Selection.MaskTriple
                {
                    Type = 121, // UF_pmi_entity_type
                    Subtype = 0,
                    SolidBodySubtype = 0,
                },
            };
            block.SetSelectionFilter(SelectObject.SelectionAction.ClearAndEnableSpecific, mask);
        }

        private static void ConfigureBalloonFilter(SelectObject block)
        {
            var mask = new Selection.MaskTriple[]
            {
                new Selection.MaskTriple
                {
                    Type = 12,
                    Subtype = 9, // Id symbols
                    SolidBodySubtype = 0,
                },
            };
            block.SetSelectionFilter(SelectObject.SelectionAction.ClearAndEnableSpecific, mask);
        }

        private void Inform(string message)
        {
            var lw = _session.ListingWindow;
            if (!lw.IsOpen) lw.Open();
            lw.WriteLine("[KnBalloon] " + message);
        }
    }
}
