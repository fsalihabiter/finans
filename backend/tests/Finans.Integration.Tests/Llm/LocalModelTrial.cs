using System.Diagnostics;
using System.Text;
using Finans.Application.Llm;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Finans.Infrastructure.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace Finans.Integration.Tests.Llm;

/// <summary>
/// <b>Manuel ölçüm düzeneği — normal test koşusunda ÇALIŞMAZ.</b>
/// Yerel (self-hosted, açık ağırlıklı) bir modelin bu ürünün yorum hattında
/// kullanılabilir olup olmadığını ölçer. Karar girdisi: <c>D-018</c> adayı
/// (bkz. <c>D-009</c> — LLM sağlayıcı soyutlaması).
///
/// <para><b>Neden gerçek hat:</b> aynı <see cref="CommentaryPrompts.SystemPrompt"/>,
/// aynı JSON şema, aynı parse, aynı <see cref="CommentaryOutputGuard"/> /
/// <see cref="CommentaryLanguageGuard"/> ve aynı üretim istemcisi
/// (<see cref="OpenRouterLlmClient"/>, OpenAI-uyumlu → Ollama'nın
/// <c>/v1/chat/completions</c> ucu). Ölçülen şey "model iyi mi" değil,
/// <b>bu üründe iyi mi</b>.</para>
///
/// <para><b>Çalıştırma:</b>
/// <code>
/// FINANS_LLM_TRIAL=1 FINANS_LLM_TRIAL_MODELS=gemma3:12b,qwen3:8b \
///   dotnet test --filter FullyQualifiedName~LocalModelTrial
/// </code>
/// Ortam değişkenleri: <c>FINANS_LLM_TRIAL</c> (kapı) ·
/// <c>FINANS_LLM_TRIAL_MODELS</c> (virgüllü) · <c>FINANS_LLM_TRIAL_BASEURL</c>
/// (vars. <c>http://localhost:11434/</c>) · <c>FINANS_LLM_TRIAL_RUNS</c> (vars. 3) ·
/// <c>FINANS_LLM_TRIAL_LABEL</c> (rapor etiketi, ör. <c>cpu-only</c>).</para>
///
/// <para><b>Çıktı:</b> <c>tmp_diag/llm-trial/</c> (gitignore'da) — model başına
/// markdown rapor + ham kartlar. Guard'dan geçmek "iyi metin" demek DEĞİLDİR;
/// Türkçe akıcılık ve eğitici ton insan değerlendirmesi ister (raporun sonundaki
/// ham kartlar bunun içindir).</para>
///
/// <para>⚠ Kurgusal portföy kullanır; gerçek kullanıcı verisi gitmez.
/// Yerel uçta veri makineden çıkmaz (KVKK artısı, <c>11</c> §KVKK).</para>
/// </summary>
public sealed class LocalModelTrial(ITestOutputHelper output)
{
    private const string GateEnv = "FINANS_LLM_TRIAL";

