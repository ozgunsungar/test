# UML — Sınıf Diyagramı

KN Balloon Tool'un yazılım mimarisi. Mermaid (GitHub render eder).

```mermaid
classDiagram
    direction LR

    class Program {
        <<entry point>>
        +Main(args) int
        +ufusr(param, retCode, len)
        +GetUnloadOption(arg) int
    }

    class KnBalloonDialog {
        -Session _session
        -UI _ui
        -Part _workPart
        -BlockDialog _dialog
        -SelectObject _selectionDims
        -IntegerBlock _intKnNumber
        -Toggle _toggleAutoKn
        -SelectObject _selectionBalloon
        -SelectObject _selectionPairDim
        -StringBlock _stringFilePath
        +Show()
        +Dispose()
        -InitializeCb()
        -DialogShownCb()
        -UpdateCb(dialog, block) int
        -HandleApply() int
        -HandlePair() int
        -HandleExport() int
        -ConfigureDimensionFilter(block)$
        -ConfigureBalloonFilter(block)$
    }

    class KnCounterService {
        <<static>>
        +KnPrefix string$
        +DigitCount int$
        +GetNextKnNumber(part) int$
        +Format(n) string$
        ~ReadUpperText(part, IdSymbol) string$
        ~ReadUpperText(part, PmiIdSymbol) string$
    }

    class BalloonService {
        <<static>>
        +CreateDraftingBalloon(part, dim, kn) IdSymbol$
        +CreatePmiBalloon(part, pmiDim, kn) PmiIdSymbol$
        -AttachLeader(part, leaderBuilder, target)$
        -OffsetPoint(origin) Point3d$
    }

    class MappingService {
        <<static>>
        +AttrOwned string$
        +AttrNumber string$
        +AttrDimTag string$
        +AttrDimSnapshot string$
        +MarkBalloonAsToolOwned(balloon, kn, dim)$
        +ManualPair(balloon, dim, kn)$
        +CollectRecords(part) List~KnBalloonRecord~$
        -BuildDimensionLookup(part) Dictionary$
        -BuildRecordCore(...) KnBalloonRecord$
    }

    class ExcelExportService {
        <<static>>
        +Export(path, records)$
    }

    class KnBalloonRecord {
        <<model>>
        +KnNumber string
        +DimensionType string
        +LiveValueText string
        +SnapshotText string
        +UpperTolerance string
        +LowerTolerance string
        +OwningViewOrSheet string
        +DimensionJournalId string
        +Note string
    }

    class NXOpen_Annotations {
        <<NXOpen API>>
        IdSymbol
        IdSymbolBuilder
        PmiIdSymbol
        Dimension / PmiDimension
        LeaderBuilder / LeaderData
    }

    class NXOpen_BlockStyler {
        <<NXOpen API>>
        BlockDialog
        SelectObject
        IntegerBlock
        Toggle
        StringBlock
        Button
    }

    class ClosedXML {
        <<3rd party>>
        XLWorkbook
        IXLWorksheet
    }

    Program --> KnBalloonDialog : creates
    KnBalloonDialog --> KnCounterService : uses
    KnBalloonDialog --> BalloonService : uses
    KnBalloonDialog --> MappingService : uses
    KnBalloonDialog --> ExcelExportService : uses
    BalloonService --> MappingService : MarkBalloonAsToolOwned
    KnCounterService --> NXOpen_Annotations
    BalloonService --> NXOpen_Annotations
    MappingService --> NXOpen_Annotations
    MappingService ..> KnBalloonRecord : produces
    ExcelExportService ..> KnBalloonRecord : consumes
    ExcelExportService --> ClosedXML
    KnBalloonDialog --> NXOpen_BlockStyler
```

## Katmanlar

| Katman | Sorumluluk | Dosya |
|---|---|---|
| **Entry** | NX'in DLL'i yüklediği giriş noktası | `Program.cs` |
| **UI** | Block UI Styler dialog + olay kanalları | `Dialog/KnBalloonDialog.cs` + `.dlx` |
| **Service** | İş kuralları (sayaç, balon, mapping, excel) | `Services/*.cs` |
| **Model** | Salt veri taşıyıcısı | `Models/KnBalloonRecord.cs` |
| **API** | NXOpen .NET + ClosedXML | external |

## Bağımlılık Yönü

```
Program → Dialog → Services → (NXOpen + Models + ClosedXML)
```

Tek yönlü, üst katmanlar aşağıyı bilir, tersi yok. Servisler birbirini çağırırken yalnızca **BalloonService → MappingService** kenarı var (attribute setlemek için).

## Statik vs Instance

- **Instance:** Sadece `KnBalloonDialog` (dialog state'i tutuyor).
- **Static:** Tüm servisler. State tutmuyorlar; her çağrıda part'tan oku, işle, döndür. Bu mimari kararla "tool kapansa da bozulmasın" gereksinimi karşılandı — RAM state'i yok, kaynak NX part'ı.

## Persistence (Class diagram'ın dışında ama önemli)

```mermaid
flowchart LR
    Part[(NX Part .prt)] -->|IdSymbol koleksiyonu| Sym1[Balon KN001]
    Part -->|IdSymbol koleksiyonu| Sym2[Balon KN002]
    Sym1 -->|User Attrs| A1[KN_NUMBER<br>KN_DIM_TAG<br>KN_DIM_VALUE<br>KN_TOOL_OWNED]
    Sym2 -->|User Attrs| A2[KN_NUMBER<br>KN_DIM_TAG<br>KN_DIM_VALUE<br>KN_TOOL_OWNED]
```

Tüm KN ↔ ölçü bağı NX'in kendi annotation + attribute mekanizmasında yaşıyor. Tool tarafında ek bir veritabanı / dosya yok.
