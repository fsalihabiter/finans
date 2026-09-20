---
id: D-018
title: Yorum metni deterministik üretilir; LLM opsiyonel ve varsayılan kapalı
status: accepted
scope: architecture
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-018 — Deterministik yorum çekirdeği; LLM isteğe bağlı zenginleştirme

## Context

Ürün sahibi kısıtı (2026-09-20): **ücretli API anahtarı kullanılmayacak** ve
üretim self-hosted kalacak (D-014: tek VPS + Docker, GPU yok). Bu kısıt altında
yerel açık ağırlıklı modeller SPIKE-001 ile **gerçek yorum hattında** ölçüldü
(`LocalModelTrial.cs`; aynı prompt, aynı şema, aynı parse, aynı guard'lar).

Ölçüm (hepsi CPU üzerinde — makinedeki RTX 3060, sürücü 512.89 eski olduğu için
Ollama'ya hiç görünmedi; `total_vram="0 B"`. Bu istemsizce **hedef dağıtımın**
(GPU'suz VPS) temsilî ölçümü oldu):

| Model | Şema tutturma | Guard düşürdü | Süre |
|---|---|---:|---:|
| `qwen3:0.6b` | 1/1 | 0 | 374 sn |
| `qwen3:8b` | 0/2 | 0 | 600+ sn (zaman aşımı) |
| `gemma3:12b` | 0/2 | 0 | 600-905 sn |
| `gemma3:4b` | 1/2 | 0 | 281-586 sn |

Üç bulgu:

1. **Süre bütçeyi karşılamıyor.** En iyi başarılı koşu 281 sn; ürün bütçesi
   150 sn (`LlmOptions.TimeoutSeconds`). GPU'suz üretimde her soğuk üretim
   fallback'e düşer → "LLM var ama pratikte hiç çalışmıyor".
2. **Güvenilirlik, hızdan büyük sorun.** Şema tutturma ~%50. Daha kritiği:
   `qwen3:0.6b` **doğru oranı yanlış varlıklara bağladı** ("%86 = BES + Altın";
   doğrusu Altın + Döviz). Guard'lar bunu yakalamadı — **yakalayamaz** da, çünkü
   yönlendirme/tahmin/dil kalıbı tarıyorlar, olgusal atıf denetlemiyorlar.
   Bu üründe LLM'in asıl riski SPK dili değil, **sessizce yanlış söylemek**.
3. **Guard hattı temiz çalıştı.** Hiçbir koşuda yönlendirme/tahmin kalıbı
   üretilmedi; kuşak-1 prompt korkuluğu iş görüyor. Ama bu, (1) ve (2)'yi
   telafi etmiyor.

Kısıt üçgeni: *para ödememe · kendi sunucunda çalıştırma · LLM kalitesi* —
üçü aynı anda sağlanamıyor. Ölçüm ilk ikisini korumayı seçtirdi.

## Decision

**Kullanıcıya gösterilen yorum metninin çekirdeği deterministik üretilir.**

- Yorum kartları kural + şablon ile kurulur: koşul kodda değerlendirilir, metin
  repoda yazılıdır, boşluklar (`{oran}`, `{ad1}`, …) **hesaplanmış** değerlerle
  dolar. Desen yeni değil — `NudgeRuleEngine` bunu zaten yapıyor; yorum tarafı
  aynı desene taşınır.
- **LLM opsiyonel zenginleştirmedir ve varsayılan kapalıdır.** `ILlmClient`
  soyutlaması, sağlayıcı dalları ve guard hattı **kaldırılmaz** (D-009 geçerli
  kalır); yapılandırılmadığında `NoopLlmClient` devrede olur ve ürün eksiksiz
  çalışır.
- LLM açıksa çıktısı **deterministik çekirdeğin yerine değil, üstüne** gelir:
  LLM başarısız olursa kullanıcı düşük kaliteli bir şey değil, **tam** bir yorum
  görür (bugünkü "Yorum şu an üretilemedi" fallback'i bu yüzden yeterli değil).
- Ücretli API anahtarı **varsayılan yol değildir**. Deneme/karşılaştırma için
  yapılandırılabilir kalır.

## Consequences

- **Maliyet sıfır, gecikme yok, dış bağımlılık yok.** Kota/kesinti riski ortadan
  kalkar (`14` §2'deki "veri kırılganlığı" boşluğu bu yüzeyde kapanır).
- **KVKK:** portföy verisi hiçbir üçüncü tarafa gitmez; aydınlatma yükü azalır.
- **SPK:** üretilen her cümlenin kaynağı repoda yazılı bir şablondur → incelemede
  gösterilebilir. D-002'nin uygulanması kalıp taramasından **kaynak denetimine**
  yükselir.
- **Test:** yorum çıktısı deterministik olduğu için birim testle kilitlenir
  (D-015). Bugün mümkün olmayan "bu portföyde şu kart çıkar" testi mümkün olur.
- **Bedel: dil çeşitliliği.** Aynı durumdaki iki kullanıcı aynı cümleyi görür.
  Kural sayısı ve cümle varyantlarıyla yumuşatılır; LLM doğallığına ulaşmaz.
  Bu kabul edilen bedeldir.
- Eğitim modülündeki **T6.14 (LLM ders yorumu)** bu kararla isteğe bağlı hale
  gelir; önceliği düşer.
- Yeniden değerlendirme kapısı: donanım (GPU'lu barındırma) veya küçük model
  kalitesi anlamlı biçimde değişirse SPIKE tekrarlanır. Ölçüm düzeneği
  (`LocalModelTrial.cs`) bu yüzden repoda kalır.

## Keywords (for Doctor drift detection)
- NudgeRuleEngine
- NoopLlmClient
- ILlmClient
- LocalModelTrial
- deterministik şablon