    [Fact]
    public async Task Measure_local_models_on_the_real_commentary_pipeline()
    {
        if (Environment.GetEnvironmentVariable(GateEnv) != "1")
        {
            output.WriteLine($"{GateEnv}=1 verilmedi — manuel ölçüm atlandı (normal koşuda beklenen davranış).");
            return;
        }

        var baseUrl = Env("FINANS_LLM_TRIAL_BASEURL", "http://localhost:11434/");
        var models = Env("FINANS_LLM_TRIAL_MODELS", "gemma3:12b")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var runs = int.TryParse(Env("FINANS_LLM_TRIAL_RUNS", "3"), out var r) ? Math.Max(1, r) : 3;
        var label = Env("FINANS_LLM_TRIAL_LABEL", "default");

        var (summary, holdings) = SamplePortfolio();
        var report = new StringBuilder();
        report.AppendLine($"# Yerel model denemesi — {label}");
        report.AppendLine();
        report.AppendLine($"- Uç: `{baseUrl}v1/chat/completions` · koşu/model: {runs} · tarih: {DateTime.UtcNow:yyyy-MM-dd HH:mm}Z");
        report.AppendLine($"- Hat: gerçek `LlmCommentaryService` (prompt + şema + parse + guard'lar) · istemci `OpenRouterLlmClient`");
        report.AppendLine();
        report.AppendLine("**Örnek portföyün gerçeği (atıf doğruluğu kontrolü için):** " +
            "Altın %57 (366.000) · Döviz %29 (185.850) · BES %12 (78.500) · Nakit %2 (12.000) · " +
            "toplam 642.350 ₺ · nominal getiri +%37,1 · **reel getiri −%0,7** · en büyük iki kalem = " +
            "**Altın + Döviz = %86**. Kartlar bu etiketleri doğru bağlıyor mu? Guard bunu YAKALAMAZ " +
            "(yalnız yönlendirme/tahmin/dil kalıbına bakar) — insan kontrolü gerekir.");
        report.AppendLine();
        report.AppendLine("| Model | Koşu | Kaynak | Kart | Kavramlı kart | Guard düşürdü | Giriş tok. | Çıkış tok. | Süre (sn) |");
        report.AppendLine("|---|---:|---|---:|---:|---:|---:|---:|---:|");

        var samples = new StringBuilder();
        var sampledModels = new HashSet<string>();

        foreach (var model in models)
        {
            for (var run = 1; run <= runs; run++)
            {
                var metrics = new RecordingMetrics();
                var service = BuildService(baseUrl, model, metrics);

                var sw = Stopwatch.StartNew();
                CommentaryResponse response;
                try
                {
                    response = await service.GetCommentaryAsync(summary, holdings);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    report.AppendLine($"| `{model}` | {run} | **hata** | – | – | – | – | – | {sw.Elapsed.TotalSeconds:F1} |");
                    output.WriteLine($"{model} koşu {run}: {ex.GetType().Name} — {ex.Message}");
                    continue;
                }
                sw.Stop();

                var detailed = response.Cards.Count(c => c.Detail is not null);
                report.AppendLine(
                    $"| `{model}` | {run} | {response.Source} | {response.Cards.Count} | {detailed} | " +
                    $"{metrics.GuardBlocked} | {metrics.InputTokens} | {metrics.OutputTokens} | {sw.Elapsed.TotalSeconds:F1} |");

                // Örnek: modelin İLK BAŞARILI koşusu. (Önceden "birinci koşu" idi; o koşu
                // fallback'e düşünce okunacak metin kalmıyordu — ölçümün amacı kaçıyordu.)
                if (response.Source == "llm" && sampledModels.Add(model))
                {
                    samples.AppendLine($"## `{model}` — ilk başarılı koşunun kartları (koşu {run})");
                    samples.AppendLine();
                    foreach (var card in response.Cards)
                    {
                        samples.AppendLine($"### {card.Emoji} {card.Title}");
                        samples.AppendLine();
                        samples.AppendLine(card.Body);
                        if (card.Detail is not null)
                        {
                            samples.AppendLine();
                            samples.AppendLine($"> **Kavram:** {card.Detail}");
                        }
                        samples.AppendLine();
                    }
                }
            }
        }

        report.AppendLine();
        report.AppendLine("**Okuma notu:** `fallback` kaynak = model kullanılabilir kart üretemedi. ");
        report.AppendLine("`Guard düşürdü` > 0 = model yönlendirme/tahmin/yabancı dil kalıbı üretti ve ");
        report.AppendLine("kart düşürüldü — SPK sınırı açısından **modelin eğilimini** gösteren asıl sayı budur. ");
        report.AppendLine("Guard'dan geçmek metnin iyi olduğunu kanıtlamaz; aşağıdaki ham kartları oku.");
        report.AppendLine();
        report.Append(samples);

        var dir = Path.Combine(RepoRoot(), "tmp_diag", "llm-trial");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"trial-{label}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.md");
        await File.WriteAllTextAsync(path, report.ToString());

