# 12 — Gözlemlenebilirlik: Loglama & Monitoring

> Admin olarak sistemi izlemek, sorun ve güvenlik olaylarını erken tespit etmek
> için. Üç ayak: **loglar, metrikler, izler (traces)** + **health check** +
> **audit log** + **alarm**. Yığın (karar): açık kaynak, self-host — **Serilog +
> Seq** (log), **OpenTelemetry + Prometheus + Grafana** (metrik/trace),
> **ASP.NET HealthChecks**. İlgili: `02`, `10`, `11`.

---

> 🌐 **Web istemciye özel izleme** (hata boundary, Web Vitals): [`13`](13-WEB-FRONTEND.md) §8.

## 1. Neden & İlkeler

- **Göremediğini yönetemezsin.** Çok kullanıcıda sorun/yavaşlık/saldırı ancak
  ölçülürse fark edilir.
- **Baştan kur (Faz 0-1):** yapılandırılmış log + health check ilk günden;
  metrik/trace/dashboard trafikle birlikte (Faz 2+) büyür.
- **Maliyetsiz:** tümü açık kaynak, VPS'te Docker ile; retention sınırıyla disk
  kontrol (`10` §7).
- **Güvenli gözlem:** log/metrik'e **asla sır/PII/token** sızmaz (`11` §7).

---

## 2. Mimari (Docker Compose servisleri)

```
  ASP.NET API  ──(Serilog)──►  Seq            (yapılandırılmış log arama)
       │
       └──(OpenTelemetry)──►  Prometheus  ──►  Grafana   (metrik + dashboard + alarm)
                             (+ Tempo/Jaeger, opsiyonel: trace)

  Grafana Alerting / Alertmanager ──►  e-posta / Telegram  (admin uyarı)
  /health, /health/ready  ◄── reverse proxy / uptime kontrol
```
Tümü API ile aynı Docker ağında; **dışarıya kapalı**, yalnızca admin erişimi
(SSH tüneli / kısıtlı IP) — `11` §5.

---

## 3. Loglama (Serilog → Seq)

- **Yapılandırılmış (structured) JSON log.** Düz metin değil → aranabilir,
  filtrelenebilir.
- **Sink'ler:** Console (dev/Docker stdout) + **Seq** (arama UI). 
- **Korelasyon:** her isteğe **CorrelationId / TraceId** (enricher) → bir
  isteğin tüm log'larını izleyebilmek.
- **Seviyeler:** `Debug` (dev) / `Information` (normal akış) / `Warning`
  (fallback tetiklendi) / `Error` (beklenmeyen) / `Fatal`.
- **Bağlam zenginleştirme:** `UserId` (PII değil, opak id), endpoint, süre,
  sonuç kodu, dış servis adı.
- **REDAKSİYON (zorunlu):** parola, token, API anahtarı, e-posta, ham finansal
  PII **log'a yazılmaz**. Hassas alanlar maskeleme ile (`***`). Bunu merkezî bir
  Serilog destructuring politikası ile zorla.
- **Ne logla:** istek başlangıç/bitiş + süre + durum; dış servis çağrısı +
  sonuç (fiyat/LLM); fallback tetikleri; hatalar (stack iç tarafta, istemciye
  değil — `11` §4).

---

## 4. Metrikler (OpenTelemetry → Prometheus)

**RED + kaynak + iş + maliyet metrikleri:**

| Grup | Metrik |
|------|--------|
| **RED (istek)** | İstek hızı, hata oranı, gecikme (p50/p95/p99 histogram) — endpoint başına |
| **Bağımlılık** | Dış fiyat API / LLM çağrı sayısı, gecikmesi, hata/fallback sayısı |
| **Cache** | İsabet/ıska oranı (düşükse hem yavaş hem pahalı — `10` §7) |
| **DB** | Sorgu süresi, aktif bağlantı, havuz doygunluğu |
| **Hesap** | Hesap süresi (bütçe `10` §2 ihlali alarmı) |
| **Maliyet** | LLM çağrı sayısı/token (fatura takibi `10` §7) |
| **Güvenlik** | Başarısız giriş, 401/403, 429 (rate-limit) sayısı — anomali = saldırı işareti |
| **Sistem** | CPU, RAM, disk, container sağlığı (node/cAdvisor exporter) |

