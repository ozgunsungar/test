# NX2406 KN Atama & Excel İhraç Demo Tool'u

## Context

Kullanıcı NX2406 üzerinde NXOpen API ile çalışan bir demo tool istiyor. Tool, ribbon butonundan açılacak; Drawing veya PMI fark etmeksizin **ölçülere (dim)** ve **tabular note hücrelerine (cell)** otomatik artan KN numarası (`KN001`, `KN002` …) basıp istendiğinde KN ↔ değer eşleşmelerini Excel'e dökmeli.

KN, ayrı bir balon (ID Symbol annotation) olarak değil, hedef objenin **text'ine** NX kontrol kodları ile yazılır: `<&71><+> KN001 <+><&71>`. Bu kontrol kodu kombinasyonu dimension text'in yanında balonsu bir görünüm üretir; KN ile ölçü/hücre aynı objeye ait olduğundan ayrı leader, ayrı bağ veya manuel eşleştirme mantığına gerek kalmaz.
- **Dimension**'da: `SetAppendedText(After, …)` alanına yazılır (üzerine yazma riski yok, suffix slot).
- **Tabular note cell**'inde: hücrenin mevcut metninin **sonuna append** edilir (hücredeki kullanıcı verisi korunur).

Tool kapanıp tekrar açıldığında "son KN" bilgisini doğru tespit etmeli; bunun için part içindeki tüm dimension After-text'leri **ve tüm tabular note cell text'leri** taranıp `KN(\d{3})` pattern'i ile max numara bulunur — bu, **manuel olarak da** basılmış KN'leri kapsar. Üstüne kullanıcının manuel integer override imkânı vardır (silinen numarayı yeniden basmak için).

## Karar Özeti (Kullanıcıyla onaylandı)

