# KN Balloon Tool (NX2406 demo)

NXOpen .NET tabanlı demo. Drawing veya PMI üzerinde ölçülere ve tabular
note hücrelerine, ayrı bir balon objesi oluşturmadan, kontrol kodu ile
balonsu görünüm üreten otomatik artan `KN001`, `KN002` … suffix'i basar
ve KN ↔ değer eşleşmelerini Excel'e ihraç eder.

## Yaklaşım

KN, ayrı bir ID Symbol objesi olarak DEĞİL, hedef objenin kendi text'ine
NX kontrol kodları ile yazılır:

```
<&71><+> KN001 <+><&71>
```

- **Dimension**'da: `SetAppendedText(AppendedTextType.After, …)` ile
  ölçünün sağına eklenir; mevcut after-text (örn. `TYP`) korunur.
- **Tabular note hücresi**'nde: `TableSection.SetCellText(row, col, …)`
  ile mevcut metnin sonuna eklenir; hücredeki kullanıcı verisi korunur.

Avantajlar:

- Ayrı balon objesi, leader, manuel eşleştirme akışı YOK — KN ile değer
  zaten aynı objede.
- Part başka makinede açılsa da KN bilgisi part dosyasındadır; tool
  olmadan da görünür.
- `JournalIdentifier` stabilite riski yok; KN hedef objenin kendi
  text'inde yaşar.

## Özellikler

- **2 buton + kapsam radio** sade UI: KN Yaz / KN Sil; kapsam:
  `Tüm Ölçüler` / `Seçili Ölçüler` / `Seçili Hücreler`.
- Drawing dim + PMI dim + Drafting tabular note cell + PMI tabular note
  cell desteği.
- **Tek global KN sayacı**: dim ve cell aynı seriyi paylaşır.
- Açılışta part'taki tüm dim after-text ve cell text'leri taranıp
  `KN(\d{3})` regex max+1 ile sıradaki KN bulunur — manuel yazılanlar
  dahil.
- Çoklu seçimde **seçim sırasında** numara atanır (KN005, 006, 007 …).
- "Tüm Ölçüler → KN Sil" yıkıcı; **onay popup'ı** zorunlu.
- Excel ihraç: tüm KN'leri (dim + cell) tek tabloda dökme.

## Excel Çıktısı

Kolonlar: `KN No | Kaynak | Anlık Değer | Üst Tol. | Alt Tol. | Lokasyon | Journal ID | Not`