- **OpenTelemetry** .NET otomatik enstrümantasyonu (ASP.NET, HttpClient, EF Core)
  → Prometheus'a aktarım. Özel metrikler (cache, LLM maliyet) elle eklenir.

---

## 5. İzleme / Trace (opsiyonel, Faz 2+)

- **OpenTelemetry traces** → Tempo veya Jaeger. Bir isteğin uçtan uca yolu:
  API → DB → fiyat API → LLM, her adımın süresi.
- Yavaş istekte darboğazı **görünür** kılar (hangi dış çağrı?).
- Faz 1'de şart değil; log korelasyonu yeterli. Trafik artınca aç.

---

## 6. Dashboard & Alarm (Grafana)

**Dashboard'lar (admin görünümü):**
1. **API Sağlığı (RED):** hız/hata/gecikme, endpoint kırılımı.
2. **Bağımlılıklar:** fiyat API & LLM gecikme/hata/fallback; cache isabeti.
3. **İş:** aktif kullanıcı, portföy/işlem sayısı, LLM çağrı/maliyet.
4. **Güvenlik:** başarısız giriş, 401/403/429 eğilimi, anomali.
5. **Sistem:** CPU/RAM/disk/container.

**Alarm kuralları (→ e-posta/Telegram):**
- Hata oranı eşik üstü (örn. 5xx > %2 / 5 dk).
- p95 gecikme bütçe ihlali (`10` §2).
- Dış servis (fiyat/LLM/DB) erişilemiyor / fallback patlaması.
- **Güvenlik:** başarısız giriş / 401 / 429 ani artışı (brute-force/saldırı).
- Sistem: disk %85, RAM/CPU sürekli yüksek, container down, `/health` kırmızı.

---

## 7. Audit Log (güvenlik & inkâr-edilemezlik)

- **Ayrı, değiştirilemez akış:** hassas/önemli eylemler — giriş, çıkış, parola
  değişimi, veri silme, admin işlemleri, yetki reddi (403).
- İçerik: kim (UserId), ne, ne zaman (UTC), nereden (IP), sonuç. **PII değil**,
  opak kimlik.
- Normal uygulama log'undan ayrı tutulur (uzun retention, salt-ekleme).
- KVKK ihlal tespiti ve "kim yaptı" sorusu için (`11` §1 Repudiation).

---

## 8. Health Checks

- **`/health` (liveness):** uygulama ayakta mı.
- **`/health/ready` (readiness):** DB + Redis + kritik dış bağımlılık erişilebilir
  mi → trafiğe hazır mı (replika ekleme/proxy için, `10` §5).
- ASP.NET Core `HealthChecks` paketi; reverse proxy / uptime izleyici bunu yoklar.
- Hazır değilse proxy o örneğe trafik göndermez.

---

## 9. Faz Bazlı Kurulum

| Faz | Gözlemlenebilirlik adımı |
|-----|--------------------------|
| 0 | Serilog (Console + dev'de Seq) yapılandırılmış log + CorrelationId; `/health`. |
| 1 | Redaksiyon politikası; temel istek log'u; audit log iskeleti (giriş yokken bile yapı). |
| 2 | Docker Compose'a Seq + Prometheus + Grafana; RED + bağımlılık + cache metrikleri; ilk dashboard + alarmlar; dış servis fallback metriği. |
| 3 | LLM çağrı/maliyet metriği + alarmı. |
| 5 | Audit log tam (kimlikle), güvenlik dashboard'u, trace (Tempo/Jaeger), retention/yedek politikası. |

---

## 10. Yapma Listesi

- ❌ Log'a sır/token/PII yazmak (redaksiyon zorunlu).
- ❌ Düz metin log (yapılandırılmış JSON kullan).
- ❌ İzleme servislerini (Seq/Grafana/Prometheus) internete açık bırakmak.
- ❌ Alarmsız metrik (kimse bakmazsa metrik işe yaramaz).
- ❌ Gözlemlenebilirliği "sonra eklerim" diye ertelemek — log + health Faz 0'da.