| Konu | Karar |
|---|---|
| KN yazma yöntemi (dim) | Dimension'ın **Appended Text → After** alanı (ayrı annotation YOK) |
| KN yazma yöntemi (cell) | Tabular note hücresinin mevcut metninin **sonuna append** (var olan veri korunur) |
| Format | `KN` + 3 hane sıfır-pad → `KN001`, `KN002`, … `KN999` |
| Kontrol kodu | `<&71><+> KN001 <+><&71>` (NX karakter kodu balonsu görünüm üretir) |
| NXOpen API | Dim: `Annotations.Dimension.SetAppendedText(AppendedTextType.After, …)`. Cell: `Annotations.TableSection.SetCellText(row, col, oldText + suffix)` (PMI varyantları ayni interface'ten türer) |
| Dil / target | **C# .NET Framework 4.7.2**, NXOpen .NET API |
| UI | NX ribbon butonu + **Block UI Styler dialog** (`.dlx`) — **2 buton + kapsam radio** |
| Kapsam | Radio: ○ Tüm Ölçüler  ○ Seçili Ölçüler  ○ Seçili Hücreler. Kapsama göre ilgili selection list enable/disable |
| Drawing / PMI ayrımı | Tek selection block; obje tipine göre koda branch (drafting vs PMI). Her ikisinde de aynı `SetAppendedText` / `SetCellText` API'si var (PMI türleri ata sınıflardan miras alır) |
| KN sayacı | **Tek global** sayaç — dim ve cell aynı seriyi paylaşır. Açılışta part'taki tüm dim After-text'leri + tüm tabular note hücre text'leri taranır → `KN(\d{3})` regex max → next = **max+1**. Kullanıcı dialog'da int alanından override edebilir |
| Numara sırası | Birden çok dim/cell seçildiğinde **SelectionList seçim sırası** (NX `SelectionList` order'ı korur) |
| Manuel KN | Ayrı obje olmadığı için "manuel eşleştirme" akışı yok. Manuel olarak `KN###` yazan herkes scan'e otomatik dahil olur |
| Yıkıcı işlem | "Tüm Ölçüler → KN Sil" tek tıkla part'taki **bütün** KN'leri siler — Apply'dan önce **confirm popup** zorunlu |
| Persistence | KN bilgisi dim'in kendi After-text'inde / cell'in kendi text'inde yaşar — ayrı user attribute YOK (opsiyonel `KN_TOOL_OWNED=true` audit attribute) |
| Excel kütüphanesi | **ClosedXML** (MIT lisans, NuGet) |

## Proje Yapısı

```
Project/
  KnBalloonTool.sln
  KnBalloonTool/
    KnBalloonTool.csproj            // target: net472; ref NXOpen DLL'leri (%UGII_BASE_DIR%\UGII\managed\)
    Program.cs                       // NX entry: Main(), ufusr(), GetUnloadOption()
    Dialog/
      KnBalloonDialog.dlx            // Block UI Styler XML (NX'in nxbsv tool'u ile üretilir)
      KnBalloonDialog.cs             // Dialog code-behind: Initialize, apply_cb, update_cb
    Services/
      KnCounterService.cs            // Tüm dim AppendedText + tüm cell text'lerini tara → max KN
      KnDimensionWriter.cs           // Dimension.SetAppendedText(After, …) yaz/sil
      KnCellWriter.cs                // TableSection.SetCellText(row,col,…) append/sil (mevcut metni korur)
      KnScopeExecutor.cs             // Kapsam radio'ya göre Tüm/Seçili Dim/Seçili Cell üzerinde Yaz/Sil orkestrasyonu
      ExcelExportService.cs          // ClosedXML ile XLSX yazma
    Models/
      KnRecord.cs                    // KnNumber, SourceType (Dim/Cell), LiveValue, Tolerance, View/Sheet, JournalId
  startup/
    kn_balloon.men                   // Ribbon/menubar registration script
    kn_balloon_tab.xml               // (opsiyonel) custom ribbon tab tanımı
    images/
      kn_balloon_24.png              // 24x24 ribbon ikonu
      kn_balloon_16.png
  README.md                          // build & deploy talimatları
```

## NXOpen API Eşlemeleri

### Selection Filter (Block UI Styler)
İki ayrı SelectionList block (kapsam radio'sundan birine göre enable):
- **selectionDims** — Mask: `Annotations.Dimension`, `Annotations.PmiDimension` (her ikisi de `Annotations.Annotation` ata sınıfından türer; tek mask ile kabul edilebilir).
- **selectionCells** — Mask: tabular note cell objesi (`Annotations.Tag` / `Annotations.TableSection` cell tag). NX'te cell'ler "Annotation Cell" filtre tipi ile seçilebilir; NXOpen tarafında `NXOpen.Annotations.Tag` (cell tag) tipinde döner. Drafting tabular note (`Annotations.Table`) ve PMI tabular note (`Annotations.PmiTable`) cell'leri aynı `TableSection` API'sini paylaşır.

### KN yazma — Dimension (Drafting + PMI)
```csharp
const string KN_FMT  = "<&71><+> KN{0:D3} <+><&71>";    // KN + 3 hane sıfır pad
const string KN_PATT = @"<&71><\+>\s*KN(\d{3})\s*<\+><&71>";  // silme/değişim regex'i

string suffix = string.Format(KN_FMT, knNumber);
dim.SetAppendedText(Annotations.AppendedTextType.After, new[] { suffix });
```
- `AppendedTextType.After` → suffix konumu (sağ).
- **Mevcut suffix korunması:** önce `GetAppendedText(After)` ile mevcut metni oku → `KN_PATT` regex'iyle eski KN bloğunu çıkar → kalanın sonuna yeni KN'yi ekle → `SetAppendedText` ile geri yaz.
- `PmiDimension : Dimension` → aynı API. Tek helper: `void WriteKn(Annotations.Dimension dim, int kn)`.

### KN yazma — Tabular Note Cell (Drafting + PMI)
```csharp
// cellTag: Annotations.Tag (Block UI Styler selection'dan)
// Cell'in ait olduğu TableSection ve (row, col) bilgisini cellTag'tan al
var section = cellTag.OwningSection;          // Annotations.TableSection
section.GetCellCoordinates(cellTag, out int row, out int col);

string current = section.GetCellText(row, col) ?? string.Empty;
string cleaned = Regex.Replace(current, KN_PATT, "").TrimEnd();
string newText = string.IsNullOrEmpty(cleaned)
    ? string.Format(KN_FMT, knNumber)
    : cleaned + " " + string.Format(KN_FMT, knNumber);

section.SetCellText(row, col, newText);
```
- **Append davranışı:** mevcut hücre metni korunur, sondaki eski KN (varsa) regex ile temizlenip yenisi eklenir.
- PMI tabular note cell'i de aynı `TableSection` interface'inden gelir → ortak helper.

### KN silme / değiştirme
- Dim: `GetAppendedText(After)` → regex ile `KN###` bloğunu çıkar → kalanı `SetAppendedText` ile geri yaz (tamamen boşsa `new string[0]`).
- Cell: `GetCellText` → regex ile `KN###` bloğunu çıkar → `SetCellText` ile geri yaz (mevcut kullanıcı metni korunur).

### Opsiyonel attribute (tool-owned işareti)
- `obj.SetUserAttribute("KN_TOOL_OWNED", "true", Update.Option.Now)` — manuel yazılmış KN'lerle bizim yazdığımızı ayırmak istersek. Audit için.

### Sayaç tarama (KnCounterService)
```csharp
int max = 0;
var rx = new Regex(@"KN(\d{3})");

void ProbeText(string s)
{
    if (string.IsNullOrEmpty(s)) return;
    foreach (Match m in rx.Matches(s))
        max = Math.Max(max, int.Parse(m.Groups[1].Value));
}

void ProbeDim(Annotations.Dimension d)
{
    foreach (var line in d.GetAppendedText(Annotations.AppendedTextType.After) ?? new string[0])
        ProbeText(line);
}

void ProbeTableSection(Annotations.TableSection sec)
{
    for (int r = 0; r < sec.NumberOfRows; r++)
        for (int c = 0; c < sec.NumberOfColumns; c++)
            ProbeText(sec.GetCellText(r, c));
}

foreach (var d in workPart.Annotations.Dimensions)        ProbeDim(d);
foreach (var d in workPart.PmiManager.PmiDimensions)      ProbeDim(d);
foreach (var t in workPart.Annotations.Tables)            foreach (var sec in t.Sections) ProbeTableSection(sec);
foreach (var t in workPart.PmiManager.PmiTables)          foreach (var sec in t.Sections) ProbeTableSection(sec);
return max + 1;
```
Not: Kontrol kodları (`<&71>`, `<+>`) regex'i etkilemez; pattern sadece `KN###`'i yakalar. Cell ve dim tek sayacı paylaşır.

### Excel export (ExcelExportService) — **anlık değer modu**
- ClosedXML `XLWorkbook`, sheet "KN Listesi"
- Sütunlar: `KN No | Kaynak (Drafting Dim / PMI Dim / Drafting Cell / PMI Cell) | Anlık Değer | Üst Tol. | Alt Tol. | View / Sheet / Tablo[R,C] | Journal ID`
- Kaynak: part'taki tüm `Annotations.Dimensions` + `PmiManager.PmiDimensions` + tüm tabular note hücreleri taranır → text'ten `KN(\d{3})` çıkarılır → her bulunan KN bir satır.
- Anlık değer (dim): `dim.GetDimensionData().Value` (nominal) ya da `GetAnnotationText()[0]`'dan KN bloğu temizlenmiş gövde.
- Anlık değer (cell): hücrenin KN'siz raw text'i (`Regex.Replace(cellText, KN_PATT, "").Trim()`).
- Tolerans (dim): `Dimension.GetDimensionData()` → `ToleranceType` / `ToleranceUpperValue` / `ToleranceLowerValue`. Cell satırlarında tolerans boş bırakılır.
- Lokasyon: Drafting dim için `dim.OwningView.Name` + view'ın `OwningSheet.Name`; PMI dim için `dim.OwningView.Name` (model view adı, boşsa "Model"); cell için `table.Name + "[" + row + "," + col + "]"`.

### Persistence (state nerede tutuluyor?)
- **Tool hafıza tutmaz.** RAM state'i her açılışta sıfır.
- Tek kaynak: **NX part dosyasının kendisi** — KN bilgisi her dim'in kendi After-text'inde / her cell'in kendi text'inde yaşar. Ayrı attribute, ayrı annotation, ayrı haritalama yok.
- Her açılışta `KnCounterService` Dimensions + PmiDimensions + Tables + PmiTables koleksiyonunu döngüyle gezip max KN'yi bulur.
- Avantajları:
  - Part başka makinede açılsa da KN bilgisi part dosyasındadır — tool olmadan da görünür.
  - "Manuel eşleştirme" akışı yok; çünkü KN ile ölçü/hücre zaten aynı objede.
  - `JournalIdentifier` stabilite riski yok; KN hedef objenin **kendi text'inde**.
- "Aradan zaman geçti, yeni ölçü/hücre ekledim" akışı: part'ı aç → tool aç → `InitializeCb`'de `GetNextKnNumber(part)` → sayaç doğru numaradan başlar → yeni öğeleri seç → "KN Yaz".

## Dialog Akışı

`KnBalloonDialog.dlx` block'ları (sadeleştirilmiş 2-buton tasarımı):

1. **Group: "Kapsam"**
   - `radioScope` — RadioBox 3 seçenek: `Tüm Ölçüler` / `Seçili Ölçüler` / `Seçili Hücreler`
   - `selectionDims` — SelectionList (Dimension + PmiDimension); yalnız "Seçili Ölçüler" modunda enable
   - `selectionCells` — SelectionList (Annotation Cell tag); yalnız "Seçili Hücreler" modunda enable
2. **Group: "Numara"**
   - `intStartKn` — Integer (default = scan'den gelen next; kullanıcı override edebilir, silinmiş KN'yi yeniden basmak için)
   - Çoklu seçimde **seçim sırasında** artarak basılır (KN005, KN006, KN007, …).
3. **Group: "Aksiyon"**
   - `buttonWrite` — "KN Yaz" → kapsama göre `KnScopeExecutor.WriteAll(scope, startKn, selectionDims, selectionCells)`
   - `buttonDelete` — "KN Sil" → kapsama göre `KnScopeExecutor.DeleteAll(scope, selectionDims, selectionCells)`
     - **Confirm popup zorunlu** eğer scope = "Tüm Ölçüler" (yıkıcı). `NXOpen.UI.NXMessageBox.Show("Onay", Question, "Part'taki TÜM KN'ler silinecek. Devam?")`
4. **Group: "Excel İhraç"**
   - `stringFilePath` — File save path
   - `buttonExport` — "Excel'e Bas" → part taraması + XLSX yazımı (kapsamdan bağımsız; hep tüm KN'leri export eder)

Kapsam → davranış matrisi (`KnScopeExecutor`):
| Scope | KN Yaz | KN Sil |
|---|---|---|
| Tüm Ölçüler | `workPart.Annotations.Dimensions` + `PmiDimensions` — her birine sırayla intStartKn'den itibaren KN bas | Aynı koleksiyonların hepsinden `KN###` bloğunu sil **(confirm popup sonrası)** |
| Seçili Ölçüler | `selectionDims.GetSelectedObjects()` seçim sırasında KN bas | Sadece seçili dim'lerden sil |
| Seçili Hücreler | `selectionCells.GetSelectedObjects()` seçim sırasında her cell'in mevcut metnine append | Sadece seçili cell'lerden sil |

Callback iskeleti:
- `initialize_cb()` → `KnCounterService.GetNext(workPart)` → `intStartKn.Value`; `radioScope` default = `Seçili Ölçüler`; cell list disabled.
- `update_cb` → `radioScope` değişiminde `selectionDims` / `selectionCells` enable/disable.
- `apply_cb` / `buttonWrite` / `buttonDelete` → `KnScopeExecutor` çağrısı; her apply sonrası `intStartKn.Value = KnCounterService.GetNext(workPart)` (sayaç refresh).

## Ribbon Entegrasyonu

`startup/kn_balloon.men` (NX MenuScript):
```
VERSION 120
EDIT UG_GATEWAY_MAIN_MENUBAR
AFTER UG_HELP
   CASCADE_BUTTON  KN_BALLOON_TAB
   LABEL Balonlama
END_OF_AFTER

MENU KN_BALLOON_TAB
   BUTTON KN_BALLOON_OPEN
   LABEL KN Balon Tool
   BITMAP kn_balloon_24
   ACTIONS  KnBalloonTool.dll
END_OF_MENU
```

Deployment: `KnBalloonTool.dll` + `kn_balloon.men` + `images/*` → `%UGII_USER_DIR%\startup\` (NX2406 başlangıçta tarar). Alternatif: `Customer Defaults → Gateway → Custom Directories`.

## Kritik Dosyalar (modifikasyon listesi)

Yeni eklenecekler (repo şu an boş):
- `Project/KnBalloonTool.sln`
- `Project/KnBalloonTool/KnBalloonTool.csproj`
- `Project/KnBalloonTool/Program.cs`
- `Project/KnBalloonTool/Dialog/KnBalloonDialog.dlx`
- `Project/KnBalloonTool/Dialog/KnBalloonDialog.cs`
- `Project/KnBalloonTool/Services/{KnCounterService,KnDimensionWriter,KnCellWriter,KnScopeExecutor,ExcelExportService}.cs`
- `Project/KnBalloonTool/Models/KnRecord.cs`
- `Project/startup/kn_balloon.men`
- `Project/startup/images/kn_balloon_24.png` (placeholder)
- `Project/README.md` — build + NX deployment talimatı

Mevcut `Project/test.py` silinebilir (demo placeholder).

## Verification / Test Plan

1. **Build:** Visual Studio 2022, `Project/KnBalloonTool.sln` aç → NXOpen referansları `C:\Program Files\Siemens\NX2406\UGII\managed\` altından eklenmiş olmalı (`Copy Local=False`). Build → `bin\Release\KnBalloonTool.dll`.
2. **Deploy:** DLL + `startup\` içeriğini `%UGII_USER_DIR%\startup\` altına kopyala (örn. `C:\Users\<user>\AppData\Local\Siemens\NX2406\startup\`).
3. **Smoke:** NX2406 aç, test part'ı (drawing + birkaç dim + 1 tabular note içeren) yükle. Ribbon'da "Balonlama" butonu görünmeli; tıkla → dialog açılmalı, `radioScope` = Seçili Ölçüler, `intStartKn` = 1.
4. **Seçili Ölçü path:** Drawing sheet'inde 3 dim seç → "KN Yaz" → her dim'in sağında `<&71><+> KN001 <+><&71>` (balonsu görünüm) basılmalı; **seçim sırasında** KN001, KN002, KN003.
5. **PMI path:** Modeling work part'ında PMI dim oluştur, dialog'u tekrar aç → `intStartKn` = 4; PMI dim seç → "KN Yaz" → PMI dim'in After-text'inde KN004 görünmeli.
6. **Seçili Hücre path:** Drawing'deki tabular note'tan 2 cell seç (önceden "Malzeme" yazıyor) → scope=Seçili Hücreler → "KN Yaz" → cell text: `Malzeme <&71><+> KN005 <+><&71>` ve KN006. **Mevcut "Malzeme" metni korunmalı**.
7. **Tüm Ölçüler path:** Yeni boş bir part'a 5 dim ekle, scope=Tüm Ölçüler, intStartKn=1 → "KN Yaz" → 5 dim'in hepsi KN001…KN005 ile basılmalı (drawing+PMI karışıksa hepsi).
8. **Counter persist:** Dialog'u kapat, NX'i kapat/aç, part'ı tekrar yükle, tool aç → `intStartKn` doğru next değerinden başlamalı (dim + cell tarama dahil).
9. **Manuel KN testi:** NX native Edit Annotation → bir dim'e elle `<&71><+> KN042 <+><&71>` yaz. Tool'u tekrar aç → next = KN043.
10. **Override:** `intStartKn` alanına 7 yaz (varsayalım KN007 silinmiş), bir dim seç, "KN Yaz" → o dim'e KN007 yazılmalı.
11. **Seçili Sil:** KN basılı bir dim'i seç → "KN Sil" → After-text'inden sadece `KN###` bloğu temizlenmeli, dim'in nominal değeri ve diğer suffix metni bozulmamalı.
12. **Cell Sil:** KN basılı bir cell seç → "KN Sil" → cell metni "Malzeme" geri dönmeli, KN bloğu kaybolmalı.
13. **Tüm Sil + confirm:** scope=Tüm Ölçüler → "KN Sil" → **confirm popup çıkmalı**, Cancel → hiçbir şey değişmemeli; tekrar dene + OK → part'taki tüm dim KN'leri silinmeli (cell'ler etkilenmemeli, çünkü kapsam "Tüm Ölçüler" — sadece dim).
14. **Excel:** Path seç, "Excel'e Bas" → XLSX aç → part'taki tüm KN'ler satır halinde gelmeli; Kaynak/Anlık Değer/Tolerans/Lokasyon kolonları dolu, cell kaynakları "Drafting Cell" / lokasyon `TabloAdı[2,3]` formatında.
15. **Anlık değer doğrulaması:** Bir dim'in nominal değerini elle düzenle (10 → 12) → Excel'i tekrar bas → Anlık Değer kolonu 12 olmalı.
16. **Audit attribute (opsiyonel):** Tool'un bastığı dim/cell'lere bak (Information → Object → Attributes): `KN_TOOL_OWNED=true` set olmalı; manuel KN042 yazılmış dim'de bu attribute olmamalı.

## Riskler / Notlar

- **Kontrol kodu rendering:** `<&71>` font-table'a bağlı bir özel karakter; NX'in default text font'unda balonsu glif üretir. Drawing template farklı font kullanıyorsa görünüm değişebilir → ilk smoke test'te kontrol edilmeli, gerekirse font override ile `AnnotationStyle` ayarlanır.
- **PMI Dimension API:** `PmiDimension : Dimension` kalıtımıyla aynı `SetAppendedText` imzası bekleniyor; ilk PMI testinde NXOpen referans dokümanı ile doğrulanmalı.
- **TableSection API:** `Annotations.TableSection.SetCellText(row, col, text)` ve `GetCellText` NX12+'dan beri mevcut; NX2406'da hem drafting (`Annotations.Table`) hem PMI tabular note (`Annotations.PmiTable`) için aynı interface. Cell tag'ten (row, col) çözümleme için `OwningSection` + `GetCellCoordinates` (veya iterate fallback) gerekli — implementasyonda doğrulanmalı.
- **Cell selection mask:** Block UI Styler'da "Annotation Cell" filter type'ın aktif olabilmesi için drawing veya PMI tablo cell'leri selectable olmalı; mask: `UF_drafting_entity_type` + cell subtype. Manuel doğrulama gerekebilir.
- **AppendedText üzerine yazma:** Bir dim'in After-text'inde zaten kullanıcı suffix'i (örn. "TYP") varsa, naif `SetAppendedText` üzerine yazar. Tasarımda: önce mevcut After'ı oku → KN bloğunu regex ile değiştir → kullanıcı text'ini koru. Aynı mantık cell için de geçerli (append).
- **Tek sayaç çakışması:** Dim ve cell aynı KN serisini paylaşır; aynı anda iki kullanıcı tool kullanırsa double-KN riski yok çünkü sayaç her apply öncesi part-scan ile yenilenir. (Tek-kullanıcı varsayımıyla.)
- **"Tüm Ölçüler → Sil" yıkıcı:** Confirm popup zorunlu. "Geri al" desteği yok — kullanıcı NX'in Undo'sundan dönmek zorunda.
- **Numara sırası:** `SelectionList.GetSelectedObjects()` NX'te seçim sırasını korur; ancak Ctrl+A gibi bulk select'lerde sıra deterministik olmayabilir → kullanıcıyı uyaracak küçük not dialog'da yer alacak.
- **ClosedXML net472 desteği:** v0.97.x net472 ile uyumlu; v0.100+'da minimum net46 olmasına rağmen bazı bağımlılıkları net472'de çalışıyor. NuGet'ten çekildiğinde DLL'lerin `Copy Local=True` ve startup klasörüne dahil edilmesi gerek.
- **Ribbon vs MenuScript:** `.men` cascade button menubar'da görünür; gerçek "Ribbon Tab"ı için ek olarak Customer Defaults / Role file düzenlemesi gerekebilir. Demo için menubar yeterli; ribbon istiyorsan ikinci iterasyonda XML role definition eklenir.
