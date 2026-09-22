# ADM v2.0-alpha — Kullanım Rehberi

> Bu rehber, ADM (Agent Development Method) v2 alpha template'ini **yeni bir
> projede** ilk kez kullanacak kişi içindir. Ürün ilkesi:
> **Dışarısı basit, içerisi disiplinli.** Günlük kullanımda bilmen gereken
> şey aslında iki komuttur: `adm-open` ve `adm-close`.
>
> Makine yüzeyi (dosya adları, ID'ler, YAML anahtarları) her zaman İngilizcedir;
> agent'ın seninle konuştuğu dil `config.yaml → method.language` ayarından gelir
> (varsayılan: `tr`).

---

## 1. ADM'nin tek işi

```text
Claude ile çalış → adm-close → Codex ile aç → adm-open → kaldığın yerden devam
        (→ Cursor → Antigravity → tekrar Claude ... aynı niyet, aynı kararlar,
         aynı GD'ler, aynı geçmiş — sohbet geçmişi taşınmadan)
```

Bir repository'de aynı anda **tek aktif ADM oturumu** vardır. Paralel yazılabilir
oturumlar, worktree orkestrasyonu, lock'lar alpha kapsamında değildir (roadmap
V2.4). Kendi Git akışın için worktree kullanabilirsin; ancak ADM bunları
yönetmez ve eşzamanlı yazma için doğruluk garantisi vermez.

## 2. Kurulum — yeni projede başlangıç

1. **Template'i kopyala.** Paket içeriğini yeni repo'nun köküne kopyala
   (`examples/` klasörü isteğe bağlıdır).
2. **Git'i doğrula.** Repo bir Git repository olmalı (TFS Git tabanlı repo'lar
   dahil — `start_sha` ve evidence modeli aynen çalışır).
3. **`adm-bootstrap`'ı çalıştır.**
   - Claude Code: `/adm-bootstrap` — Codex: `$adm-bootstrap` veya skill seçici —
     Cursor / Antigravity: `/adm-bootstrap`
   Skill seninle kısa bir mülakat yapar: amaç, hedefler, **kapsam dışı liste**
   (buraya somut kavram/sınıf/modül adları yaz — Doktor drift tespitini bu
   anahtar kelimelerle mekanik olarak yapar), kısıtlar, başarı kriteri.
4. `PROJECT_INTENT.md` metnini **sen onaylarsın**; onaysız kurulum tamam sayılmaz.

## 3. Günlük döngü (Level 1)

```text
adm-open  →  çalış  →  adm-close
```

### `adm-open` — oturum açılışı
- `state.yaml`'ı okur, tutarlılığı kontrol eder, yeni oturum kaydını açar
  (S-NNN, runtime, started_at, start_sha) ve sana kısa bir brifing verir:

```text
Proje: Consumer Portal
Faz: Authentication
Aktif GD: GD-014 — Refresh Token (devam ediyor)
Önceki runtime: claude-code (S-013)
Blocker: yok
Sıradaki adım: refresh-token revocation testlerini tamamla.
```

- `state.yaml`'da kapatılmamış bir oturum görürse (ör. önceki oturum `adm-close`
  yapılmadan bitmişse) bunu **raporlar ve sana sorar**; sessizce üzerine yazmaz.
- Tutarsızlık bulursa **raporlar, düzeltmez** ve `adm-doctor` önerir.

### GD ile çalışmak
- İş birimi GD'dir. Yeni GD açarken `cycles/_GD_TEMPLATE.md` kopyalanır,
  `status: in-progress` yapılır, `state.yaml → active.gd_id` buna işaret eder.
- **Reuse analizi zorunludur:** koda başlamadan `registry/CAPABILITIES.md`
  taranır; Reuse / Extend / Extract / New merdiveni doldurulur (~%70 uyum eşiği,
  forced-abstraction sinyali not edilir).