- `Kaynak`: `Drafting Dim` / `PMI Dim` / `Drafting Cell` / `PMI Cell`
- `Lokasyon`: dim için `Sheet / View`, cell için `TabloAdı[r,c]`
- Anlık değer her export'ta dim'in güncel `GetAnnotationText()`'inden
  çıkarılır (snapshot yok; part'taki canlı değer).

## Proje Yapısı

```
Project/
  KnBalloonTool.sln
  KnBalloonTool/
    KnBalloonTool.csproj         # .NET Framework 4.7.2
    Program.cs                    # ufusr + Main entry
    Dialog/
      KnBalloonDialog.dlx         # Block UI Styler XML (2-buton + radio)
      KnBalloonDialog.cs          # Dialog code-behind
    Services/
      KnFormat.cs                 # KN format + regex sabitleri
      KnDimensionWriter.cs        # SetAppendedText yaz/sil
      KnCellWriter.cs             # SetCellText yaz/sil (append)
      KnCounterService.cs         # Tüm dim + cell tarama → next KN
      KnScopeExecutor.cs          # Kapsama göre orkestrasyon
      KnRecordCollector.cs        # Excel için satır toplama
      ExcelExportService.cs       # ClosedXML XLSX
    Models/
      KnBalloonRecord.cs
  startup/
    kn_balloon.men                # NX MenuScript (menubar entry)
    images/                       # 24x24 / 16x16 PNG ikonları
```

## Build

1. Visual Studio 2022 ile `Project/KnBalloonTool.sln` aç.
2. `UGII_BASE_DIR` ortam değişkeni NX2406 kurulumuna işaret etmeli;
   `csproj` referansları `$(UGII_BASE_DIR)\UGII\managed\` altından NXOpen
   DLL'lerini çeker. Override için MSBuild prop: `/p:NXOpenManagedDir=...`.
3. NuGet ile `ClosedXML 0.102.3` otomatik gelir.
4. Build (Release) → `Project/KnBalloonTool/bin/Release/KnBalloonTool.dll`
   ve yanında `KnBalloonDialog.dlx`.

## Deploy

`%UGII_USER_DIR%\startup\` klasörüne aşağıdakileri kopyala:

- `KnBalloonTool.dll` + `KnBalloonDialog.dlx` + bağımlı `ClosedXML.dll`
  (ve diğer NuGet output DLL'leri)
- `Project/startup/kn_balloon.men`
- `Project/startup/images/kn_balloon_24.png` (ve `_16.png`)

NX2406 başlangıçta `startup\*.men` dosyalarını tarar; "Balonlama" sekmesi
menubar'da Help'in yanında görünür.

## Test Senaryosu

1. Drawing + bir kaç dim + 1 tabular note içeren part aç → "Balonlama →
   KN Balon Tool..." tıkla.
2. Dialog açılır: `Kapsam = Seçili Ölçüler`, `Başlangıç KN No = 1`.
3. 3 dim'i sırayla seç → "KN Yaz" → her dim'in sağında balonsu
   `KN001`/`002`/`003` (seçim sırasında).
4. **PMI:** Kapsam aynı, PMI dim seç → KN004 PMI dim'in After-text'inde.
5. **Cell:** Kapsam = Seçili Hücreler, 2 cell seç (içinde "Malzeme"
   yazıyor) → "KN Yaz" → cell text: `Malzeme <&71><+> KN005 <+><&71>`,
   sonra KN006.
6. **Tüm Ölçüler:** Yeni boş bir part'a 5 dim ekle, Kapsam = Tüm Ölçüler,
   Başlangıç = 1 → "KN Yaz" → 5 dim KN001…KN005.
7. **Counter persist:** NX'i kapat-aç, tool tekrar aç → `Başlangıç KN No`
   doğru next değerden başlar.
8. **Manuel KN:** Native Edit Annotation ile bir dim'in After'ına elle
   `<&71><+> KN042 <+><&71>` yaz. Tool aç → next = 43.
9. **Override:** `Başlangıç KN No = 7`, bir dim seç, "KN Yaz" → o dim'e
   KN007 yazılır.
10. **Seçili Sil:** KN basılı dim → "KN Sil" → sadece KN bloğu temizlenir,
    nominal değer ve "TYP" gibi diğer suffix korunur.
11. **Tüm Sil:** Kapsam = Tüm Ölçüler → "KN Sil" → **onay popup'ı**
    çıkmalı; Cancel → değişmemeli, OK → tüm dim KN'leri silinir.
12. **Excel:** Path seç → "Excel'e Bas" → XLSX'te dim + cell KN'leri,
    Kaynak/Lokasyon/Anlık Değer dolu.

## API Kullanım Notları (NXOpen .NET)

- **Dialog** `NXOpen.UI.GetUI().CreateDialog("KnBalloonDialog.dlx")` ile
  yaratılır; `.dlx` `UGII_USER_DIR\application` veya `startup` altında
  aranır.
- **Dimension After-text** yazma:
  `dim.SetAppendedText(AppendedTextType.After, new[] { "<&71><+> KN001 <+><&71>" })`.
  `PmiDimension : Dimension` olduğundan aynı API.
- **Tabular note cell** yazma: `Tag.OwningSection` → `TableSection`,
  `GetCellCoordinates(tag, out row, out col)` → `GetCellText(r,c)` /
  `SetCellText(r,c,text)`. Drafting (`Annotations.Table`) ve PMI
  (`PmiManager.PmiTables`) cell'leri aynı interface.
- **MaskTriple** sabitleri: dim için `UF_drafting_entity_type` +
  `UF_dimension_subtype`, PMI dim için `UF_pmi_entity_type` +
  `UF_pmi_dimension_subtype`; cell için tabular note subtype'ları.
- **SetUserAttribute** imzası `(title, index, value, Update.Option)` —
  scalar attribute için `index = -1`.
- **KN bloğu regex'i**: `<&71><\+>\s*KN(\d{3})\s*<\+><&71>` — silme/değişim
  için kullanılır; `KN(\d{3})` ise sayaç taramada kullanılır.

## Bilinen Sınırlar

- `<&71>` kontrol kodunun balonsu glif'i NX'in default text font'una
  bağlıdır. Farklı font template'lerde görünüm değişebilir.
- PMI koleksiyonu lisans yoksa try/catch ile sessizce atlanır.
- "Tüm Ölçüler → KN Sil" yıkıcıdır; geri al desteği yok (NX'in Undo'su
  kullanılmalı).
- `.men` cascade button menubar'a iner; gerçek ribbon tabı için NX role
  XML'i ayrıca düzenlenmeli (demo kapsamı dışı).
