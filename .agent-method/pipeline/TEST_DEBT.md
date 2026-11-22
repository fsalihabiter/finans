# TEST_DEBT — finding-driven coverage debt (from COMPLETED work)

> Semantics (INV-07 / INV-08):
> - Failing REQUIRED validation is NOT test debt — the GD cannot be completed.
> - Coverage debt: feature correct, acceptance passed, ideal automated coverage
>   missing. GD may complete; the debt is recorded here.

| ID | Source GD | Missing coverage | Risk | Status |
|---|---|---|---|---|
| TD-001 | GD-003, GD-004, GD-005 | **S0-L8 9 + S0-L9 10 + S0-L10 10 figür — toplam 29 figür — render testinde yok.** `web/src/components/LessonFigure.test.tsx` yalnız `InflationSlider`'ı ve bilinmeyen-anahtar fallback'ini kapsıyor. M4 mutabakatı figür anahtarlarını TSX'ten **regex'le** okuduğu için anahtar kayıtlı olsa da bileşen bozuk ya da okunmaz olabilir; seed testleri bu boşluğu göremez. ⚠ **Borç artık kendini kanıtladı:** REVIEW-004'te 1 (RV-039), REVIEW-005'te 3 figür bulgusu (RV-053/054/055) ancak **gözle** yakalandı ve üçü de daha önce kapatılmış desenlerin tekrarıydı. Set 0 kapandığına göre bu ertelenmiş bir kalem değil, ayrı bir iş. | **yüksek** — tekrar oranı ölçüldü: dört bulgunun dördü de otomatik testten kaçtı; bozuk ya da okunmaz figür kullanıcıya eksik ders olarak gider | open |
