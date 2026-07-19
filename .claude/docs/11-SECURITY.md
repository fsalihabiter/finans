# 11 — Güvenlik Mimarisi (Security)

> Bu bir **finansal veri** uygulaması: kullanıcının varlık/portföy bilgisi
> kötü niyetli kişilerin eline geçmemeli. Hedef: **derinlemesine savunma** —
> tek bir kontrol değil, katman katman. Bu doküman tehdit modelini, kimlik &
> yetki, veri koruması, sırlar, ağ, bağımlılık ve güvenlik testlerini tanımlar.
> İlgili NFR: NFR-2 (SPK), NFR-3 (KVKK), NFR-4 (güvenlik). İlgili: `02`, `04`,
> `10`, `12`.

> **Mutlak kural (çok kullanıcı):** Her veri erişimi **kullanıcı kimliğine
> göre kapsanır.** Bir kullanıcı asla başkasının portföyünü göremez/değiştiremez.
> Bu, finansal API'lerin #1 açığıdır (OWASP API1: BOLA/IDOR) — §3.

> 🌐 **Web istemciye özel güvenlik** (XSS, token saklama, CSP): [`13`](13-WEB-FRONTEND.md) §6.

---

## 1. Tehdit Modeli (STRIDE-lite)

**Korunan varlıklar:** kullanıcı kimliği/oturumu, portföy & işlem verisi, dış
API/LLM anahtarları, veritabanı.

| Tehdit | Örnek | Karşı kontrol |
|--------|-------|---------------|
| **Spoofing** (kimlik taklidi) | Çalınan token | Kısa ömürlü JWT + refresh, HTTPS, güçlü parola hash (§2) |
| **Tampering** (veri kurcalama) | İstek gövdesini değiştirme | Sunucu tarafı doğrulama, yetki kontrolü, parametreli sorgu |
| **Repudiation** (inkâr) | "Ben yapmadım" | Audit log (`12` §7) |
| **Information disclosure** (sızıntı) | Başkasının portföyü, stack trace, log'da sır | Per-user kapsam (§3), hata maskeleme (`04`), log redaksiyonu (`12`) |
| **Denial of Service** | İstek baskını, brute-force | Rate limit (§5), reverse proxy, lockout |
| **Elevation of privilege** | Normal kullanıcı admin işlemi | Rol bazlı yetki (§3), admin endpoint izolasyonu |

**Saldırı yüzeyleri:** mobil↔API (HTTPS), API↔DB, API↔dış servis (fiyat/LLM),
sırların durduğu yer, container imajı, bağımlılıklar.

---

## 2. Kimlik Doğrulama (AuthN)

> Faz 1-4: tekil yerel kullanıcı (kimlik yok) — ama **kod baştan UserId-kapsamlı**
> yazılır (§3). Gerçek kimlik **Faz 5**'te; tasarım şimdiden hazır.

- **Mekanizma:** JWT (kısa ömürlü **access** ~15 dk + **refresh** token).
  Stateless → yatay ölçeklenmeyle uyumlu (`10` §5).
