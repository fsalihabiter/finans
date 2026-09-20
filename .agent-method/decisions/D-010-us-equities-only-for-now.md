---
id: D-010
title: Hisse verisi şimdilik yalnız ABD (Finnhub + Yahoo); BIST ertelendi
status: proposed
scope: architecture
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-010 — Hisse veri kaynağı

## Context
Güvenilir BIST verisi büyük ölçüde ücretli. (CLAUDE.md §3.3, T4.1)

## Decision
Temel metrikler Finnhub (ABD), fiyat geçmişi Yahoo üzerinden
`IStockDataProvider` ve `IStockHistoryProvider` arkasında alınır. Anahtar yoksa
`NotConfiguredStockDataProvider` devreye girer. BIST ileri faza bırakıldı.

## Consequences
- Hisse modülü TR kullanıcısı için bugün kısmi kapsam sunar.
- BIST kararı maliyet değerlendirmesiyle yeniden açılacak (açık soru).

## Keywords (for Doctor drift detection)
- FinnhubStockDataProvider
- YahooStockHistoryProvider
- IStockDataProvider
- BIST
