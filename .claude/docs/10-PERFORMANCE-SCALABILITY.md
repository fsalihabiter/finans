# 10 — Performans, Ölçeklenebilirlik & Maliyet

> Hedef: çok kullanıcıyla **hızlı veri çekme + hızlı hesap**, düşük maliyetle.
> Barındırma kararı: **self-hosted / VPS + Docker** (açık kaynak yığın). Bu
> doküman performans bütçelerini, önbellekleme katmanlarını, ölçeklenme desenini
> ve maliyet kontrolünü tanımlar. İlgili: `02` (mimari), `11` (güvenlik),
> `12` (gözlemlenebilirlik).

---

> 🌐 **Web istemciye özel performans** (kod bölme, bundle bütçesi, Web Vitals):
> [`13`](13-WEB-FRONTEND.md) §7.

## 1. İlkeler

1. **Hesap hızlıdır, dış çağrı yavaştır.** Sayısal hesap zaten in-process,
   deterministik ve `decimal` (`02` §1). Darboğaz **dış çağrı** (DB, fiyat API,
   LLM) ve **N+1 sorgular**dır → orayı optimize et, önbelleğe al.
2. **Önce ölç, sonra optimize et.** Performans bütçeleri (§2) + `12`'deki
   metriklerle kanıtla. Erken mikro-optimizasyon yok.
3. **Stateless API → yatay ölçeklenme.** Tüm durum Postgres + Redis'te; uygulama
   örneği kopyalanabilir (§5).
4. **Cache, hem hızın hem maliyetin anahtarı.** Dış API ve LLM çağrısını
   azaltmak hem gecikmeyi hem faturayı düşürür (§3, §7).

---

## 2. Performans Bütçeleri (SLO — başlangıç hedefleri)

| Yol | Hedef (p95) | Not |
|-----|-------------|-----|
| `GET /api/portfolio/summary` (cache isabet) | < 150 ms | Sıcak cache'ten |
| `GET /api/portfolio/summary` (cache ıska, yeniden hesap) | < 500 ms | DB + hesap |
| `GET /api/holdings` (sayfalı) | < 300 ms | AsNoTracking + projeksiyon |
| Hesap fonksiyonları (saf, in-process) | < 5 ms | 1 portföy, ~50 kalem |
| `GET /api/portfolio/commentary` (cache isabet) | < 200 ms | LLM cache'ten |
| `GET /api/portfolio/commentary` (LLM çağrısı) | < 4 s | nadiren; cache'lenir |

> Bu hedefler `12`'de Prometheus histogramlarıyla izlenir; ihlal = alarm.

---

## 3. Önbellekleme Katmanları (Redis + in-memory)

| Katman | Ne | TTL / Geçersizleştirme | Faz |
|--------|----|------------------------|-----|
| **Fiyat cache** | Altın/döviz/hisse fiyatı | 5-15 dk TTL | 2 |
| **FX kuru cache** | Kur çiftleri | 5-15 dk TTL | 2 |
| **Varlık kataloğu** | `Assets` referans verisi (nadiren değişir) | in-memory, uzun TTL | 1 |
| **Portföy özeti** | hesaplanmış summary | portföy hash'i değişince geçersiz | 1-2 |
| **LLM yorumu** | commentary kartları | portföy hash / günde 1 (`07` §6) | 3 |