- **Parola saklama:** **Argon2id** (karar netleşti 2026-07-19: OWASP birinci
  tercih, asgari **m=19 MiB / t=2 / p=1**; ASP.NET Identity varsayılan
  PBKDF2'si OWASP eşiğinin altında → alternatif DEĞİL). **Asla** düz/zayıf
  hash. Salt otomatik. Hash fonksiyonu birim testli (NFR-1 disiplini).
- **Refresh token:** sunucuda **hash'i** saklanır/iptal edilebilir, her
  kullanımda **rotasyon** + iptal edilmiş token yeniden gelirse **aile iptali**
  (çalınma tespiti) + mutlak ömür sınırı (IETF browser-based-apps BCP şartı).
- **Hazır yapı NOTU (2026-07-19):** `MapIdentityApi` **kullanılmaz** — refresh
  rotasyonu yok (dotnet/aspnetcore#52815, .NET 11 öncesi gelmiyor) ve Microsoft
  kendisi "basit senaryolar" ile sınırlıyor. Mevcut `Users`/`RefreshTokens`/
  `Roles` şeması (`03` §B) + `ICurrentUser` takası üzerine kendin-yap.
  Harici IdP (Keycloak/Zitadel) ve SaaS (Auth0/Clerk) değerlendirildi ve
  reddedildi (tek-VPS yükü / KVKK m.9 yurt dışı aktarım) — analiz: TASKLOG
  2026-07-19. Sosyal giriş gerekirse yükseltme yolu: OpenIddict (in-app).
- **Brute-force:** başarısız giriş sayacı + geçici lockout + rate limit (§5).
- **Token taşıma (web) — KARAR 2026-07-19, cookie/BFF:** tarayıcıda token
  **JavaScript'in erişebildiği hiçbir yerde durmaz** (localStorage/
  sessionStorage/memory YASAK — IETF BCP + MS Learn aynı yönde). JWT'ler
  **`httpOnly + Secure + SameSite` cookie** ile taşınır + **CSRF koruması**
  (antiforgery veya SameSite=Strict + özel header şartı). Caddy SPA'yı ve
  `/api`'yi aynı origin'den sunduğu için ayrı BFF süreci GEREKMEZ — desenin
  kazanımı bedavaya gelir (CORS yok).
- **Token taşıma (mobil):** aynı endpoint'lerden `Authorization: Bearer` +
  **güvenli depo** (`expo-secure-store` / Keychain / Keystore) —
  `AsyncStorage`'a token koyma.

---

## 3. Yetkilendirme (AuthZ) — EN KRİTİK

- **Per-user veri izolasyonu (zorunlu):** Her sorgu `WHERE UserId = currentUser`
  ile kapsanır. Endpoint `{id}` alsa bile, kayıt **mevcut kullanıcıya ait
  değilse 404** (varlığı bile sızdırma). Bu kontrol **her** holding/transaction/
  detail/commentary yolunda olmalı.
- **Merkezîleştir:** Kullanıcı kapsamı tek yerden (örn. sorgu filtresi / base
  repository / EF Core global query filter) uygulanır ki unutulmasın.
- **IDOR/BOLA testi zorunlu:** "Kullanıcı A, B'nin holding id'siyle istek atar →
  404" senaryosu (`09` SC-13). Bu test olmadan çok-kullanıcı kapısı açılmaz.
- **Roller:** `User` ve (ileride) `Admin`. Admin endpoint'leri ayrı yetki +
  ayrı audit. Admin izleme yüzeyi `12`'deki Grafana (uygulama içi admin paneli
  değil — saldırı yüzeyini küçük tut).

---

## 4. Girdi Doğrulama & Çıktı Güvenliği

- **Sunucu tarafı doğrulama (her zaman):** miktar > 0, geçerli para birimi
  (allow-list), tarih makul, sembol biçimi. İstemci doğrulaması güvenlik değil,
  UX'tir.
- **SQL Injection:** EF Core parametreli sorgu kullanır → ham SQL'den kaçın;
  zorunluysa parametre bağla, string birleştirme yok.
- **Kütle atama (mass assignment):** entity'yi doğrudan bind etme; **DTO** al,
  izinli alanları map'le (`04`).
- **Hata maskeleme:** istemciye stack trace/iç detay **yok** → sözleşmeli hata
  (`04` §2), iç detay yalnızca log'da (`12`).
- **LLM çıktı güvenliği:** prompt injection ve yasaklı yönlendirmeye karşı
  çıktı filtresi (`07` §7) + LLM'e asla sır/başka kullanıcı verisi verme.

---

## 5. Ağ & Taşıma Güvenliği

- **TLS her yerde:** reverse proxy'de Let's Encrypt (Traefik/Caddy otomatik).
  **HSTS** açık. HTTP→HTTPS yönlendirme.
- **Güvenlik başlıkları:** `Strict-Transport-Security`, `X-Content-Type-Options:
  nosniff`, `X-Frame-Options/CSP frame-ancestors`, `Referrer-Policy`. Proxy
  veya ASP.NET middleware ile.
- **CORS:** allow-list (yalnızca mobil/bilinen origin); `*` yok.
- **Rate limiting:** ASP.NET Core rate limiter + proxy seviyesi. Giriş ve
  pahalı endpoint'lerde (commentary) daha sıkı. DoS + brute-force azaltır (`10`§5).
