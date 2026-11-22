---
name: finans-lesson
description: Nirengi'nin "Portföyünle Öğren" eğitim modülünde ders yazarken/zenginleştirirken bu skill'i kullan — Set 0/1/2 dersleri (S0-L8 gibi), ders aşamaları, figürler (LessonFigure.tsx), mini testler, kavram etiketleri ve seed. Dokunulacak altı dosyayı, künye-önce sırasını, MiniMarkdown sınırlarını, tavsiye yasağı taramasını (M7) ve kırılacak sayım testlerini verir. Kullanıcı "ders", "Set 0", "S0-Lx", "müfredat", "figür", "quiz", "kavram sözlüğü", "eğitim seed" dediğinde buna başvur.
---

# Nirengi — Ders yazımı (Faz 6)

Bir ders yazmak metin yazmak değil; **altı dosyayı tutarlı** bırakmaktır. Tek
dosyada kalan değişiklik ya seed'e inmez ya testleri kırar ya da künyeden kayar.
Bu skill o altı dosyayı ve aralarındaki bağı tutar.

## Sıra: künye → içerik → seed → figür → test → doğrulama

### 1. Künyeyi oku — kanonik kaynak odur (D-017)

`.claude/docs/16-CURRICULUM.md` içindeki `§<ders-id>` künyesi dersin
sözleşmesidir: slug, süre, ön bilgi, **tanıtılan/pekiştirilen kavramlar**,
LiveContext token'ları, öğrenme çıktıları, **aşama planı** (numaralı tablo:
katman/tür · konu · figür), işlenmiş örnekler, tuzaklar, figür planı.

Künye ile kod çelişiyorsa **sessizce düzeltme — bildir** (AGENTS.md §4.2) ve
hangisinin kanonik olduğunu söyle. Düzeltmeyi künyeye işlerken GD dosyasına
"Künye düzeltmeleri" başlığı altında tarihli not düş.

**Ama künye de yanılabilir — ve testle korunan bir değişmez künye hücresinden
üstündür.** Gerçek örnek (S0-L8, 2026-11-22): künye aşama 11'i `Context/Trap`
gösteriyordu, kod `Trap(...)` yani `Core` kullanıyordu. `EducationSeedTests` açık
gerekçeli bir değişmez tutuyor — tuzak blokları **her zaman** `DepthTier.Core`,
çünkü yanılgı düzeltmesi tam da başlangıç seviyesine ulaşmalı — ve
`DepthTier.Context + SectionKind.Trap` bileşimi projede hiç kullanılmıyor. Yani
kod doğruydu, künye hücresi yanlıştı. Çelişkide önce **testin gerekçesine** bak;
kodu künyeye uydurmak için bir değişmezi kırma. Hangisi yanlış olursa olsun
kararı bildir, kanonik dokümanı tek başına değiştirme.

### 2. İçerik blokları — `backend/src/Finans.Infrastructure/Seed/EducationContent.cs`