- Görev kırılımı GD gövdesindeki checklist'te yaşar; state'e taşınmaz.
- **Test pazarlık konusu değildir:** required validation geçmeden GD tamamlanamaz
  (INV-07). Eksik ideal kapsam TEST_DEBT'e yazılır, GD'yi bloklamaz (INV-08).

### `adm-close` — oturum kapanışı ve handoff
- Git kanıtını toplar (`start_sha..HEAD` + staged + unstaged + untracked;
  oturum dışı değişiklikleri sahiplenmez).
- Kalıcı izi yazar: `evidence/sessions/S-NNN.md` (amaç, yapılanlar, artifact'ler,
  validation sonucu, açık işler, **next step**). Runtime handoff'un hafıza
  mekanizması bu dosyalardır.
- `state.yaml`'daki session bloğunu temizler ve net bir "Sıradaki adım" satırı
  bırakır. **Runtime değiştirmek = X'te `adm-close`, Y'de `adm-open`.** Hepsi bu.

## 4. Kalite gerektiğinde (Level 2)

### `adm-review`
- Diff önce sınıflandırılır (UI-only / auth-security / DB migration / karışık);
  reviewer perspektifleri **dinamik** seçilir — sabit "5 agent" yok.
- Runtime paralel destekliyorsa paralel, değilse sıralı; çıktı şeması aynı.
- Bulgular `severity` (critical/high/medium/low) + `evidence`
  (proven/strong/heuristic) ile derecelenir; 0–100 güven puanı yoktur.
- **Bulgu yaşam döngüsü:** her bulgu `open` doğar; `resolved / accepted /
  false-positive` durumuna yalnız senin kararınla geçer (resolution kaydıyla).
  `open` durumda critical bulgu varken GD kapatılamaz.

### `adm-doctor`
- Tek komut: `adm-doctor`. İçeride structural (INVARIANTS.md listesini —
  INV-01..18 — deterministik kontrol) ve semantic (intent drift, karar ihlali,
  scope inflation) katmanları çalışır; bu ayrımı bilmek zorunda değilsin.
- **Asla kendiliğinden düzeltmez**; `evidence/doctor/DOCTOR-*.md` raporu yazar,
  çelişkide hangi dosyanın truth olduğunu (`canonical_source`) söyler, düzeltme
  seçenekleri sunar. Çözülmemiş critical bulgular sonraki `adm-open`'da blocker olur.
- Öneri: faz kapanışından önce ve büyük merge'lerden sonra çalıştır.

## 5. Yönetişim (Level 3 — her gün gerekmez)

- `adm-bootstrap`: yalnız ilk kurulum.
- `adm-phase`: faz aç/kapat/durum; 15 GD eşik uyarısı config'ten okunur.
- `adm-analyze`: mevcut kod tabanını ADM'ye almak; önce `_scan/PROPOSAL/`
  üretir, **onayın olmadan hiçbir şey uygulamaz**.

Level 4 (CLI, hooks, MCP, generated görünümler, paralel çalışma) roadmap'tedir;
alpha bunların hiçbirini gerektirmez.

## 6. Kararlar (D-NNN)

