# KN Balloon Tool (NX2406 demo)

NXOpen .NET tabanlı demo. Drawing veya PMI üzerinde seçilen ölçülere
otomatik artan `KN001`, `KN002`... numaralı **ID Symbol** balonu atar ve
KN ↔ ölçü değeri eşleşmelerini Excel'e ihraç eder.

## Dokümanlar

| Doküman | İçerik |
|---|---|
| [`docs/UserGuide.md`](docs/UserGuide.md) | Kullanım kılavuzu — kurulum, senaryolar, SSS |
| [`docs/UML.md`](docs/UML.md) | UML sınıf diyagramı + persistence şeması |
| [`docs/UseCase.md`](docs/UseCase.md) | Use case + akış + sıra diyagramları |
| [`docs/Presentation.md`](docs/Presentation.md) | Sunum (Marp/Slidev uyumlu) |

## Özellikler

- Tek dialog: balon atama, manuel eşleştirme, Excel ihraç
- Drawing (`Annotations.IdSymbol`) ve PMI (`PmiManager.PmiIdSymbols`) tek selection
- KN sayacı **tool kapansa da bozulmaz**: açılışta part'taki tüm
  ID Symbol'ler taranır, `KN\d+` regex ile maksimum bulunur, sıradaki = max+1
- Manuel oluşturulmuş balonlar da sayım ve Excel'e dahil
- Bizim oluşturduğumuz balona attribute set:
  `KN_TOOL_OWNED`, `KN_NUMBER`, `KN_DIM_TAG`, `KN_DIM_VALUE` (snapshot)
- Excel **anlık değer modunda**: export sırasında `KN_DIM_TAG` ile
  ölçüye geri ulaşılır, `GetAnnotationText()` ile o anki nominal +
  tolerans satırları, `OwningView/Sheet.Name` ile view/sheet okunur.
  Ölçü silinmiş ya da journal id değişmişse `KN_DIM_VALUE` snapshot'a
  fallback ve "ÖLÇÜ BULUNAMADI" notu basılır.
- Excel kolonları: `KN No | Tip | Anlık Değer | Üst Tol. | Alt Tol. |
  View / Sheet | Snapshot | Dim Journal ID | Not`

## Persistence (state nerede tutuluyor?)

- **Tool hafıza tutmaz.** Dialog kapanınca tüm RAM state'i gider.
- Tek kalıcı kaynak: **part dosyasının içindeki annotation'lar +
  bizim onlara yazdığımız user attribute'ler**.
- Her açılışta `KnCounterService.GetNextKnNumber(part)` `IdSymbols`
  + `PmiIdSymbols` koleksiyonlarını **baştan döngüyle gezer**,
  `KN(\d+)` regex'iyle max'ı bulur, +1 döndürür.
- Excel export'taki tarama da aynı şekilde her seferinde fresh çalışır;
  ayrıca tüm Dimension/PmiDimension'lar `JournalIdentifier` ile
  dict'lenir ki balon → ölçü lookup'ı O(1) olsun.
- Sonuç: part'ı başka makinede aç, NX restart et, aylar sonra dön —
  next-KN doğru hesaplanır ve Excel doğru basılır.

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
9. **Anlık değer testi:** KN001'in bağlı olduğu ölçüyü düzenle
   (örn. 10 → 12). Tekrar "Excel'e Bas". XLSX'i aç → KN001 satırında
   "Anlık Değer" = 12, "Snapshot" = 10. Tolerans/View kolonları dolu.
10. **Yeniden açılış testi:** NX'i kapat-aç, part'ı tekrar yükle,
    yeni bir ölçü ekle, tool'u aç → `KN No` doğru sıradan (12) başlar.
    Yeni ölçüyü balonla, Excel'e bas → eski KN'ler de hâlâ orada.

## API Kullanım Notları (NXOpen .NET — Siemens docs ile doğrulandı)

- **Dialog** `NXOpen.UI.GetUI().CreateDialog("KnBalloonDialog.dlx")` ile
  yaratılır; `.dlx` `UGII_USER_DIR\application` veya `startup` altında
  aranır.
- **IdSymbolBuilder** üyeleri builder üzerinde *doğrudan* set edilir:
  `Type = IdSymbolBuilder.SymbolTypes.Circle`, `UpperText`, `Size`,
  `Origin`, `Leader`. `Style` üzerinden değil.
- **Var olan IdSymbol'ün UpperText'i** okumak için
  `IdSymbols.CreateIdSymbolBuilder(existingSymbol)` ile builder
  yaratılır, `UpperText` okunur, `Destroy()` çağrılır.
- **LeaderData** `part.Annotations.CreateLeaderData()` ile üretilir;
  `SetTermObject(target)` ile ölçüye bağlanır,
  `leaderBuilder.Leaders.Append(leader)` eklenir.
- **SetUserAttribute** imzası `(title, index, value, Update.Option)` —
  scalar attribute için `index = -1`.
- **MaskTriple** sabitleri `NXOpen.UF.UFConstants.UF_*` (örn.
  `UF_drafting_entity_type` + `UF_dimension_subtype`,
  `UF_pmi_entity_type` + `UF_pmi_dimension_subtype`).

## Bilinen Sınırlar

- `JournalIdentifier` çoğu annotation için kalıcıdır ancak edit/replace
  sonrası değişebilir; bu yüzden balonda `KN_DIM_VALUE` snapshot'ı da
  saklanıyor (Excel için fallback).
- PMI ID Symbol koleksiyonu lisans yoksa try/catch ile sessizce atlanır.
- `.men` cascade button menubar'a iner; gerçek ribbon tabı için NX role
  XML'i ayrıca düzenlenmeli (demo kapsamı dışı).