Dersin aşamaları `Build(lessonId, ...)` ile serbest sayıda `Blk` olarak
tanımlanır. Yazım yardımcıları: `Intro` (ilk aşama — "Bu derste ne
öğreneceksin?") · `Core` / `Ctx` / `Deep` (anlatım, derinlik katmanı) ·
`Ex` / `ExDeep` (örnek) · `Trap` (yaygın yanılgı) · `Live` (senin portföyünde) ·
`Src` (son aşama — kaynak + "tavsiye değildir" + "sayılar LLM'den gelmez").

**Açılış ve kaynak blokları zorunludur** (T6.19). Başlık gövdenin ilk `##`
satırından ayrıştırılır — yol haritası onu adım adı olarak gösterir, bu yüzden
başlığı metinde ikinci kez yazma.

Dosyanın başındaki `<remarks>` bloğu içerik kurallarının kanonik listesidir;
yazmaya başlamadan onu oku. Özeti:

- **Tavsiye yok** (`CLAUDE.md` §2): al/sat/yükselir yok, gelecek tahmini yok,
  **enstrüman sıralaması da yok** — örnekler "A/B yatırımı" gibi soyut etiket
  kullanır, "altın dolardan iyiydi" demez (zımni yönlendirme).
- **MiniMarkdown alt kümesi**: `##`/`###`, `- ` liste, `> ` alıntı, `**kalın**`,
  bağlantı, boru işaretli tablo (hizalama satırı zorunlu). **Kod bloğu yok** —
  renderer desteklemiyor, ham metin görünür.
- **TR biçim**: binlik nokta, ondalık virgül (`422.970,50 ₺`).
- Örnek sayılar **statik ve kurgusal**; kullanıcının gerçek verisi yalnız
  `LiveContext` bloğunda, `{{token}}` ile gelir.

**LiveContext token'ları** yalnız `backend/src/Finans.Application/Education/LessonContext.cs`
→ `ContextKeys`'te tanımlı olanlar olabilir; çözülemeyen token'ın satırı düşer.
Künyedeki LiveContext listesi ile kullandığın token'lar birebir aynı olmalı.

### 3. Mini test — aynı dosyada `RemainingQuizzes()`

Set 0 dersleri **9 soru / 3 zorluk** taşır (3 Easy + 3 Medium + 3 Hard).
`SeedQuestion(Type, Difficulty, Prompt, Explanation, Options)`:

- Her soruda **eğitici `Explanation`** var (test bunu denetler) — yalnız doğru
  cevabı değil, neden yanlış olduğunu da anlatır.
- `SingleChoice`/`TrueFalse` → tam **1** doğru seçenek; çok seçmeli türde >1.
- Çeldiriciler gerçek yanılgılardan gelir; tuzak kalıpları burada **serbesttir**
  (M7a) — yanlışın kendisi öğretiliyor.

### 4. Seed kaydı — `backend/src/Finans.Infrastructure/Seed/SeedData.cs`

Set 0 için `SeedSet0Async` içinde, var-mı kontrolüyle (idempotent) dört şey:

1. `Lesson` — `Id = Id("lesson-s0lN")`, `TrackId`, `Slug`, `OrderIndex`, `Title`,
   `Summary`, `BodyMarkdown` (özet anlatım), `EstimatedMinutes`, `Level`,
   `IsPublished`.
2. `LessonPrerequisite` — track içi zincir (S0-L7 → S0-L8).
3. `ConceptTag` + `LessonConceptTag` — künyedeki **tanıtılan** kavramlar; etiket
   `Id("tag-<key>")`, `Key` sözlükteki anahtar, `Label` görünen ad.
4. Bölüm kurucusunun kaydı (`EducationContent` çağrısı).

Kimlikler `SeedData.Id("...")` ile deterministiktir: seed "bu blok var mı?" diye
bakar, böylece sonradan eklenen aşamalar mevcut kurulumlara iner, var olanlar
çoğalmaz. Yeni anahtar uydururken mevcut adlandırmayı sürdür.

### 5. Figürler — `web/src/components/LessonFigure.tsx`

Her figür bir React bileşeni + dosya sonundaki `FIGURES` kaydında bir satır
(`"figure-key": Component`). Künyedeki figür anahtarları ile kayıt birebir
örtüşmeli; `FigureKey` kayıtta yoksa aşama figürsüz render edilir (sessiz kayıp).

- Veriyi figüre **gömme** — figür şemadır, sayı anlatımda durur.
- Çok panelli figürlerde ortak yardımcıları (ör. `WeightStack`) yeniden kullan.
- Renkler tema token'larından; light/dark ikisinde de okunur olmalı.

### 6. Sayım testleri **kırılacak** — güncelle

`backend/tests/Finans.Integration.Tests/EducationSeedTests.cs` yapısal
sayımları sabit tutar: ders · kavram · ön-koşul · quiz · soru · seçenek ·
bölüm · LiveContext sayıları, ön-koşul zinciri, "her soruda bir doğru",
idempotentlik ve sıfır ilerleme. Ders eklemek bunların çoğunu kırar — **beklenen
davranış budur**; yeni gerçek sayımlarla güncelle, testi gevşetme.

```bash
cd backend && dotnet test --filter FullyQualifiedName~EducationSeedTests
pnpm test                       # web + shared (repo kökünden)
pnpm --filter @finans/web build # tsc -b + vite build
```

## Yasak kalıp taraması (M7 / M7a / M7b)

Yazdıktan sonra metni bu üç tarama gözüyle tekrar oku (`16` §9.1, §12-K3):

- **M7** — tavsiye · tahmin · enstrüman sıralaması; `Source` blokları **dahil**.
- **M7a** — grafik/tahmin kalıpları ("formasyon ⇒", "sinyal verdi", "destek
  kırıldı ⇒", "hedef fiyat") **anlatımda yasak**; `Trap` bloğunda ve quiz
  çeldiricisinde serbest.
- **M7b** — özel ad beyaz listesi: yalnız **geniş endeks** adları geçebilir
  (ölçüt işlevi); tek şirket · fon · aracı kurum adı yasak, endeks geçen
  cümlede **eylem fiili** (al/sat/geç/kaydır) bulunmaz.

Ayrıca kişiye özel plan yasağı: "senin vaden şu olmalı", "şu dağılımı kur" gibi
cümleler tavsiye sayılır. Ders çerçeve kurar, karar kullanıcınındır.

## Bitirme kontrol listesi

- [ ] Künye ↔ kod birebir (aşama sayısı · figür anahtarları · kavramlar · LiveContext)
- [ ] Katman/tür (`DepthTier`/`SectionKind`) değişmezleri korunuyor — tuzak blokları `Core`
- [ ] Açılış + kaynak aşaması var; kaynak bloğu hesap yöntemini ve "sayılar koddan" ilkesini yazıyor
- [ ] 9 soru / 3 zorluk, her birinde açıklama ve tek doğru
- [ ] Ders · ön-koşul · kavram etiketleri seed'de, idempotent
- [ ] Figürler `FIGURES` kaydında, light/dark okunur
- [ ] Sayım testleri güncel ve yeşil; web build (`tsc -b`) temiz
- [ ] M7/M7a/M7b taraması temiz
- [ ] Tarayıcıda uçtan uca okundu (figürler render ediliyor)
- [ ] GD dosyası güncellendi + ilgili GitHub issue'su (ders issue'su / alt-görev) kapatıldı ya da durumu yazıldı

Son adım: `finans-green-gate`.
