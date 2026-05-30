using Finans.Application.Pricing;

namespace Finans.Integration.Tests.Pricing;

/// <summary>Test için yapılandırılabilir sağlayıcı: çağrı sayar, verilen tırnakları döner.</summary>
internal sealed class StubPriceProvider(
    string source,
    Func<PriceInstrument, bool> canQuote,
    Func<IReadOnlyCollection<PriceInstrument>, IEnumerable<PriceQuote>> quote) : IPriceProvider
{
    public int Calls { get; private set; }

    public string Source => source;

    public bool CanQuote(PriceInstrument instrument) => canQuote(instrument);

    public Task<IReadOnlyList<PriceQuote>> GetQuotesAsync(
        IReadOnlyCollection<PriceInstrument> instruments, CancellationToken ct = default)
    {
        Calls++;
        var handled = instruments.Where(canQuote).ToList();
        return Task.FromResult<IReadOnlyList<PriceQuote>>(quote(handled).ToList());
    }
}

/// <summary>Çağrıldığında daima patlayan sağlayıcı (izolasyon/fallback testi için).</summary>
internal sealed class ThrowingPriceProvider(string source, Func<PriceInstrument, bool> canQuote) : IPriceProvider
{
    public string Source => source;

    public bool CanQuote(PriceInstrument instrument) => canQuote(instrument);

    public Task<IReadOnlyList<PriceQuote>> GetQuotesAsync(
        IReadOnlyCollection<PriceInstrument> instruments, CancellationToken ct = default) =>
        throw new HttpRequestException("test: sağlayıcı çöktü");
}
