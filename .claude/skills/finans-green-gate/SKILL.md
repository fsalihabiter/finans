---
name: finans-green-gate
description: Nirengi'de bir işi "tamam" demeden önce bu skill'i çalıştır — CLAUDE.md §11/§12/§13 kapılarını (testler · güvenlik · gözlemlenebilirlik · ADM kayıtları) tek geçişte denetler ve kanıt raporu üretir. Kod değişikliği, yeni doküman, kalıcı karar, şema/endpoint ekleme ya da düzeltme sonrası; GD kapatmaya hazırlanırken; `adm-close` öncesi; kullanıcı "bitti mi", "yeşil mi", "kapatalım", "kontrol et" dediğinde kullan. Yeşil olmadan görev kapanmaz — bu yüzden yanıtı bitirmeden önce buna başvur.
---

# Nirengi — Yeşil kapı

`CLAUDE.md` §11-13 her anlamlı işten sonra bir dizi kapı tanımlar. Bu kapılar
unutulduğunda tipik sonuç şu olur: iş "bitti" sanılır, sonraki oturum neyin
yarım kaldığını bilmez ve ADM durumu gerçeğin gerisinde kalır. Bu skill o
kapıları sırayla yürütüp **kanıt** üretir.

İki şeyi baştan netleştir: **kabulü sen vermezsin** (AGENTS.md §4.3 — kanıt
sunarsın, insan karar verir) ve **gerekli doğrulama kırmızıyken GD tamamlanamaz**
(INV-07). Kapsam borcu başka şeydir: eksik *kapsama* `.agent-method/pipeline/TEST_DEBT.md`'ye
yazılır (INV-08), kırmızı test oraya yazılmaz.

## 1. Testler (§12)

```bash
cd backend && dotnet test     # unit (Application) + integration
pnpm test                     # web + shared (repo kökünden)
pnpm --filter @finans/web build   # tsc -b + vite build
```

Sor ve cevapla:

- Davranış/kod değiştiyse ilgili test **yazıldı mı**, yeşil mi?
- İki kategori de düşünüldü mü — **unit** (izole hesap; parasal hesapta zorunlu)
  ve **senaryo/olay** (dış API hatası, kullanıcı akışı, fallback)?
- Senaryo `.claude/docs/09-TESTING-STRATEGY.md` §5 kataloğuna işlendi mi,
  durumu `[x]` ve hangi testin karşıladığı yazıldı mı?
- Kırmızı varsa: **bilinen** bir kırmızı mı (`INCIDENTS.md`'de kayıtlı) yoksa
  bu değişikliğin getirdiği yeni bir kırmızı mı? İkincisi kapıyı kapatır.

Çalıştırılamayan bir paket varsa (ortamda .NET/Docker yok gibi) bunu
**eksik doğrulama** olarak yaz — "geçti" deme, "koşulamadı, şu makinede
koşulmalı" de.

## 2. Güvenlik (§13, `11` §10)

- 🔒 **Per-user izolasyon:** dokunulan her veri erişimi `UserId` ile kapsanıyor
  mu? Yeni endpoint varsa IDOR testi (SC-13) var mı ve başkasının kaydı **404**
  mü dönüyor?
- 🔒 Sır repoda yok (anahtar/parola env/secret'ta), diff'te sızan anahtar yok.
- 🔒 Girdi sunucuda doğrulanıyor, hata iç detay sızdırmıyor, sınırda DTO var.
- 🔒 Log'a sır/PII/token yazılmıyor; LLM'e giden portföy anonimleştiriliyor.
- 🔒 Çok kullanıcı/kimlik açılmadan önce IDOR + AuthZ + rate-limit testleri yeşil.

## 3. Performans & gözlemlenebilirlik (`10`, `12`)

- ⚡ Yeni dış çağrı (DB/fiyat/LLM) cache'li ve async mı? Cache anahtarı `UserId`
  içeriyor mu? Sorgu projeksiyonlu mu (gereksiz kolon/satır çekmiyor mu)?
- 👁 Yapılandırılmış log var mı; yeni dış bağımlılık için **metrik + fallback**
  eklendi mi? Health check etkilendi mi?

## 4. ADM kayıtları (§11)

Sırayla, yalnız gerçekten değişenleri:

1. **Aktif GD** (`.agent-method/cycles/GD-NNN.md`) — görev listesi ve durum
   güncel; kabul kriterleri **işaretlenmemiş** bırakılır, altına kanıt yazılır.
2. **GitHub Issues** (kanonik backlog — D-022) — kalem GD'ye dönüştüyse
   `adm:gd` etiketi, kapandıysa issue kapanır (`Closes #N` commit mesajında).
   Yeni iş: `gh issue create --type Task|Feature|Bug --milestone "<faz>"
   --label "prio:N,area:X"`. Plan dışı çıkan iş: `IDEAS.md` (ham fikir) /
   `INCIDENTS.md` (planlı iş dışında çıkan sorun + `Bug` issue'su).
   ⚠ `generated/BACKLOG.md` **elle düzenlenmez** (INV-12/INV-17).
3. **Karar** verildiyse `.agent-method/decisions/D-NNN-<slug>.md` (bir karar =
   bir dosya, ID tekil ve yeniden kullanılmaz — INV-09) + **Keywords** bölümü
   dolu (Doctor'ın drift taraması bunu kullanır). Teknik karşılığını ilgili
   kalıcı dokümana da işle (ör. veri modeli → `.claude/docs/03-DATA-MODEL.md`).
4. **Yeni/değişen yetenek** → `.agent-method/registry/CAPABILITIES.md`.
5. Kod değiştiyse `adm-review` gerekip gerekmediğine bak.

Tarihi sistemden al, **mutlak** yaz ("2026-11-22", "yarın" değil).
Tutarsızlık görürsen sessizce düzeltme — bildir ve kanonik kaynağı göster
(AGENTS.md §4.2).

**Kayıt gerekmeyen turlar:** salt soru-cevap, keşif/okuma, hiçbir şeyi
değiştirmeyen turlar. Araç-zinciri bakımı gibi işler aktif GD'nin kapsamına
girmiyorsa oraya yazılmaz — oturum kanıtına bir satır yeter.

## 5. Raporu şu biçimde ver

```
## Yeşil kapı — <iş>
**Testler:** <backend: N geçti / M başarısız> · <web: ...> · <koşulamayan: ...>
**Güvenlik:** <izolasyon · sır · doğrulama · log — her biri ✓ / ⚠ + gerekçe>
**Gözlemlenebilirlik:** <log/metrik/fallback — ✓ / gerekmedi>
**ADM:** <GD · issue(#N) · karar · CAPABILITIES — ne güncellendi>
**Eksik / doğrulanmayan:** <madde madde; yoksa "yok">
**Kabul:** kanıt sunuldu — onay kullanıcıda (AC-N için insan kararı)
```

Eksik kalan bir şey varsa **sakla ve yaz**; "muhtemelen tamam" yazmak bu
projede en pahalı cümledir — yanlış rakam ya da sızan veri geri alınamaz.
