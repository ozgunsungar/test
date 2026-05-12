# KN Balloon Tool (NX2406 demo)

NXOpen .NET tabanlı demo. Drawing veya PMI üzerinde seçilen ölçülere
otomatik artan `KN001`, `KN002`... numaralı **ID Symbol** balonu atar ve
KN ↔ ölçü değeri eşleşmelerini Excel'e ihraç eder.

## Özellikler

- Tek dialog: balon atama, manuel eşleştirme, Excel ihraç
- Drawing (`Annotations.IdSymbol`) ve PMI (`PmiManager.PmiIdSymbols`) tek selection
- KN sayacı **tool kapansa da bozulmaz**: açılışta part'taki tüm
  ID Symbol'ler taranır, `KN\d+` regex ile maksimum bulunur, sıradaki = max+1
- Manuel oluşturulmuş balonlar da sayım ve Excel'e dahil
- Bizim oluşturduğumuz balona attribute set:
  `KN_TOOL_OWNED`, `KN_NUMBER`, `KN_DIM_TAG`, `KN_DIM_VALUE`
- Excel: ClosedXML ile `.xlsx` (KN no, tip, nominal, view/sheet, not...)

## Proje Yapısı

```
Project/
  KnBalloonTool.sln
  KnBalloonTool/
    KnBalloonTool.csproj         # .NET Framework 4.7.2
    Program.cs                    # ufusr + Main entry
    Dialog/
      KnBalloonDialog.dlx         # Block UI Styler XML
      KnBalloonDialog.cs          # Dialog code-behind
    Services/
      KnCounterService.cs         # Part tarama + next KN
      BalloonService.cs           # Drafting/PMI ID Symbol oluşturma
      MappingService.cs           # Attribute yaz/oku, manuel eşleştir
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

1. Drawing içeren bir part aç → "Balonlama → KN Balon Tool..." tıkla.
2. Dialog'da `KN No (override)` alanı `1` ile gelir.
3. 3 dim seç → "Balon Ata" → KN001/002/003 oluşur.
4. Tool'u kapat, manuel olarak NX'in ID Symbol komutuyla "KN010" yaz.
5. Tool'u tekrar aç → `KN No` = `11`. Manuel balonu da gördü.
6. "Manuel Eşleştir" grubunda manuel KN010 + bir ölçü seç → "Eşleştir".
7. Information → Object → Attributes ile balonun `KN_DIM_TAG`,
   `KN_DIM_VALUE` attribute'larını doğrula.
8. Çıktı yolu seç → "Excel'e Bas" → `.xlsx`'i aç, tüm KN'ler listede.

## Notlar / Bilinen Sınırlar

- `JournalIdentifier` çoğu annotation için kalıcıdır ancak edit/replace
  sonrası değişebilir; bu yüzden balonda `KN_DIM_VALUE` snapshot'ı da
  saklanıyor (Excel için fallback).
- PMI ID Symbol koleksiyonu lisans yoksa try/catch ile sessizce atlanır.
- Selection mask numaraları NXOpen `Selection.MaskTriple` ile veriliyor;
  NX sürümüne göre subtype değerleri farklı çıkarsa
  `ConfigureDimensionFilter` içindeki triple'lar ayarlanmalı.
- `.men` cascade button menubar'a iner; gerçek ribbon tabı için NX role
  XML'i ayrıca düzenlenmeli (demo kapsamı dışı).