- **İç servisler dışarı kapalı:** Postgres/Redis/Seq/Grafana yalnızca Docker
  ağında; VPS firewall yalnızca 80/443 (ve yönetim için kısıtlı SSH).

---

## 6. Sır (Secret) Yönetimi

- **Repoda sır yok.** API anahtarı, DB parolası, JWT imza anahtarı, LLM anahtarı
  → ortam değişkeni / Docker secrets / `.env` (gitignore'da).
- **Dev:** .NET User Secrets. **Prod (VPS):** Docker secrets veya kısıtlı izinli
  `.env` (640, app kullanıcısı). İstersen **SOPS + age** ile repoda *şifreli*
  config (anahtar repoda değil).
- **Rotasyon:** JWT imza anahtarı, DB parolası, dış anahtarlar dönemsel
  değiştirilebilir olmalı (env'den okunur, koda gömülü değil).
- **Sızıntı taraması:** commit'lerde sır taraması (gitleaks) — CI'da (Faz 2+).

---

## 7. Veri Koruması & KVKK (NFR-3)

- **At-rest şifreleme:** VPS disk şifreleme + Postgres veri dizini şifreli disk.
  Yedekler (backup) **şifreli** saklanır.
- **Hassas alan:** Gerekirse seçili alanlar uygulama düzeyinde şifrelenir;
  e-posta gibi PII minimumda tutulur (veri minimizasyonu).
- **KVKK hakları (Faz 5, kimlikle):** veriye erişim, **silme** ("verimi sil"),
  taşınabilirlik. Tasarım bunları destekleyecek (kullanıcıya bağlı kayıtlar
  cascade silinebilir).
- **Saklama (retention):** gereksiz veriyi tutma; log'da PII tutma (`12`).
- **İhlal müdahalesi:** tespit (alarm `12` §6) + bildirim planı (Faz 5, hukuki).
- **Loglama:** sır/token/PII **asla** log'a yazılmaz (redaksiyon, `12` §3).

---

## 8. Bağımlılık & Container Güvenliği

- **Bağımlılık taraması:** `dotnet list package --vulnerable`, `npm audit`,
  Renovate/Dependabot ile güncel tut. Bilinen açıklı paket = blok.
- **Container:** minimal temel imaj (distroless/alpine), **non-root** kullanıcı,
  read-only dosya sistemi mümkünse, imajda sır yok. **Trivy** ile imaj taraması.
- **En az ayrıcalık:** her container yalnızca ihtiyacı olan ağ/volume'a erişir.

---

## 9. Güvenlik Testleri (test stratejisine bağlı, `09`)

| Test | Senaryo | Seviye |
|------|---------|--------|
| **IDOR/BOLA** | Kullanıcı A, B'nin kaydını ister → 404 (SC-13) | Integration |
| **AuthZ** | Token'sız/expired token → 401 | Integration |
| **Rate limit** | Eşik üstü istek → 429 (SC-14) | Integration |
| **Girdi doğrulama** | Geçersiz/aşırı girdi → 400, çökme yok (SC-07) | Integration |
| **Hata maskeleme** | Hata yanıtında stack trace yok | Integration |
| **Bağımlılık** | `dotnet list package --vulnerable` / `npm audit` temiz | CI/elle |
| **Statik analiz** | `/security-review` skill'i (PR öncesi) | Elle |

> **Kapı:** Çok-kullanıcı (kimlik) devreye girmeden önce IDOR, AuthZ ve rate
> limit testleri **yeşil** olmalı.

---

## 10. Güvenlik Kontrol Listesi (her endpoint / PR)

- [ ] İstek **kimlik doğrulanmış** mı (Faz 5+)?
- [ ] Veri erişimi **UserId ile kapsanmış** mı? (başkasının verisi → 404)
- [ ] Girdi **sunucuda doğrulanıyor** mu (allow-list)?
- [ ] DTO kullanılıyor mu (mass assignment yok)?
- [ ] Hata yanıtı **iç detay sızdırmıyor** mu?
- [ ] Log'a **sır/PII yazılmıyor** mu?
- [ ] Yeni sır **env/secret**'ta mı (repoda değil)?
- [ ] Pahalı/giriş endpoint'i **rate-limited** mi?
