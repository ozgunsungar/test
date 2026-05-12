# KN Balon Tool — Kullanım Kılavuzu

NX 2406 için balon atama ve Excel ihraç eklentisi.

---

## 1. Kuruluma Hazırlık

### Gereksinimler

- **NX 2406** kurulu olmalı
- Yönetici izinli bir klasöre yazma yetkisi (eklentiyi kopyalamak için)

### Dosyalar

Build çıktınızda şu dosyalar olmalı:

```
KnBalloonTool.dll
KnBalloonDialog.dlx
ClosedXML.dll (ve bağımlı NuGet DLL'leri)
kn_balloon.men
images/kn_balloon_24.png
images/kn_balloon_16.png
```

### Yerleştirme

1. NX'in kullanıcı klasörünü açın:
   ```
   %UGII_USER_DIR%\startup\
   ```
   (örn. `C:\Users\<kullaniciAdi>\AppData\Local\Siemens\NX2406\startup\`)
2. Yukarıdaki tüm dosyaları bu klasöre kopyalayın.
3. `images\` alt klasörünü oluşturun ve PNG'leri içine koyun.
4. NX'i kapatıp tekrar açın.

### Doğrulama

NX'in üst menüsünde **Balonlama → KN Balon Tool…** menüsünü görüyorsanız kurulum tamamdır. Görmüyorsanız:

- `%UGII_USER_DIR%\startup\` klasör yolunu doğrulayın
- `kn_balloon.men` dosyasının orada olduğundan emin olun
- NX'i tekrar başlatın

---

## 2. İlk Bakış: Dialog

**Balonlama → KN Balon Tool…** tıkladığınızda 3 gruplu bir dialog açılır:

```
┌────────────────────────────────────────────┐
│  KN Balon Atama                  [×]       │
├────────────────────────────────────────────┤
│  Balon Ata                                 │
│  ─────────────────                         │
│  [ Ölçü seç (Drafting / PMI) ]  ✏          │
│  [✓] Otomatik artır                        │
│  KN No (override): [ 1 ]                   │
│                          [ Balon Ata ]     │
├────────────────────────────────────────────┤
│  Manuel Eşleştir                           │
│  ─────────────────                         │
│  [ Manuel balon seç     ]  ✏               │
│  [ Eşleşecek ölçü       ]  ✏               │
│                          [ Eşleştir ]      │
├────────────────────────────────────────────┤
│  Excel İhraç                               │
│  ─────────────────                         │
│  Çıktı yolu (.xlsx):                       │
│  [ C:\Users\...\kn_balloons.xlsx ]         │
│                          [ Excel'e Bas ]   │
├────────────────────────────────────────────┤
│                       [ OK ]  [ Cancel ]   │
└────────────────────────────────────────────┘
```

**KN No** alanı dialog açıldığı anda otomatik doldurulur: part'ta zaten KN001..KN005 varsa **6** ile gelir. Yani siz hiçbir şey yapmadan "kaldığınız yerden" devam edersiniz.

---

## 3. Senaryo A: Yeni Balon Atama (Temel)

### Adım Adım

1. **Drawing'i açın** veya PMI'ı içeren modelin work part'ını seçin.
2. **Balonlama → KN Balon Tool…**
3. **Ölçü seç** alanına tıklayın, ardından grafik ekranda balonlamak istediğiniz ölçüleri (Drafting Dimension veya PMI Dimension) sırayla tıklayın. Birden çok seçebilirsiniz.
4. **Otomatik artır** açık (varsayılan). Numara `KN006`'dan başlasın istiyorsanız bir şey değiştirmeyin.
5. **Balon Ata** butonuna basın.
6. Seçtiğiniz her ölçü için sırayla `KN006`, `KN007`, `KN008` … balonları oluşur, ölçülere leader ile bağlanır.
7. **KN No** alanı bir sonraki numaraya otomatik atlar.

### İpuçları

- Yanlış balon attıysanız NX'in standart **Undo** (Ctrl+Z) çalışır.
- Aynı ölçüye iki kere balon atmaktan kaçının; tool engellemez ama Excel'de iki satır oluşur.

---

## 4. Senaryo B: Bir KN Numarasını Tekrar Kullanma

Diyelim `KN003`'ü manuel olarak sildiniz, o numarayı tekrar kullanmak istiyorsunuz.

1. **Otomatik artır** kutusundaki tiki **kaldırın**.
2. **KN No** alanına `3` yazın.
3. Tek bir ölçü seçin → **Balon Ata**.
4. Yeni balon `KN003` ile oluşur. Otomatik artırma kapalı olduğu için sayaç değişmez.
5. İşlem bitince **Otomatik artır**ı tekrar açın; bir sonraki **Balon Ata**'da sayaç yine en yüksek mevcut numaranın bir fazlasından devam eder.

> Tool, mevcut KN numaralarıyla çakışıp çakışmadığını **kontrol etmez**. KN3'ün gerçekten silinmiş olduğundan siz emin olun.

---

## 5. Senaryo C: Manuel Çizilmiş Balonu Bağlama

Diyelim daha önce NX'in kendi ID Symbol komutuyla elle `KN010` çizmişsiniz. Bu balonun Excel raporunda hangi ölçüye karşı geldiği görünsün isteyebilirsiniz.

1. **Manuel Eşleştir** grubuna inin.
2. **Manuel balon seç** → ekranda `KN010` balonuna tıklayın.
3. **Eşleşecek ölçü** → bağlı olması gereken ölçüye tıklayın.
4. **Eşleştir** butonuna basın.
5. Tool, balona `KN_TOOL_OWNED=manual`, `KN_DIM_TAG=<ölçü journal id>`, `KN_DIM_VALUE=<o anki değer>` attribute'lerini yazar.
6. Excel ihraçta artık bu balon "Manuel eşleştirildi" notuyla ve doğru ölçü değeriyle çıkar.

---

## 6. Senaryo D: Excel'e İhraç

### Adımlar

1. **Çıktı yolu** alanına `.xlsx` uzantılı tam dosya yolu yazın (varsayılan: Belgeler klasöründe `kn_balloons.xlsx`).
2. **Excel'e Bas** butonuna basın.
3. Tool, **o anki** part'taki tüm balonları tarar ve XLSX dosyası oluşturur.
4. NX'in **Information Window**'unda "Excel yazıldı: <yol>" mesajı görünür.

### Excel İçeriği

| KN No | Tip | Anlık Değer | Üst Tol. | Alt Tol. | View / Sheet | Snapshot | Dim Journal ID | Not |
|---|---|---|---|---|---|---|---|---|
| KN001 | Drafting | 25.4 | +0.1 | -0.1 | SHEET 1 / TOP@1 | 25.4 | ... | |
| KN002 | Drafting | 12 | | | SHEET 1 / TOP@1 | 10 | ... | |
| KN003 | PMI | 50 ±0.05 | | | MODEL@1 | 50 | ... | |
| KN010 | Drafting | 8 | | | SHEET 1 / FRONT@1 | 8 | ... | Manuel eşleştirildi |
| KN015 | Drafting | | | | | | | MANUEL — eşleştirilmedi |

**Önemli ayrım — Anlık vs Snapshot:**

- **Anlık Değer**: Excel'i bastığınız andaki canlı ölçü değeri. Ölçüyü sonradan düzenlediyseniz güncel değer burada görünür.
- **Snapshot**: Balon ilk atıldığı andaki değer. Revizyon takibi için kullanışlı.

Örneğin balonu attığınızda ölçü 10 idi, sonra 12'ye değiştirdiyseniz → Anlık=12, Snapshot=10. Aradaki fark size revize edildiğini söyler.

---

## 7. Senaryo E: Aradan Zaman Geçti, Devam Ediyorum

Bu tool'un en güçlü tarafı: **Hiçbir şey kaybetmez.**

1. Bir hafta önce parta 5 balon attınız (`KN001..KN005`).
2. Bugün part'ı açın → 2 yeni ölçü eklediniz.
3. **Balonlama → KN Balon Tool…**
4. **KN No** alanı otomatik **6** ile gelir.
5. Yeni 2 ölçüyü seçip **Balon Ata** → `KN006`, `KN007` oluşur.
6. Eski balonlar olduğu gibi durur, hiçbir şey bozulmaz.

> Tool kendi içinde hiçbir state tutmaz. Tüm bilgi NX part dosyasının içinde (annotation + user attribute'ler). Yani part'ı farklı bir bilgisayara da götürseniz aynı şekilde çalışır.

---

## 8. Sık Sorulan Sorular

**S: Hangi numarayı silmişim, geri kazanmak istiyorum. Tool gösterir mi?**
H: Hayır. Mevcut **en yüksek** numarayı bilir, "boşluk" tespiti yapmaz. Manuel olarak override modunu kullanın.

**S: Aynı KN'i yanlışlıkla iki kez attım, ne olur?**
H: Tool izin verir. Excel'de iki ayrı satır olarak gözükür. İstemediğinizi NX'te silin.

**S: Balon attığım ölçüyü sildim, Excel ne yapar?**
H: O satırda **Anlık Değer** boş kalır, **Snapshot** dolu kalır, **Not** sütununda "ÖLÇÜ BULUNAMADI" görünür.

**S: Balon attığım ölçüyü düzenledim (10 → 12), Excel ne yapar?**
H: **Anlık Değer** = 12, **Snapshot** = 10. Fark sizi uyarır.

**S: PMI balonlama çalışmıyor.**
H: Part'ın PMI modülü açık olmalı ve work part'ta olduğunuzdan emin olun. Lisans yoksa tool sessizce PMI tarafını atlar.

**S: Tool açılmıyor, "DLX bulunamadı" hatası alıyorum.**
H: `KnBalloonDialog.dlx` dosyası `%UGII_USER_DIR%\startup\` veya `%UGII_USER_DIR%\application\` klasöründe olmalı.

**S: Excel açıldığında bazı sütunlar boş.**
H: Tolerans/view bilgileri sadece o ölçüde varsa görünür. Manuel olarak çizilmiş ama henüz eşleştirilmemiş balonlarda tüm değer kolonları boş, "MANUEL — eşleştirilmedi" notu gelir.

---

## 9. Klavye / Mouse Kısayolları (Dialog İçi)

| Tuş | İşlev |
|---|---|
| `Enter` | Aktif butonu tetikler |
| `Esc` | Dialog'u iptal eder |
| `Tab` | Sonraki alana geçer |
| `Sol klik` (selection alanında) | Ölçü/balon seç |
| `Orta klik` | Onayla / OK |

---

## 10. Sorun Giderme

| Belirti | Sebep | Çözüm |
|---|---|---|
| Tool menüde yok | `.men` yanlış yerde | `%UGII_USER_DIR%\startup\` |
| "DLX not found" hatası | `.dlx` çıkıştan kopyalanmamış | `KnBalloonDialog.dlx`'i DLL ile aynı yere koy |
| "ClosedXML... not found" | NuGet DLL'leri yok | `ClosedXML.dll` + bağımlılarını startup klasörüne kopyala |
| Balon atılıyor ama leader bağlanmıyor | Ölçü silinmiş veya geçersiz | Yeni ölçü seçin |
| KN sayacı 1'den başlıyor (oysa 5 balon var) | Manuel balonların text'i `KN###` formatında değil | Manuel balonun upper text'ini doğru formata getirin (örn. `KN006`) |
| Excel açılırken bozuk | Hedef yol yazma yetkisi yok | Belgeler klasörünü deneyin |

---

## 11. İletişim / Geri Bildirim

Sorun bildirmek için: `<sizin issue tracker linkiniz>`

Tool versiyonu ve NX versiyonunu mutlaka belirtin.
