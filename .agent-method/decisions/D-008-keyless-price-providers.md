---
id: D-008
title: Fiyat sağlayıcıları anahtarsız ve arayüz arkasında (Frankfurter + Truncgil)
status: proposed
scope: architecture
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-008 — Fiyat kaynakları

## Context
Bütçe sıfıra yakın; repoda sır bulunmamalı; gayriresmi kaynak riski var.
(02 §2.3 T2.1)

## Decision
Döviz için Frankfurter (ECB), gram altın için Truncgil — ikisi de anahtarsız.
Tümü `IPriceProvider` arkasında; `PriceFetchService` cache + son bilinen fiyata
fallback uygular. Yeni sağlayıcı aynı arayüze bağlanır.

## Consequences
- Kaynak değişimi tek noktadan yapılır; uygulama dış kesintide çökmez.
- Sağlayıcı sağlığı metriklerle izlenir; sıralı fallback zinciri ileride (C5).

## Keywords (for Doctor drift detection)
- IPriceProvider
- FrankfurterPriceProvider
- TruncgilGoldPriceProvider
- PriceFetchService
