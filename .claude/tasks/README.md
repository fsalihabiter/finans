# Görev Takip Sistemi (`.claude/tasks/`)

> Bu klasör, projeyle yapılan her işin **otomatik** izini tutar. Kullanıcının
> tetiklemesine gerek yoktur; Claude her oturumda protokole uyar ve harness
> oturum başında durumu önüne getirir.

## Parçalar

| Dosya | Rol |
|-------|-----|
| [`ACTIVE.md`](ACTIVE.md) | **Anlık durum.** Şu an çalışılan + sıradaki görevler. Kısa tutulur. |
| [`TASKLOG.md`](TASKLOG.md) | **Kronolojik günlük** (append-only, en yeni üstte). Yapılan her anlamlı iş bir girdi bırakır. |
| [`session-start.mjs`](session-start.mjs) | SessionStart hook scripti. Oturum başında ACTIVE + son TASKLOG girdisini bağlama yazar. |
| `../settings.json` | Hook kaydı (SessionStart → `session-start.mjs`). |
| `../docs/08-BACKLOG.md` | **Plan kaynağı.** Faz/görev (T-ID) durumlarının asıl yeri. Worklog buna referans verir. |

## Nasıl otomatik çalışır? (iki katman)

1. **Oturum başı (harness → otomatik):** SessionStart hook'u
   `session-start.mjs`'i çalıştırır; ACTIVE.md ve son worklog girdisi otomatik
   bağlama düşer. Böylece "nerede kalmıştık?" sorusu sorulmadan cevaplanır.
2. **İş sırasında (Claude → otomatik):** `CLAUDE.md` §11 protokolü gereği Claude,
   anlamlı her iş sonunda:
   - `08-BACKLOG.md`'de ilgili görev durumunu günceller (`[ ]→[~]→[x]`),
   - `TASKLOG.md`'ye bir girdi ekler (en üste),
   - `ACTIVE.md`'yi sıradaki göreve göre tazeler.

## Ne kayıt edilir, ne edilmez?

- ✅ Kayıt: kod değişikliği, yeni doküman, kalıcı karar, şema/endpoint ekleme,
  düzeltme, bağımlılık ekleme.
- ❌ Kayıt yok: salt soru-cevap, keşif/okuma, henüz bir şey değiştirmeyen turlar.

> Kalıcı bir **karar** verildiyse, onu yalnızca worklog'a değil, ilgili kalıcı
> dokümana da işle (örn. veri modeli kararı → `docs/03-DATA-MODEL.md`).

## Hook'u test et

```bash
node .claude/tasks/session-start.mjs
```
Çıktı: aktif görevler + son worklog girdisi. (Hook bunu otomatik yapar; bu komut
sadece elle doğrulama içindir.)