        output.WriteLine(report.ToString());
        output.WriteLine($"Rapor: {path}");
    }

    private static LlmCommentaryService BuildService(string baseUrl, string model, ILlmMetrics metrics)
    {
        var options = Options.Create(new LlmOptions
        {
            Provider = "OpenRouter",   // OpenAI-uyumlu hat — yerel uç de aynı sözleşmeyi konuşur
            ApiKey = "local",          // yerel uç anahtar doğrulamaz; boş olamaz (DI dalı)
            Model = model,
            BaseUrl = baseUrl,
            TimeoutSeconds = 600,      // yerel/CPU çıkarım yavaş olabilir; ölçüm zaman aşımına takılmasın
        });

        var http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(600),
        };

        var client = new OpenRouterLlmClient(http, options, NullLogger<OpenRouterLlmClient>.Instance);
        return new LlmCommentaryService(client, NullLogger<LlmCommentaryService>.Instance, TimeProvider.System, metrics);
    }

    /// <summary>
    /// Kurgusal ama gerçekçi portföy: yoğunlaşma (iki kalem ağırlıkta), pozitif nominal
    /// getiri, enflasyon altında kalan reel getiri, BES kalemi. Yorum hattının konuşacak
    /// şey bulması için bilinçli seçildi. Gerçek kullanıcı verisi DEĞİL.
    /// </summary>
    private static (PortfolioSummaryDto, IReadOnlyList<HoldingDto>) SamplePortfolio()
    {
        var asOf = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);

        var holdings = new List<HoldingDto>
        {
            new(Guid.Parse("11111111-1111-1111-1111-111111111111"), AssetType.Gold, "Gram Altın", null,
                CurrencyCode.TRY, CurrencyCode.TRY, "gram", 120m, 2_100m, 3_050m, 252_000m, 366_000m, 114_000m, 0.452m, 0.57m, null),
            new(Guid.Parse("22222222-2222-2222-2222-222222222222"), AssetType.Fx, "Amerikan Doları", "USD",
                CurrencyCode.USD, CurrencyCode.TRY, "adet", 4_500m, 32.10m, 41.30m, 144_450m, 185_850m, 41_400m, 0.287m, 0.29m, null),
            new(Guid.Parse("33333333-3333-3333-3333-333333333333"), AssetType.Bes, "BES Sözleşmesi", null,
                CurrencyCode.TRY, CurrencyCode.TRY, "adet", 1m, 60_000m, 78_500m, 60_000m, 78_500m, 18_500m, 0.308m, 0.12m, null),
            new(Guid.Parse("44444444-4444-4444-4444-444444444444"), AssetType.Cash, "Nakit", null,
                CurrencyCode.TRY, CurrencyCode.TRY, "adet", 12_000m, 1m, 1m, 12_000m, 12_000m, 0m, 0m, 0.02m, null),
        };

        var allocation = new List<AllocationDto>
        {
            new(AssetType.Gold, "Altın", 366_000m, 0.57m),
            new(AssetType.Fx, "Döviz", 185_850m, 0.29m),
            new(AssetType.Bes, "BES", 78_500m, 0.12m),
            new(AssetType.Cash, "Nakit", 12_000m, 0.02m),
        };

        var summary = new PortfolioSummaryDto(
            BaseCurrency: CurrencyCode.TRY,
            TotalValue: 642_350m,
            TotalCost: 468_450m,
            NetProfit: 173_900m,
            ReturnRatio: 0.371m,
            RealReturnRatio: -0.007m,   // nominal pozitif, reel eksi — eğitici çekirdek
            Allocation: allocation,
            AsOf: asOf);

        return (summary, holdings);
    }

    private static string Env(string key, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    /// <summary>bin/ içinden repo köküne yürür (EducationSeedTests'teki desenle aynı).</summary>
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, ".agent-method")))
            dir = dir.Parent;
        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }

    /// <summary>Guard'ın kaç kart düşürdüğünü servisin kendi metrik portundan okur.</summary>
    private sealed class RecordingMetrics : ILlmMetrics
    {
        public int GuardBlocked { get; private set; }
        public int InputTokens { get; private set; }
        public int OutputTokens { get; private set; }

        public void RecordCall(bool success, int inputTokens, int outputTokens, int guardBlocked)
        {
            GuardBlocked += guardBlocked;
            InputTokens += inputTokens;
            OutputTokens += outputTokens;
        }

        public void RecordServed(string source) { }
    }
}