- **Araç:** Faz 1 in-memory (`IMemoryCache`); Faz 2+ **Redis** (dağıtık —
  birden çok API örneği aynı cache'i paylaşsın, §5 için şart).
- **Cache anahtarı:** kullanıcı + girdi hash'i. **Kullanıcılar arası cache
  sızıntısı olmamalı** (anahtar mutlaka `UserId` içerir — `11` ile ilişkili).
- **Stampede koruması:** popüler anahtarda eşzamanlı yeniden-hesabı tek
  çalıştır (lock/`GetOrCreateAsync`).

---

## 4. Veritabanı Performansı (EF Core + PostgreSQL)

- **İndeksler:** `03` §7'deki FK ve `Holdings(UserId,AssetId)` unique indeksi.
  Sorgu planına göre ek indeks (örn. `Transactions(HoldingId, Date)`).
- **N+1'den kaçın:** ilişkili veriyi `Include`/projeksiyon ile tek sorguda al;
  gerekirse `AsSplitQuery()`.
- **Okumada `AsNoTracking()`:** summary/list gibi salt-okuma sorgularında
  değişiklik takibi kapalı → daha hızlı, az bellek.
- **Projeksiyon (`Select` → DTO):** entity'nin tamamını değil, gereken alanı çek.
- **Sayfalama:** liste endpoint'leri `skip/take` (veya keyset) ile sayfalı.
- **Connection pooling:** Npgsql varsayılan havuz; `DbContext` scoped, kısa ömürlü.
- **Yazımda türetilen alan:** `Holdings.Quantity/AvgCost` işlem değişiminde
  yeniden hesaplanıp **saklanır** (`03` §3) → her okumada Transactions'ı taramaya
  gerek kalmaz (okuma yolu hızlanır).
- **Async her yerde:** `async/await`, bloklayan `.Result`/`.Wait()` yasak
  (thread havuzunu tüketir, ölçeklenmeyi öldürür).

---

## 5. Ölçeklenme Deseni (VPS + Docker)

```
                 ┌─────────── VPS (Docker Compose) ───────────┐
   İnternet ──►  │  Reverse Proxy (Traefik/Caddy)             │
   (HTTPS)       │   • TLS (Let's Encrypt) • rate limit        │
                 │        │            │                       │
                 │   API replica 1   API replica N  (stateless)│
                 │        └─────┬──────┘                        │
                 │        ┌─────┴─────┐     ┌──────────┐         │
                 │        │ PostgreSQL │     │  Redis    │        │
                 │        └───────────┘     └──────────┘         │
                 └────────────────────────────────────────────┘
```

- **Önce dikey ölçekle** (VPS'e RAM/CPU ekle) — tek kişilik ekip için en ucuz
  ve en basit. Yetmezse **yatay**: API'yi stateless tut, proxy arkasında
  replika ekle (durum Postgres+Redis'te olduğu için sorunsuz).
- **Stateless şart:** oturum/state bellekte tutulmaz; kimlik **JWT** (stateless,
  `11`); cache **Redis** (paylaşımlı).
- **Reverse proxy:** TLS sonlandırma + **rate limiting** + sıkıştırma (gzip/br)
  + statik içerik. Traefik (Docker etiketleriyle kolay) veya Caddy (otomatik
  HTTPS). Nginx de uygun.
- **Bağlantı sınırları:** Postgres `max_connections` ve API havuzu uyumlu
  ayarlanır (replika sayısı × havuz < DB limiti).

---

## 6. Hesaplama Performansı

- Hesap saf fonksiyon ve in-process → milisaniye düzeyi; **darboğaz değil.**
- Çok kullanıcıda toplam yük: özet **okuma-anında hesap + cache** modeli yeterli
  (her kullanıcı kendi portföyünü ister; ortak iş yok). Materialized/önceden
  hesap **gerekmez** — cache + türetilmiş alanlar yeterli.
- LLM yorumu hesap değildir; cache + tetikleme disiplini ile yönetilir (`07`).

---

## 7. Maliyet Kontrolü

| Kalem | Strateji |
|-------|----------|
| Barındırma | Tek VPS + Docker Compose; açık kaynak (Postgres, Redis, Grafana) → lisans maliyeti yok. Dikey ölçek önce. |
| Dış fiyat API | Cache (5-15 dk) → istek sayısı ve ücretli kademe baskısı düşer. Ücretsiz katman + cache. |
| LLM | En pahalı kalem. Cache (portföy hash/günde 1), uygun model kademesi, prompt cache (`07` §6). Geliştirmede aylık birkaç $. |
| DB | Tek instance; yedek (backup) ucuz disk. Gereksiz büyük sorgudan kaçın (projeksiyon). |
| İzleme | Self-host açık kaynak (`12`) → ek SaaS maliyeti yok; retention sınırıyla disk kontrol. |

> **Maliyet metriği izle:** LLM çağrı sayısı/token, dış API çağrı sayısı,
> cache isabet oranı (`12`). Cache isabeti düşükse hem yavaş hem pahalı demektir.

---

## 8. Yük & Performans Testi (test stratejisine bağlı)

- **Araç:** **k6** (JS, açık kaynak) veya **NBomber** (.NET). 
- **Senaryo:** N eşzamanlı kullanıcı `GET /portfolio/summary` → p95 bütçeyi
  (§2) tutuyor mu; cache ıska/isabet altında davranış.
- Bu testler `09` senaryo kataloğuna **performans senaryosu** olarak eklenir
  (SC-P1, bkz. `09`). CI'da değil, kilometre taşlarında elle koşulur (Faz 2+).

---

## 9. Yapma Listesi

- ❌ Bloklayan I/O (`.Result`, `.Wait()`), senkron dış çağrı.
- ❌ Cache'siz dış API/LLM çağrısı (yavaş + pahalı).
- ❌ Cache anahtarında `UserId` unutmak (sızıntı + yanlış veri).
- ❌ `Include` zinciriyle tüm grafiği çekmek (projeksiyon kullan).
- ❌ Erken yatay ölçek / mikroservis. Önce dikey + cache.
- ❌ Stateful API (bellekte oturum) — yatay ölçeği imkânsız kılar.
