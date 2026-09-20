---
id: D-011
title: Gözlemlenebilirlik — Serilog(+Seq), OpenTelemetry/Prometheus, korelasyon, redaksiyon
status: proposed
scope: architecture
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-011 — Gözlemlenebilirlik

## Context
Dış bağımlılıklı finansal servis; sorun izlenebilir olmalı, log'a sır/PII
yazılmamalı. (12, 11 §6)

## Decision
Yapılandırılmış log Serilog ile (Console her zaman, Seq opsiyonel);
`CorrelationIdMiddleware` ile istek korelasyonu;
`SensitiveDataDestructuringPolicy` ile redaksiyon; metrikler OpenTelemetry →
Prometheus `/metrics`; `/health` ve `/health/ready`. Uç bazlı rate limit.

## Consequences
- Yeni dış bağımlılık = metrik + fallback + log kapısı.
- Log'a token/PII yazımı drift işaretidir.

## Keywords (for Doctor drift detection)
- Serilog
- CorrelationIdMiddleware
- SensitiveDataDestructuringPolicy
- AddPrometheusExporter
- health/ready
