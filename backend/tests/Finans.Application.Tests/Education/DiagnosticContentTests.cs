using Finans.Application.Education;
using Finans.Domain.Enums;

namespace Finans.Application.Tests.Education;

/// <summary>
/// Tanılama testi puanlaması (T6.6, SC-E14, 15 §4) — saf, deterministik.
/// ⚠ En kritik iddia: <see cref="RiskAttitude"/> hiçbir istemci DTO'sunda YOK (15 §1.1).
/// </summary>
public sealed class DiagnosticContentTests
{
    private static Dictionary<string, string> Answers(params (string Q, string O)[] pairs) =>
        pairs.ToDictionary(p => p.Q, p => p.O);

    [Fact]
    public void Bank_has_four_knowledge_and_four_scenario_questions()
    {
        var qs = DiagnosticContent.Questions();

        Assert.Equal(8, qs.Count);
        Assert.Equal(4, qs.Count(q => q.Kind == DiagnosticKind.Knowledge));
        Assert.Equal(4, qs.Count(q => q.Kind == DiagnosticKind.Scenario));
        Assert.All(qs, q => Assert.True(q.Options.Count >= 3));
    }

    [Fact]
    public void Questions_never_leak_the_answer_key()
    {
        // İstemciye giden şıkta yalnız Key + Text var; IsCorrect / RiskPoints yok.
        var props = typeof(DiagnosticOptionDto).GetProperties().Select(p => p.Name).ToList();

        Assert.Equal(["Key", "Text"], props);
    }

    [Fact]
    public void Result_dto_never_exposes_risk_attitude()
    {
        // 🔒 SPK sınırı (SC-E4): risk tutumu DB'de saklanır ama dışarı VERİLMEZ.
        var resultProps = typeof(DiagnosticResultDto).GetProperties().Select(p => p.Name);
        var profileProps = typeof(LiteracyProfileDto).GetProperties().Select(p => p.Name);

        Assert.DoesNotContain("RiskAttitude", resultProps);
        Assert.DoesNotContain("RiskAttitude", profileProps);
    }

    [Theory]
    [InlineData(0, LessonLevel.Beginner)]
    [InlineData(1, LessonLevel.Beginner)]
    [InlineData(2, LessonLevel.Intermediate)]
    [InlineData(3, LessonLevel.Intermediate)]
    [InlineData(4, LessonLevel.Advanced)]
    public void Literacy_level_follows_correct_answer_count(int correct, LessonLevel expected)
    {
        var right = new[]
        {
            ("real-return", "decreased"),
            ("concentration", "concentration"),
            ("pe-ratio", "context"),
            ("compound", "144"),
        };

        var answers = Answers([.. right.Take(correct)]);

        Assert.Equal(expected, DiagnosticContent.ScoreLiteracy(answers));
    }

    [Fact]
    public void Unanswered_knowledge_questions_count_as_wrong()
    {
        // Atlamak kullanıcıyı üst seviyeye taşımamalı (aksi hâlde derin içerik açılırdı).
        Assert.Equal(LessonLevel.Beginner, DiagnosticContent.ScoreLiteracy(Answers()));
    }

    [Fact]
    public void Wrong_option_does_not_count_as_correct()
    {
        var answers = Answers(
            ("real-return", "increased"),
            ("concentration", "return"),
            ("pe-ratio", "cheap"),
            ("compound", "140"));

        Assert.Equal(LessonLevel.Beginner, DiagnosticContent.ScoreLiteracy(answers));
    }

    [Fact]
    public void Risk_attitude_reflects_scenario_signals()
    {
        var cautious = Answers(
            ("drawdown", "sell"), ("fomo", "suspicious"),
            ("horizon", "short"), ("volatility", "much"));
        var bold = Answers(
            ("drawdown", "buy"), ("fomo", "missed"),
            ("horizon", "long"), ("volatility", "little"));

        Assert.Equal(RiskAttitude.Temkinli, DiagnosticContent.ScoreRiskAttitude(cautious));
        Assert.Equal(RiskAttitude.Atilgan, DiagnosticContent.ScoreRiskAttitude(bold));
    }

    [Fact]
    public void Skipping_scenarios_yields_neutral_attitude()
    {
        Assert.Equal(RiskAttitude.Dengeli, DiagnosticContent.ScoreRiskAttitude(Answers()));
    }

    [Fact]
    public void Guidance_message_never_shames_or_mentions_risk_attitude()
    {
        foreach (var level in Enum.GetValues<LessonLevel>())
        {
            var msg = DiagnosticContent.Message(level);

            Assert.NotEmpty(msg);
            // Puan/yanlış sayısı verilmez (14 §4-A2 "utandırmayan").
            Assert.DoesNotContain("yanlış", msg, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("puan", msg, StringComparison.OrdinalIgnoreCase);
            // Risk tutumu etiketi asla anılmaz (15 §1.1).
            foreach (var label in Enum.GetNames<RiskAttitude>())
                Assert.DoesNotContain(label, msg, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Input_validation_helpers_reject_unknown_keys()
    {
        Assert.True(DiagnosticContent.IsKnownQuestion("real-return"));
        Assert.False(DiagnosticContent.IsKnownQuestion("uydurma-soru"));
        Assert.True(DiagnosticContent.IsKnownOption("real-return", "decreased"));
        Assert.False(DiagnosticContent.IsKnownOption("real-return", "uydurma-sik"));
        // Şık başka soruya aitse de geçersiz.
        Assert.False(DiagnosticContent.IsKnownOption("real-return", "buy"));
    }
}