- Bir karar = bir dosya: `decisions/D-NNN-<slug>.md` (düz yapı; `scope`
  frontmatter'dadır: architecture | naming | structure).
- GD'ler `decisions_followed` / `decisions_assumed` alanlarını doldurur;
  kapanışta assumed olanlar ya gerçek D-NNN'e terfi eder ya düşer (senin onayınla).
- Her karardaki **Keywords** bölümü Doktor'un mekanik tespiti için somut isimler içerir.
- Eskiyen karar `archive/` altına gider, `superseded_by` doldurulur. ID'ler asla
  yeniden kullanılmaz.

## 7. Dosya sahipliği — neye elle dokunulur?

| Elle düzenlenir (canonical) | Elle DOKUNULMAZ |
|---|---|
| `charter/`, `cycles/GD-*`, `decisions/*`, `pipeline/*`, `registry/CAPABILITIES.md`, `config.yaml` | `evidence/` (skill'ler yazar), `.claude/skills/` shim'leri |
| `state.yaml` (nadiren; normalde skill'ler günceller) | varsa `generated/` (alpha varsayılanında yoktur) |

Not: Alpha'da `generated/` klasörü ve CURRENT_STATE/BACKLOG/MASTER dosyaları
**yoktur** — brifing doğrudan canonical kaynaklardan üretilir. Backlog =
`pipeline/POOL.md` + GD status'ları; kronoloji = `evidence/sessions/`.

## 8. Doğrulama projesi için kontrol listesi

Yeni projende V2'yi test ederken sonuçları `RUNTIME_TEST_PLAN.md` tablosuna işle:

- [ ] **Test A (Discovery):** Hiç açıklama vermeden "bu projedeki çalışma
      metodunu açıkla" → runtime AGENTS/adapter'ı ve `.agent-method`'u buluyor mu?
- [ ] **Test B (Claude için kritik):** `/adm-open` shim üzerinden canonical
      skill'i gerçekten yüklüyor mu? Yüklemiyorsa fallback (full mirror) kararı
      senindir ve `V2_REFACTOR_REPORT.md`'e yazılır.
- [ ] **Test C (ANA ÜRÜN TESTİ):** Claude → Codex → Cursor → Antigravity →
      Claude zinciri; her devirde `adm-open`, sohbet geçmişi olmadan aktif fazı,
      GD'yi, son işi, kararları ve next step'i doğru buluyor mu? (5 devir = 5 sonuç)
- [ ] **Test D:** Canonical bir referansı elle boz → `adm-open`/`adm-doctor`
      yakalıyor ve truth kaynağını gösteriyor mu?
- [ ] **Test E:** Subagent kapalıyken `adm-review` sıralı tamamlanıyor, şemaya
      uyuyor mu?
- [ ] **Test F:** Session bloğu dolu bırakılmış halde başka runtime ile
      `adm-open` → kapatılmamış oturum raporlanıyor, sessizce ezilmiyor mu?
- [ ] **Test G:** Windows'ta symlink'siz, Bash'siz temel döngü çalışıyor mu?
- [ ] Brifing/özetler Türkçe, dosya/ID yüzeyi İngilizce mi?
- [ ] 5 dakikada temel workflow anlaşılıyor mu; günlük hayat sadece
      `adm-open`/`adm-close` ile yürüyor mu? (Self-audit 1-2)

Sorun bulduğunda `pipeline/INCIDENTS.md`'e yaz; metot değişikliği gerekiyorsa
yeni bir D-NNN olarak kaydet — V2 iterasyonu da kendi disipliniyle ilerlesin.

## 9. Sık sorulanlar

**Oturumu kapatmadan bilgisayarı kapattım.** Sonraki `adm-open` dolu session
bloğunu görür ve sorar: önce eski oturumu kanıt dosyasına dönüştürüp kapatmayı
önerir. Sessizce üzerine yazmaz (INV-13).

**BACKLOG/MASTER/CHANGELOG nerede?** Alpha'da üretilmiyor. Backlog =
`pipeline/POOL.md` + GD status'ları; kronoloji = session evidence dosyaları.
Deterministik generated görünümler CLI ile (V2.2) gelecek.

**Aynı anda iki GD'de iki agent çalıştırabilir miyim?** Alpha'da hayır — bir
repo'da tek aktif oturum. Paralel çalışma V2.4 roadmap'indedir ve ancak gerçek
ihtiyaç kanıtlanırsa tasarlanacak.

**Eski komutlar (`/ac`, `/kapat`)?** Canonical adlar `adm-*`'dır. İstersen kendi
runtime'ında alias tanımlayabilirsin; template bunları taşımaz.
