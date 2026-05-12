# Kullanıcı Diyagramları

## 1. Use Case Diyagramı

Aktör ve sistem etkileşimleri.

```mermaid
flowchart TB
    User((CAD Mühendisi))

    subgraph Tool[KN Balloon Tool]
        UC1([Tool'u Aç])
        UC2([Ölçü Seç ve Balon Ata])
        UC3([KN Numarasını Override Et])
        UC4([Manuel Balon Eşleştir])
        UC5([Excel'e İhraç Et])
    end

    subgraph NX[NX 2406 / Part]
        NXPart[(Part .prt)]
        NXDim[Drafting / PMI Dim]
        NXBalloon[ID Symbol Balon]
        NXAttr[User Attributes]
    end

    User --> UC1
    User --> UC2
    User --> UC3
    User --> UC4
    User --> UC5

    UC1 -.okur.-> NXBalloon
    UC2 -.oluşturur.-> NXBalloon
    UC2 -.attribute yazar.-> NXAttr
    UC4 -.attribute yazar.-> NXAttr
    UC5 -.canlı okur.-> NXDim
    UC5 -.snapshot okur.-> NXAttr
    UC5 -.xlsx üretir.-> Excel[(Excel Dosyası)]
```

## 2. Ana Kullanım Akışı (User Flow)

```mermaid
flowchart TD
    Start([Kullanıcı NX'i Açar]) --> OpenPart[Part Aç]
    OpenPart --> ClickRibbon[Ribbon → Balonlama → KN Balon Tool]
    ClickRibbon --> DialogOpen{Dialog Açıldı}

    DialogOpen --> Scan["Sistem: tüm balonları tara<br>(KnCounterService)"]
    Scan --> ShowNext["intKnNumber alanı: 'next' KN gösterir"]

    ShowNext --> Choose{Ne Yapacak?}

    Choose -->|Yeni balon| Pick[Ölçüleri Seç]
    Pick --> Apply[Balon Ata]
    Apply --> Create["BalloonService:<br>ID Symbol + Leader + Attrs"]
    Create --> Increment[Sayaç ++]
    Increment --> Choose

    Choose -->|Manuel balonu bağla| PickBalloon[Manuel Balon + Ölçü Seç]
    PickBalloon --> Pair["MappingService.ManualPair:<br>attribute backfill"]
    Pair --> Choose

    Choose -->|Excel| Path[Çıktı Yolu Yaz]
    Path --> Export["MappingService.CollectRecords<br>+ ExcelExportService.Export"]
    Export --> XLSX[(KN listesi .xlsx)]
    XLSX --> Choose

    Choose -->|Bitti| Close([Dialog Kapat])
```

## 3. Sıra Diyagramı: Balon Atama

```mermaid
sequenceDiagram
    actor User as Kullanıcı
    participant UI as KnBalloonDialog
    participant Counter as KnCounterService
    participant Balloon as BalloonService
    participant Map as MappingService
    participant NX as NX Part

    User->>UI: Tool'u aç
    UI->>Counter: GetNextKnNumber(part)
    Counter->>NX: IdSymbols + PmiIdSymbols tarama
    NX-->>Counter: tüm balonlar
    Counter-->>UI: max+1 (örn. 6)
    UI-->>User: intKnNumber = 6

    User->>UI: 3 ölçü seç + "Balon Ata"
    loop her ölçü için
        UI->>Balloon: CreateDraftingBalloon(part, dim, "KN006")
        Balloon->>NX: IdSymbolBuilder.Commit
        NX-->>Balloon: yeni IdSymbol
        Balloon->>Map: MarkBalloonAsToolOwned(sym, "KN006", dim)
        Map->>NX: SetUserAttribute × 4
        Note over Map,NX: KN_NUMBER, KN_DIM_TAG,<br>KN_DIM_VALUE, KN_TOOL_OWNED
        Balloon-->>UI: created
        UI->>UI: sayaç++ (7)
    end
```

## 4. Sıra Diyagramı: Excel İhraç (Anlık Değer)

```mermaid
sequenceDiagram
    actor User as Kullanıcı
    participant UI as KnBalloonDialog
    participant Map as MappingService
    participant Excel as ExcelExportService
    participant NX as NX Part

    User->>UI: "Excel'e Bas"
    UI->>Map: CollectRecords(part)

    Map->>NX: tüm Dimension + PmiDimension oku
    NX-->>Map: ölçü listesi
    Map->>Map: JournalId → Annotation dict kur

    Map->>NX: tüm IdSymbol + PmiIdSymbol oku
    NX-->>Map: balon listesi

    loop her balon için
        Map->>NX: GetUserAttributeAsString(KN_DIM_TAG)
        NX-->>Map: journalId
        alt dict'te bulundu
            Map->>NX: ann.GetAnnotationText() (canlı)
            NX-->>Map: [nominal, üstTol, altTol]
            Map->>NX: ann.OwningView.Name
            NX-->>Map: view/sheet adı
        else bulunamadı (silinmiş)
            Map->>NX: GetUserAttributeAsString(KN_DIM_VALUE)
            NX-->>Map: snapshot
            Note over Map: Not = "ÖLÇÜ BULUNAMADI"
        end
        Map->>Map: KnBalloonRecord doldur
    end

    Map-->>UI: List<KnBalloonRecord>
    UI->>Excel: Export(path, records)
    Excel->>Excel: XLWorkbook + headers + satırlar
    Excel-->>User: .xlsx oluştu
```

## 5. Aktör Kararları (Karar Ağacı)

```mermaid
flowchart TD
    A[Yeni balon mu eklemek?] -->|Evet| B{Sayaç doğru mu?}
    B -->|Evet| C[Direkt 'Balon Ata']
    B -->|Hayır, silinmiş bir KN'i kullan| D[intKnNumber'a istediği no yaz<br>Otomatik artır toggle off]
    D --> C

    A -->|Hayır, eski balonu bağla| E[Manuel Eşleştir grubuna git]
    E --> F[Balon + Ölçü seç → Eşleştir]

    A -->|Hayır, rapor al| G[Çıktı yolu yaz → Excel'e Bas]
```

## Özet — Aktör Beklentileri

| Aktör | Beklenti | Karşılık |
|---|---|---|
| Mühendis | "Ölçüyü seçeyim, KN atansın" | Tek tık + otomatik artan numara |
| Mühendis | "Sonra dönsem de KN'ler korunsun" | Part attribute'lerinde persist |
| Mühendis | "Manuel attığım balonlar da rapora girsin" | Manuel Eşleştir + scan all |
| Mühendis | "Excel'de gördüğüm değer **şu anki** değer olsun" | Live lookup (snapshot fallback) |
| QA / Kalite | "Hangi balonun hangi ölçüye karşı geldiğini göreyim" | Excel: KN No, Dim Journal ID, View/Sheet |
