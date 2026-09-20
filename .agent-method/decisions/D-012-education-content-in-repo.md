---
id: D-012
title: Ders içeriği repoda kodla seed edilir; render kütüphanesiz ve XSS-güvenli
status: proposed
scope: structure
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-012 — Eğitim içeriği ve render

## Context
İçerik topluluk katkısına açılacak (14 §4-D2); render edilen metin kullanıcıya
gider, XSS riski taşır; figürler tema uyumlu olmalı. (15, 16)

## Decision
Ders içeriği `EducationContent.cs` içinde, `SeedData`'dan ayrı tutulur ve kendi
idempotent kapılarıyla **mutabakat** ederek canlı DB'ye iner. Web tarafında
`MiniMarkdown` kullanılır: `dangerouslySetInnerHTML` yok, bağlantı şeması beyaz
listeli (`safeHref`), tablo yalnız hizalama satırıyla. Figürler elle yazılmış
SVG'dir (`LessonFigure.tsx` kayıt defteri); seed anahtarları ile kayıt defteri
testle mutabıktır (M4).

## Consequences
- Yeni ders = içerik + figür + 9 soru/3 zorluk + kaynak bloğu + yapısal testler.
- Üçüncü parti markdown/grafik kütüphanesi getirmek bu kararı bozar.

## Keywords (for Doctor drift detection)
- EducationContent
- MiniMarkdown
- safeHref
- LessonFigure
- dangerouslySetInnerHTML
