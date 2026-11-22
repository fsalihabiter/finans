# TEST_DEBT — finding-driven coverage debt (from COMPLETED work)

> Semantics (INV-07 / INV-08):
> - Failing REQUIRED validation is NOT test debt — the GD cannot be completed.
> - Coverage debt: feature correct, acceptance passed, ideal automated coverage
>   missing. GD may complete; the debt is recorded here.

| ID | Source GD | Missing coverage | Risk | Status |
|---|---|---|---|---|
| TD-001 | GD-003 | **S0-L8'in 9 figürü render testinde yok.** `web/src/components/LessonFigure.test.tsx` yalnız `InflationSlider`'ı ve bilinmeyen-anahtar fallback'ini kapsıyor. M4 mutabakatı figür anahtarlarını TSX'ten **regex'le** okuduğu için anahtar kayıtlı olsa da bileşen bozuk olabilir; seed testleri bu boşluğu göremez. | orta — bozuk figür sessizce boş render eder, kullanıcı eksik bir ders görür; parasal/güvenlik etkisi yok | open |
