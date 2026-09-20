---
id: D-002
title: Ürün yatırım tavsiyesi vermez (SPK sınırı)
status: proposed
scope: architecture
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-002 — Yatırım tavsiyesi değil

## Context
Türkiye'de yatırım danışmanlığı SPK lisansına tabidir. Ürünün konumu eğitimdir.
(CLAUDE.md §2, 14 §1/§6)

## Decision
Kişiye özel alım-satım yönlendirmesi, gelecek tahmini, enstrüman sıralaması ve
sinyal/formasyon dili üretilmez. Bunun yerine çerçeve, açıklama, geçmişe dönük
senaryo sunulur. Her analiz/yorum yüzeyinde "yatırım tavsiyesi değildir"
çerçevesi görünür. Kural makine testleriyle taranır (M7/M7a/M7b).

## Consequences
- LLM çıktısında yasak kalıp → kart düşer (fallback).
- Ders içeriğinde şirket/fon/aracı kurum adı yok; geniş endeks adı ölçüt olarak
  izinli (K3, onay bekliyor).
- Davranış aynası ve bildirimler tasarlanırken hukuk görüşü alınır.

## Keywords (for Doctor drift detection)
- Disclaimer
- CommentaryOutputGuard
- tavsiye
- hedef fiyat
- sinyal
- formasyon
