using Finans.Domain.Education;
using Finans.Domain.Enums;

namespace Finans.Infrastructure.Seed;

/// <summary>
/// "Temeller" setinin katmanlı ders içeriği (T6.1, 15 §2). Her ders beş blok taşır:
/// <b>L1 Core</b> (herkes) → <b>L2 Context</b> (gelişen) → <b>L3 Deep</b> (ileri) +
/// <b>Example</b> (jenerik, güvenli sayılar) + <b>Trap</b> (yaygın yanılgı).
/// </summary>
/// <remarks>
/// İÇERİK KURALLARI (yazarken bunlara uy):
/// <list type="bullet">
/// <item><b>Tavsiye YOK</b> (CLAUDE.md §2): "al/sat/yükselir" yok, gelecek tahmini yok.
///   Enstrüman sıralaması da yok — örnekler "A/B yatırımı" gibi soyut etiketler kullanır,
///   "altın dolardan iyiydi" demez (zımni yönlendirme, 15 §3.4).</item>
/// <item><b>MiniMarkdown alt kümesi</b>: <c>##</c>/<c>###</c> başlık, <c>- </c> liste,
///   <c>&gt; </c> alıntı, <c>**kalın**</c>, düz paragraf, <b>[metin](hedef) bağlantı</b> ve
///   <b>boru işaretli tablo</b> (T6.8). <b>Kod bloğu YOK</b> — renderer desteklemiyor.
///   Bağlantı hedefi <c>https://</c>, <c>http://</c>, <c>mailto:</c> ya da uygulama içi
///   <c>/yol</c> olmalı; başka şema bağlantıya çevrilmez (ham metin görünür). Tablo
///   <b>hizalama satırı</b> ister: <c>| --- | --- |</c>, yoksa düz paragrafa düşer.</item>
/// <item><b>TR biçim</b>: binlik nokta, ondalık virgül (422.970,50 ₺).</item>
/// <item>Sayılar <b>statik ve güvenli</b>; kullanıcının gerçek verisi buraya girmez —
///   o iş <see cref="SectionKind.LiveContext"/> bloğunda ve T6.2'de.</item>
/// </list>
/// </remarks>
internal static class EducationContent
{
    /// <summary>
    /// Bir dersin katmanlı bloklarını üretir
    /// (sıra: Core→Context→Deep→<b>LiveContext</b>→Example→Trap).
    /// </summary>
    /// <param name="live">
    /// "Senin portföyünde" şablonu (T6.2). <c>{{anahtar}}</c> token'ları çalışma anında
    /// KODDA hesaplanmış metriklerle değişir; çözülemeyen token'ın satırı düşer.
    /// </param>
    /// <param name="figure">
    /// Örnek bloğuna eşlik eden görselin anahtarı (T6.7) — arayüz çizer, veri gömülmez.
    /// </param>
    /// <summary>Tek içerik bloğunun tarifi — ders yazarının kullandığı birim.</summary>
    /// <param name="Tier">Derinlik katmanı (kimin varsayılan yolunda).</param>
    /// <param name="Kind">Blok türü (anlatım/örnek/tuzak/canlı bağlam).</param>
    /// <param name="Body">Markdown gövde; ilk <c>##</c> satırı adım başlığı olur.</param>
    /// <param name="Figure">Varsa eşlik eden görselin anahtarı (arayüz çizer).</param>
    internal readonly record struct Blk(DepthTier Tier, SectionKind Kind, string Body, string? Figure = null);

    // Kısa yazım yardımcıları — ders içeriği okunur kalsın.
    private static Blk Core(string body, string? fig = null) => new(DepthTier.Core, SectionKind.Explain, body, fig);
    private static Blk Ctx(string body, string? fig = null) => new(DepthTier.Context, SectionKind.Explain, body, fig);
    private static Blk Deep(string body, string? fig = null) => new(DepthTier.Deep, SectionKind.Explain, body, fig);
    private static Blk Ex(string body, string? fig = null) => new(DepthTier.Core, SectionKind.Example, body, fig);
    private static Blk ExDeep(string body, string? fig = null) => new(DepthTier.Context, SectionKind.Example, body, fig);
    private static Blk Trap(string body, string? fig = null) => new(DepthTier.Core, SectionKind.Trap, body, fig);
    private static Blk Live(string body) => new(DepthTier.Core, SectionKind.LiveContext, body, null);

    /// <summary>
    /// Dersin AÇILIŞ bloğu — "Bu derste ne öğreneceksin?" (T6.19, <c>16</c> §2.5).
    /// Her dersin <b>ilk</b> aşamasıdır: öğrenme çıktıları madde madde okunur, böylece
    /// hem kullanıcı ne kazanacağını bilir hem de çıktılar testle denetlenebilir olur.
    /// Ayrı bir <see cref="SectionKind"/> gerekmez — konum ve başlık yeterli.
    /// </summary>
    private static Blk Intro(string body) => new(DepthTier.Core, SectionKind.Explain, body, null);

    /// <summary>
    /// Dersin KAPANIŞ bloğu — "Bu bilgiler nereden geliyor?" (T6.19, <c>16</c> §6.1).
    /// Üç şeyi söyler: (1) kurumsal kaynaklar, (2) örnek sayıların <b>kurgusal</b> olduğu
    /// beyanı, (3) hesapların kodda yapıldığı — dil modeli sayı üretmez (<c>CLAUDE.md</c> §3.1).
    /// Ayrıca "yatırım tavsiyesi değildir" çerçevesini taşır (<c>CLAUDE.md</c> §2).
    /// </summary>
    private static Blk Src(string body) => new(DepthTier.Core, SectionKind.Source, body, null);

    /// <summary>
    /// Blok tariflerini <see cref="LessonSection"/>'a çevirir. Blok SAYISI derse göre
    /// değişebilir — ders ne kadar aşama gerektiriyorsa o kadar (T6.11).
    /// </summary>
    private static IEnumerable<LessonSection> Build(Guid lessonId, params Blk[] blocks)
    {
        var order = 1;
        foreach (var b in blocks)
        {
            // Başlık gövdeden AYRIŞTIRILIR (T6.10): yol haritası adım adlarını gösterir,
            // başlık metinde ikinci kez görünmez. Tek kaynak: içeriğin kendisi.
            var (heading, body) = SplitHeading(b.Body.Trim());

            yield return new LessonSection
            {
                // DETERMİNİSTİK Id: seed "BU blok var mı?" diye bakar → sonradan eklenen
                // bloklar mevcut kurulumlara iner, var olanlar çoğaltılmaz, içerik
                // düzeltmeleri mutabakatla güncellenir (SeedData §SeedEducationSectionsAsync).
                Id = SeedData.Id($"section-{lessonId:N}-{order}"),
                LessonId = lessonId,
                OrderIndex = order++,
                Heading = heading,
                BodyMarkdown = body,
                DepthTier = b.Tier,
                Kind = b.Kind,
                FigureKey = b.Figure,
            };
        }
    }

    /// <summary>
    /// Klasik ders kalıbı (Set 1'in 3-5. dersleri hâlâ bunu kullanır; içerik turu
    /// T6.11c'de gelecek). Daha ayrıntılı dersler <see cref="Build"/> ile serbest
    /// sayıda blok tanımlar. <b>Açılış ve kaynak blokları zorunludur</b> (T6.19).
    /// </summary>
    private static IEnumerable<LessonSection> Blocks(
        Guid lessonId, string intro, string core, string context, string deep, string live,
        string example, string trap, string figure, string source) =>
        Build(lessonId,
            Intro(intro),
            Core(core),
            Ctx(context),
            Deep(deep),
            Live(live),
            Ex(example, figure),
            Trap(trap),
            Src(source));

    /// <summary>
    /// Gövdenin ilk satırı <c>## </c> veya <c>### </c> başlığıysa onu ayırır.
    /// Başlık yoksa <c>null</c> döner ve gövde olduğu gibi kalır.
    /// </summary>
    private static (string? Heading, string Body) SplitHeading(string body)
    {
        var lines = body.Split('\n');
        var first = lines[0].TrimStart();
        var prefix = first.StartsWith("## ") ? 3 : first.StartsWith("### ") ? 4 : 0;
        if (prefix == 0)
            return (null, body);

        return (first[prefix..].Trim(), string.Join('\n', lines.Skip(1)).Trim());
    }

    // ── Ders 1 — Enflasyon ve Reel Getiri ────────────────────────────────────

    public static IEnumerable<LessonSection> Lesson1(Guid id) => Build(id,

        // ── 0. Açılış: ne öğreneceksin (T6.19) ───────────────────────────────
        Intro("""
        ## Bu derste ne öğreneceksin?

        Bu dersi bitirdiğinde şunları yapabileceksin:

        - Cüzdanındaki **rakamın** büyümesi ile **alım gücünün** büyümesini birbirinden
          ayırmak
        - Reel getiriyi formülüyle hesaplamak
        - "%40 kazandım, enflasyon %38, demek ki %2 kârdayım" kısayolunun neden
          yalnızca kaba bir yaklaşım olduğunu göstermek
        - Resmî enflasyon oranının neden **senin** harcama sepetinden farklı
          olabileceğini açıklamak
        - Ölçtüğün **dönemin** sonucu nasıl değiştirdiğini fark etmek

        Dersin sonunda kısa bir test var; testi geçince bir sonraki ders açılır.
        """),

        // ── 1. Kavramın kendisi ──────────────────────────────────────────────
        Core("""
        ## Rakam mı büyüdü, paran mı?

        Bir yıl önce 100.000 ₺'n vardı, bugün 140.000 ₺. Kazandın mı?

        Cevap tek başına bu iki sayıda değil. Çünkü bir yıl içinde **paranın
        kendisi de değişti**: aynı ürün artık daha pahalı.

        İki farklı "getiri" vardır ve ikisi çok farklı şey söyler:

        - **Nominal getiri** — cüzdanındaki rakamın değişimi. Burada %40.
        - **Reel getiri** — **alım gücünün** değişimi. Yani o parayla gerçekte daha
          fazla şey alabiliyor musun?

        Fiyatlar aynı dönemde %38 arttıysa, 140.000 ₺ ile bugün ancak bir yıl önce
        100.000 ₺ ile aldıklarının **biraz fazlasını** alabilirsin. Rakam büyük
        ölçüde büyüdü; alım gücün çok az.

        Yatırımda asıl soru şudur: **param büyüdü mü, yoksa sadece rakam mı?**
        """),

        // ── 2. İlk somut örnek: tek kalem, adım adım ─────────────────────────
        Ex("""
        ## Adım adım: ekmek örneği

        Soyut kalmasın. Diyelim bir ekmek bugün **20 ₺**.

        **Bir yıl önce:** 100.000 ₺'n vardı, ekmek 14,50 ₺'ydi.
        Paran 100.000 / 14,50 ≈ **6.897 ekmek** ediyordu.

        **Bugün:** 140.000 ₺'n var, ekmek 20 ₺.
        Paran 140.000 / 20 = **7.000 ekmek** ediyor.

        Ekmek cinsinden zenginliğin: 6.897 → 7.000. Artış **%1,5 civarı**.

        Oysa lira cinsinden %40 kazanmış görünüyorsun. Aradaki fark hayal değil —
        biri lirayı, diğeri **alabildiğin şeyi** ölçüyor.

        > Reel getiri tam olarak bu ikinci ölçüdür: paranı lira yerine
        > **mal ve hizmet** cinsinden saymak.
        """, "purchasing-power"),

        // ── 3. Formül ────────────────────────────────────────────────────────
        Ctx("""
        ## Nasıl hesaplanır?

        Reel getiri, nominal getiriyi enflasyona göre düzeltir:

        > reel getiri = (1 + nominal) / (1 + enflasyon) − 1

        Örnekteki rakamlarla: (1,40 / 1,38) − 1 ≈ **%1,4**.

        Ekmek hesabıyla bulduğumuz %1,5'e çok yakın — küçük fark yuvarlamadan.
        İki yol aynı yere çıkar: formül, "kaç ekmek" hesabının kısayoludur.

        ### Formülü okuma biçimi

        - Payda **paranın** ne kadar büyüdüğü: 1,40 kat.
        - Paydada **fiyatların** ne kadar büyüdüğü: 1,38 kat.
        - Bölüm, ikisinin **yarışını** verir: para fiyatları geçebildi mi?

        Bölüm 1'den büyükse alım gücün arttı, küçükse azaldı.
        """),

        // ── 4. Çıkarma tuzağı + görsel ───────────────────────────────────────
        Trap("""
        ## Tuzak: "çıkarıver, olur biter"

        Çoğu kişi %40 − %38 = **%2** der. Bu bir kısayoldur ve **düşük enflasyonda**
        kabul edilebilir sonuç verir. Ama enflasyon yükseldikçe sapar.

        Neden? Çünkü enflasyon yalnız ana paranı değil, **kazandığın getiriyi de**
        eritir. Çıkarma bunu hesaba katmaz.

        Aynı 10 puanlık farkın üç ortamda ne verdiğine bak:

        - Nominal %12, enflasyon %10 → çıkarma %2 · **gerçek %1,8** (fark küçük)
        - Nominal %45, enflasyon %35 → çıkarma %10 · **gerçek %7,4**
        - Nominal %85, enflasyon %75 → çıkarma %10 · **gerçek %5,7**

        Enflasyon yükseldikçe çıkarma seni **giderek daha fazla** yanıltır.
        Türkiye gibi yüksek enflasyon görmüş ortamlarda bu fark önemsiz değildir.
        """, "subtraction-error"),

        // ── 5. Hangi enflasyon? ──────────────────────────────────────────────
        Ctx("""
        ## Hangi enflasyon? Senin sepetin

        TÜFE (Tüketici Fiyat Endeksi) **ortalama bir tüketim sepetini** ölçer:
        gıda, konut, ulaşım, giyim, sağlık… hepsi belli ağırlıklarla.

        Ama kimse "ortalama" harcamaz. Senin sepetin bu ortalamadan farklıysa,
        **hissettiğin enflasyon** da farklı olur.

        - Kiracıysan ve kiralar hızlı arttıysa, senin enflasyonun TÜFE'nin üstündedir.
        - Ev sahibiysen ve araba kullanmıyorsan, altında kalabilir.

        Bu, TÜFE'nin "yanlış" olduğu anlamına gelmez — **ortalama** olduğu anlamına
        gelir. Reel getiri de bu yüzden mutlak bir gerçek değil, **hangi sepete göre**
        sorusuna bağlı bir ölçüdür.
        """),

        // ── 6. Sepet örneği ──────────────────────────────────────────────────
        Ex("""
        ## Örnek: aynı yıl, iki farklı "gerçek"

        Yıl sonunda TÜFE **%50** açıklandı. İki kişinin de nominal getirisi **%55**.

        **Birinci kişi:** kirada oturuyor, kirası %80 arttı; harcamasının büyük
        kısmı kira. Kendi sepetinin enflasyonu ≈ **%62**.
        Reel getirisi: (1,55 / 1,62) − 1 ≈ **−%4,3**.

        **İkinci kişi:** kendi evinde, ulaşımı az; sepeti ağırlıkla gıda.
        Kendi sepetinin enflasyonu ≈ **%44**.
        Reel getirisi: (1,55 / 1,44) − 1 ≈ **+%7,6**.

        Aynı yatırım, aynı yıl, aynı nominal getiri — ama biri alım gücü kaybetti,
        diğeri kazandı.

        > Bu hesaplar kişisel sepet farkını göstermek içindir; belirli bir yatırım
        > aracını işaret etmez.
        """, "basket-difference"),

        // ── 7. Dönem seçimi ──────────────────────────────────────────────────
        Ctx("""
        ## Dönem seçimi: hangi tarihten bakıyorsun?

        Aynı yatırım, seçtiğin başlangıç tarihine göre çok farklı reel getiriler
        gösterebilir. Bu, istatistiğin en sessiz tuzaklarından biridir.

        Bir dönemi öne çıkarıp diğerini görmezden gelmek — özellikle kendi
        kararını haklı çıkarmak için — farkında olmadan kendini kandırmanın en
        kolay yoludur.

        Pratik alışkanlık: bir getiri rakamı gördüğünde önce ***"hangi tarihten
        hangi tarihe?"*** diye sor. Cevap yoksa rakam eksik demektir.
        """),

        // ── 8. Dönem örneği + görsel ─────────────────────────────────────────
        ExDeep("""
        ## Örnek: aynı varlık, üç farklı pencere

        Bir varlığın beş yıllık seyrini düşün. Yıllık reel getirileri sırasıyla:
        **+%18, −%12, +%6, −%9, +%21**.

        - **Son 1 yıla** bakan: +%21 görür, "harika" der.
        - **Son 2 yıla** bakan: −%9 ve +%21 → bileşik ≈ **+%10**, "iyi" der.
        - **Beş yılın tamamına** bakan: bileşik ≈ **+%21**, yani yılda ≈ **+%3,9**.

        Üçü de doğru rakam. Üçü de farklı hikâye anlatıyor.

        En dürüst bakış, **elde tutmayı planladığın süreye** en yakın pencereyi
        seçmek ve kısa pencereleri "kanıt" saymamaktır.
        """, "window-selection"),

        // ── 9. Derin: matematiği ─────────────────────────────────────────────
        Deep("""
        ## İşin matematiği: neden bölme?

        Nominal getiri paranın **miktarını**, enflasyon ise paranın **birim değerini**
        değiştirir. İkisi aynı anda çalıştığı için etkileri toplanmaz, **çarpılır**.

        Elindeki para (1 + nominal) katına çıkarken, bir birim malın fiyatı
        (1 + enflasyon) katına çıkar. Alabileceğin mal miktarı bu ikisinin
        **oranıdır** — formüldeki bölme buradan gelir.

        ### Çıkarmanın hatası ne kadar?

        Gerçek reel getiri ile çıkarma arasındaki fark yaklaşık
        **(nominal − reel) × enflasyon** kadardır. Enflasyon büyüdükçe hata da
        büyür; bu yüzden düşük enflasyonda kimse fark etmez, yüksek enflasyonda
        ise fark ciddileşir.

        ### Negatif reel getiri ne demek?

        Reel getiri eksiyse, hesabındaki rakam artmış olsa bile **daha az şey
        alabiliyorsun** demektir. Yüksek enflasyon dönemlerinde yüksek nominal
        getiriler olağanlaşır: %45 kulağa büyük gelir ama enflasyon %50 ise
        alım gücün gerilemiştir.
        """),

        // ── 10. Derin: maliyetler ve vergi öncesi/sonrası ────────────────────
        Deep("""
        ## Getiriyi aşındıran diğer kalemler

        Reel getiri hesabı çoğu zaman "brüt" yapılır. Oysa cebine giren rakam
        birkaç kalem daha eksilir:

        - **İşlem komisyonu** — her alış ve satışta.
        - **Alış-satış makası** — aldığın fiyat ile satabileceğin fiyat arasındaki
          fark; özellikle az işlem gören varlıklarda geniştir.
        - **Fon gider oranı** — fonlarda yıllık, otomatik kesilir.
        - **Saklama/hesap ücretleri** — varsa.

        Bu kalemler **nominal getiriden** düşer; sonra üstüne enflasyon işler.
        Sırayı karıştırmamak gerekir: önce net nominal, sonra reel.

        ### Neden bu kavram her şeyin temeli?

        Enflasyon, yatırımın **sıfır çizgisidir**. Enflasyonun altında kalan her
        getiri, matematiksel olarak alım gücü kaybıdır — rakam büyürken servetin
        küçülür. Bu yüzden bir portföyü değerlendirirken ilk bakılacak yer nominal
        kâr değil, enflasyona göre nerede durduğudur.
        """),

        // ── 11. Senin portföyünde ────────────────────────────────────────────
        Live("""
        ## Senin portföyünde

        Portföyünün nominal getirisi **{{return_ratio}}**; enflasyondan arındırılmış
        **reel** getirisi ise **{{real_return}}**.

        > ℹ️ Nominal getiri kendi kayıtlarından gelir (gerçek). Reel getiri ise bir
        > enflasyon oranına göre hesaplanır; uygulama şu an **örnek** bir oran
        > kullanıyor (henüz canlı TÜİK verisine bağlı değil), bu yüzden reel rakamı
        > da bir **örnek** olarak oku.

        Bu dersin anlattığı ayrım tam olarak bu iki sayının arasındaki farktır:
        birincisi rakamın, ikincisi alım gücünün ne yaptığını söyler.

        Toplam değerin **{{total_value}}** — ama bu rakamın bir yıl önceki
        karşılığıyla aynı şeyleri alıp alamayacağını söyleyen, reel getiridir.
        """),

        // ── 12. Toparlayıcı örnek + ana görsel ───────────────────────────────
        Ex("""
        ## Toparlayalım: aynı yıl, üç farklı sonuç

        Bir yılda genel fiyat artışı **%50** oldu. Üç kişinin nominal getirisi:

        - **A yatırımı:** %45 nominal → reel ≈ **−%3,3**
        - **B yatırımı:** %50 nominal → reel = **%0** (tam başa baş)
        - **C yatırımı:** %70 nominal → reel ≈ **+%13,3**

        Üçü de "kâr etti" diyebilir; ancak yalnızca C'nin alım gücü arttı. A, hesap
        özetinde artı bakiye görmesine rağmen bir yıl öncesine göre daha az şey
        alabiliyor. B ise koştu ama yerinde saydı.

        > Buradaki A, B, C birer **hesaplama örneğidir** — belirli bir yatırım
        > aracını işaret etmez ve hiçbiri diğerinden "iyi" ilan edilmiyor.
        """, "real-vs-nominal"),

        // ── 13. Kapanış tuzakları ────────────────────────────────────────────
        Trap("""
        ## Sık yapılan hatalar

        **"Yüksek nominal getiri = başarılı yatırım."**
        Yüksek enflasyon ortamında yüksek nominal getiriler olağandır — herkesin
        rakamı büyür. Bu ortamda %60 getiri, enflasyon %65 ise aslında bir kayıptır.

        **"Kâr/zararı lira olarak okumak."**
        "50.000 ₺ kazandım" cümlesi, o 50.000 ₺'nin bugün ne aldığını söylemez.

        **"Enflasyonu yalnızca kötü haber saymak."**
        Enflasyon bir düşman değil, bir **ölçüm çizgisidir**: getirini
        karşılaştıracağın taban. Bu çizgiyi bilmeden bir yatırımın iyi mi kötü mü
        gittiğini söylemek mümkün değildir.

        **"Tek bir yılın reel getirisine bakıp karar vermek."**
        Bir yıl gürültüdür. Kavramın değeri, aynı ölçüyü **yıllar boyunca tutarlı**
        biçimde uygulamakta.
        """),

        // ── Kapanış: kaynaklar (T6.19) ───────────────────────────────────────
        Src("""
        ## Bu bilgiler nereden geliyor?

        **Enflasyon ölçümü.** Türkiye'de fiyat değişiminin resmî ölçüsü **TÜİK**'in
        Tüketici Fiyat Endeksi'dir (TÜFE) — [tuik.gov.tr](https://www.tuik.gov.tr).
        Bir enflasyon oranı gördüğünde **hangi döneme ait olduğuna** bak: dönem
        değişince oran da değişir, bu yüzden uygulamada oranlar dönem damgasıyla
        birlikte gösterilir.

        **Örnek sayılar kurgusaldır.** Bu dersteki 100.000 ₺, %40, ekmek fiyatı gibi
        rakamların tamamı anlatımı somutlaştırmak için **seçilmiş örneklerdir** —
        gerçek piyasa verisi değildir. Gerçek veri yalnızca "Senin portföyünde"
        bölümünde, kendi kayıtlarından gelir.

        **Hesaplar kodda yapılır.** Reel getiri ve benzeri tüm sayılar uygulamada
        sabit formüllerle hesaplanır; bir yapay zekâ modeli bu rakamları **üretmez**,
        yalnızca hazır sonuçları sade dille anlatır.

        **Bu bir yatırım tavsiyesi değildir.** Ders bir kavramı açıklar; hangi varlığı
        alıp satacağına dair yönlendirme yapmaz.
        """));

    // ── Ders 2 — Çeşitlendirme ───────────────────────────────────────────────

    public static IEnumerable<LessonSection> Lesson2(Guid id) => Build(id,

        // ── 0. Açılış: ne öğreneceksin (T6.19) ───────────────────────────────
        Intro("""
        ## Bu derste ne öğreneceksin?

        Bu dersi bitirdiğinde şunları yapabileceksin:

        - Çeşitlendirmeyi "riski **farklı kaynaklara yayma**" olarak tanımlamak
        - İki varlığın birlikte mi bağımsız mı hareket ettiğini sezmek
        - Farklı isimler taşıyan kalemlerin neden **tek bir bahis** olabileceğini
          göstermek
        - Türkiye'ye özgü tuzağı fark etmek: dört farklı kalem, ama hepsi tek bir
          para biriminde
        - Ağırlıkların sen işlem yapmadan nasıl kaydığını görmek

        Dersin sonunda kısa bir test var.
        """),

        // ── 1. Kavram ────────────────────────────────────────────────────────
        Core("""
        ## Ağırlık nerede toplanıyor?

        Bir portföyün değeri tek bir varlığa bağlıysa, o varlık düştüğünde
        portföyün tamamı birlikte düşer. Kaderin tek bir habere bağlanmış olur.

        **Yoğunlaşma**, değerin az sayıda kalemde toplanmasıdır. Portföyünün
        %84'ü iki varlıktaysa, o iki varlığın ortak kaderi senin de kaderindir.

        Çeşitlendirme riski **yok etmez** — farklı kaynaklara **yayar**. Amaç,
        bütün varlıklarının aynı anda aynı yöne hareket etme ihtimalini azaltmak.

        Bu, "şu kadar varlık iyidir" diye bir kural değil, bir farkındalıktır:
        **ağırlığının nerede toplandığını bilmek.**
        """),

        // ── 2. İlk somut örnek ───────────────────────────────────────────────
        Ex("""
        ## Örnek: iki portföy, aynı kalem sayısı

        İki portföyün de beş kalemi var, ikisi de 100.000 ₺:

        **Birinci portföy** — ağırlıklar: %70, %10, %8, %7, %5.
        En büyük kalem tek başına portföyün üçte ikisinden fazlası.
        Bu kalem %30 değer kaybederse portföy yaklaşık **%21** küçülür.

        **İkinci portföy** — ağırlıklar: %25, %22, %20, %18, %15.
        En büyük kalem aynı %30'u kaybederse portföy yaklaşık **%7,5** küçülür.

        İkisinin de "beş kalemi" var; aynı olay birinde üç kat daha sert vuruyor.

        > Kalem **sayısı** değil, **ağırlık dağılımı** belirleyicidir.
        """, "concentration"),

        // ── 3. Ölçme ─────────────────────────────────────────────────────────
        Ctx("""
        ## Çeşitlendirme nasıl ölçülür?

        En basit ölçü **ağırlıktır**: her varlığın güncel değerinin portföy
        toplamına oranı.

        > varlık ağırlığı = varlık güncel değeri / portföy toplam değeri

        Yoğunlaşmayı görmenin pratik yolu, **en büyük iki-üç kalemin toplam
        ağırlığına** bakmaktır. Tek bir sayı, dağılımın ne kadar dengesiz
        olduğunu hızlıca özetler.

        ### Kaba bir okuma çerçevesi

        - En büyük iki kalem toplamı **%60 altındaysa** dağılım görece dengeli sayılır.
        - **%60-%80 arası** belirgin yoğunlaşma vardır.
        - **%80 üzerinde** portföy fiilen birkaç kalemden ibarettir.

        Bunlar kesin eşikler değil, **okuma alışkanlığı** için kaba işaretlerdir;
        neyin "doğru" olduğunu değil, nerede durduğunu gösterirler.
        """),

        // ── 4. Kalem sayısı tuzağı ───────────────────────────────────────────
        Trap("""
        ## Tuzak: "on kalemim var, çeşitlendirdim"

        On farklı kalem tutuyor olman çeşitlendirdiğin anlamına gelmez.
        Onunun da aynı sektörde, aynı para biriminde ya da aynı ekonomik
        hikâyeye bağlı olması mümkündür.

        Böyle bir portföyde kalem sayısı çok, **gerçek çeşitlilik azdır**:
        sektörü vuran tek bir haber onunu birden aşağı çeker. Sayıca on,
        davranışça bir.

        Kendine sorulacak soru "kaç kalemim var?" değil, şudur:
        **bu kalemler farklı sebeplerle mi değer kazanıp kaybediyor?**
        """, "same-sector"),

        // ── 5. Birlikte hareket ──────────────────────────────────────────────
        Ctx("""
        ## Asıl mesele: birlikte hareket

        Çeşitlendirmenin işe yaraması için varlıkların **farklı sebeplerle**
        değer kazanıp kaybetmesi gerekir.

        İki varlık her zaman aynı yöne gidiyorsa, portföyde ikisini birden
        tutmak tek varlık tutmaktan farksızdır — riski bölmezsin, aynı riski
        iki parçaya yazarsın.

        Farklı sebeplerle hareket eden varlıklar ise birbirinin dalgalanmasını
        **yumuşatır**: biri kötü haberle düşerken diğeri o haberden etkilenmez.
        Portföyün toplam oynaklığı, tek tek kalemlerin oynaklığından düşük olur.
        """),

        // ── 6. Birlikte hareket örneği ───────────────────────────────────────
        Ex("""
        ## Örnek: aynı yöne mi, farklı yöne mi?

        İki portföy düşün; ikisi de iki eşit parçadan oluşuyor (%50 / %50).
        Dört çeyrekteki getiriler:

        **Birlikte hareket eden çift**
        - X: +%20, −%15, +%18, −%12
        - Y: +%18, −%14, +%20, −%11
        - Portföy: +%19, −%14,5, +%19, −%11,5 → **dalgalanma aynen sürüyor**

        **Farklı hareket eden çift**
        - X: +%20, −%15, +%18, −%12
        - Z: −%6, +%11, −%4, +%9
        - Portföy: +%7, −%2, +%7, −%1,5 → **dalgalanma belirgin şekilde azaldı**

        İki portföyün de ortalama getirisi benzer; ama ikincisinin yolculuğu çok
        daha sakin. Çeşitlendirmenin kazandırdığı şey budur: aynı yere daha az
        sarsılarak gitmek.

        > X, Y, Z birer hesaplama örneğidir; belirli varlıkları temsil etmez.
        """, "correlation-paths"),

        // ── 7. Para birimi ekseni ────────────────────────────────────────────
        Ctx("""
        ## Gözden kaçan eksen: para birimi

        Farklı varlık türlerine yayılmış görünen bir portföy, hepsi aynı para
        birimine bağlıysa hâlâ **tek bir riske** açıktır.

        Türkiye'de bu özellikle önemlidir: mevduat, yerel hisse ve yerel fon
        farklı "türler" gibi durur, ama üçü de aynı para biriminin alım gücüne
        bağlıdır. Kur hareketi üçünü birden aynı yönde etkileyebilir.

        Çeşitlendirmeyi düşünürken en az üç ekseni ayrı ayrı sormak gerekir:

        - **Varlık türü** — altın, hisse, mevduat, fon…
        - **Para birimi** — TL, USD, EUR…
        - **Coğrafya / ekonomi** — yerel mi, dışa açık mı?

        Üçünde de aynı yerde toplanmışsan, kalem sayın ne olursa olsun tek bir
        bahis oynuyorsun demektir.
        """),

        // ── 8. Para birimi örneği ────────────────────────────────────────────
        ExDeep("""
        ## Örnek: dört kalem, tek eksen

        Bir portföyde dört kalem var ve hepsi TL cinsinden: vadeli mevduat,
        yerel bir hisse, yerel bir tahvil fonu, bir de TL cinsi likit fon.

        Varlık türü ekseninde dört farklı kutu görünür — dağılım "çeşitli" gibi.
        Ama **para birimi ekseninde tek kutu** vardır: dördü de TL.

        TL'nin alım gücünü etkileyen bir gelişme dördünü birden aynı yönde
        etkiler. Kalem sayısı 4, gerçek eksen sayısı 1.

        Aynı portföye farklı para biriminde bir kalem eklemek, **kalem sayısını
        1 artırırken eksen sayısını da 1 artırır** — ilkinden çok daha değerli
        bir değişimdir.
        """),

        // ── 9. Derin: korelasyon ─────────────────────────────────────────────
        Deep("""
        ## Riskin hangi kısmı azalır?

        İki varlığın birlikte hareket etme eğilimine **korelasyon** denir.
        Kabaca −1 ile +1 arasında bir sayıdır:

        - **+1'e yakın:** neredeyse hep birlikte hareket ederler — bir arada
          tutmak riski azaltmaz.
        - **0 civarı:** birbirinden bağımsız hareket ederler — bir arada tutmak
          toplam oynaklığı belirgin şekilde düşürür.
        - **−1'e yakın:** biri çıkarken diğeri düşer — teoride en güçlü
          yumuşatma, pratikte nadir ve kalıcı değildir.

        ### Ortadan kaldırılamayan risk

        Çeşitlendirme **varlığa/şirkete özgü** riski azaltır: bir şirketin kötü
        yönetilmesi, bir madenin kapanması, bir fonun yanlış konumlanması.

        Ancak **sisteme özgü** riski azaltmaz — genel ekonomik daralma, ülke
        çapında bir şok, küresel bir kriz. Bu tür olaylar her şeyi aynı anda
        etkilediği için çeşitlendirmeyle kaçılamaz.

        Bu ayrım önemlidir: **hiçbir çeşitlendirme seviyesi seni "risksiz"
        yapmaz.** Yapabileceği, tek bir kötü olayın seni orantısız vurmasını
        engellemektir.

        ### Kriz anlarında korelasyonlar artar

        Sakin dönemlerde bağımsız hareket eden varlıklar, sert düşüşlerde
        birlikte düşme eğilimi gösterir. Yani çeşitlendirme, en çok ihtiyaç
        duyulan anda beklenenden az koruyabilir. Bunu bilmek, ondan gereğinden
        fazlasını beklememeyi sağlar.
        """),

        // ── 10. Derin: sınır ve yeniden dengeleme ────────────────────────────
        Deep("""
        ## Çeşitlendirmenin sınırı ve kayması

        ### Aşırı dağılmak da bir maliyettir

        Sonsuz çeşitlendirme diye bir hedef yoktur. Çok sayıda küçük kalem:

        - takip etmeyi zorlaştırır (neyin neden değiştiğini bilemezsin),
        - her kalem için işlem maliyeti doğurur,
        - hiçbiri portföyde anlamlı bir yer tutmadığı için sonuca etki etmez.

        Çeşitlendirme "ne kadar çok o kadar iyi" değil, **anlamlı farklılık**
        meselesidir. Beş gerçekten farklı kalem, otuz benzer kalemden iyidir.

        ### Portföy kendiliğinden yoğunlaşır

        En sinsi kısım budur. Zamanla iyi giden varlık büyür ve ağırlığı artar;
        hiçbir işlem yapmasan bile portföy **kendiliğinden** yoğunlaşır.

        İki yıl önce dengeli kurduğun bir portföy, bugün tek bir kaleme bağlı
        hâle gelmiş olabilir. Bu yüzden çeşitlendirme bir kerelik bir kurulum
        değil, **süregelen bir durumdur**.

        Ağırlıkların zaman içinde nasıl kaydığını izlemek — ve gerekirse
        farkında olarak bir şey yapmak ya da yapmamak — kavramın pratik karşılığıdır.
        """),

        // ── 11. Senin portföyünde ────────────────────────────────────────────
        Live("""
        ## Senin portföyünde

        En büyük iki varlığın portföyünün **{{concentration_top2}}**'sini
        oluşturuyor. Toplam **{{holding_count}}** kalemin
        **{{asset_class_count}}** farklı varlık türüne yayılmış durumda.

        Nakit ağırlığın **{{cash_weight}}**.

        Bu dersteki **yoğunlaşma** kavramı tam olarak ilk sayıyı ölçer. Yüksek
        olması kendiliğinden yanlış değildir; **bilinmesi gereken** bir durumdur.
        """),

        // ── 12. Kayma örneği ─────────────────────────────────────────────────
        Ex("""
        ## Örnek: hiçbir şey yapmasan da değişir

        Beş eşit kalemle (%20 × 5) başladığını düşün. Üç yıl boyunca hiç işlem
        yapmadın; kalemler farklı büyüdü:

        - Bir kalem üç yılda 3 katına çıktı.
        - İkisi yaklaşık %40 büyüdü.
        - İkisi yerinde saydı.

        Yeni ağırlıklar yaklaşık: **%45, %21, %21, %6,5, %6,5**.

        Hiçbir karar almadın; ama portföyün en büyük kalemi artık toplamın
        neredeyse yarısı. Üç yıl önce "dengeli" dediğin portföy bugün yoğun.

        > Bu, "satıp dengelemelisin" demek değildir — bu ders tavsiye vermez.
        > Söylediği şu: **ağırlıklar sen fark etmeden değişir**, bu yüzden
        > arada bir bakmak gerekir.
        """, "concentration-drift"),

        // ── 13. Kapanış tuzakları ────────────────────────────────────────────
        Trap("""
        ## Sık yapılan hatalar

        **"Çeşitlendirirsem kaybetmem."**
        Çeşitlendirme kaybı engellemez; tek bir kötü olayın seni orantısız
        vurmasını engeller. Piyasanın tümü düştüğünde çeşitlendirilmiş portföy
        de düşer.

        **"Farklı isim = farklı risk."**
        Aynı sektörden, aynı para biriminden, aynı ekonomiye bağlı kalemler
        farklı isimler taşısa da tek bir bahistir.

        **"Bir kez kurdum, tamamdır."**
        Ağırlıklar kendiliğinden kayar. Çeşitlendirme bir durum değil, **süregelen
        bir gözlemdir**.

        **"Ne kadar çok kalem o kadar iyi."**
        Otuz benzer kalem, beş gerçekten farklı kalemden daha iyi korumaz;
        üstelik takibi ve maliyeti artırır.
        """),

        // ── Kapanış: kaynaklar (T6.19) ───────────────────────────────────────
        Src("""
        ## Bu bilgiler nereden geliyor?

        **Kavramlar.** Çeşitlendirme, korelasyon ve yoğunlaşma finans literatürünün
        yerleşik kavramlarıdır. Bu derste bilinçli olarak **formül verilmedi**:
        korelasyon "+1 / 0 / −1" sezgisiyle anlatıldı, çünkü amaç hesap yapmak değil
        ağırlığın nerede toplandığını görebilmek.

        **Örnek sayılar kurgusaldır.** Portföy dağılımları, yüzdeler ve "A/B varlığı"
        gibi etiketlerin tamamı **seçilmiş örneklerdir**. Örneklerde bilerek gerçek
        varlık adı kullanılmaz — bir varlığı diğerinin önüne koymuş olmamak için.

        **Hesaplar kodda yapılır.** "Senin portföyünde" bölümündeki ağırlık ve
        yoğunlaşma oranları senin kendi kayıtlarından, sabit formüllerle hesaplanır;
        bir yapay zekâ modeli bu rakamları **üretmez**.

        **Bu bir yatırım tavsiyesi değildir.** Ders "şu kadar varlığa dağıt" demez;
        yalnızca ağırlığın nerede toplandığını görmeyi öğretir.
        """));

    // ── Ders 3 — F/K, PD/DD ──────────────────────────────────────────────────

    public static IEnumerable<LessonSection> Lesson3(Guid id) => Blocks(id,
        intro: """
        ## Bu derste ne öğreneceksin?

        Bu dersi bitirdiğinde şunları yapabileceksin:

        - F/K oranını "şirketin 1 liralık kârı için kaç lira ödüyorum" sorusu olarak
          okumak
        - PD/DD'yi şirketin defter değeriyle ilişkilendirerek yorumlamak
        - Temettü verimini hesaplamak
        - Bir oranın **tek başına** değil, ancak **benzer şirketlerle birlikte**
          anlam taşıdığını göstermek

        Dersin sonunda kısa bir test var.
        """,
        core: """
        ## Bir hisseyi okumanın rakamları

        **F/K (Fiyat / Kazanç)**, hisse fiyatının şirketin hisse başına kârına
        oranıdır. Sorduğu şey basittir: **şirketin 1 liralık kârı için kaç lira
        ödüyorum?**

        **PD/DD (Piyasa Değeri / Defter Değeri)**, şirketin borsadaki değerinin
        muhasebe defterindeki öz kaynağına oranıdır. 1'in üzerinde olması, piyasanın
        şirkete defterdeki değerinden fazlasını biçtiğini gösterir.

        **Temettü verimi**, dağıtılan kâr payının hisse fiyatına oranıdır.

        Bu oranların hiçbiri tek başına bir hisseyi "iyi" ya da "kötü" yapmaz.
        Sana **neye bakman gerektiğini** ve rakamların hikâyesini anlatır.
        """,
        context: """
        ## Oranlar ne söyler, ne söylemez?

        > F/K = hisse fiyatı / hisse başına kâr (EPS)
        > PD/DD = piyasa değeri / defter değeri
        > temettü verimi = hisse başına temettü / hisse fiyatı

        ### Düşük F/K "ucuz" demek midir?

        Hayır. Düşük F/K iki çok farklı şeyin işareti olabilir: piyasa şirketi
        gözden kaçırmış olabilir ya da piyasa kârın düşeceğini düşünüyor olabilir.
        Oran hangisi olduğunu **söylemez** — yalnızca soruyu sordurur.

        ### Karşılaştırma bağlam ister

        F/K yalnızca benzer şirketler arasında anlamlıdır. Hızlı büyüyen bir
        sektörün F/K'sı, olgun ve yavaş büyüyen bir sektörden yapısal olarak
        yüksektir. Sektörler arası F/K karşılaştırması çoğu zaman elma ile armut
        karşılaştırmasıdır.

        ### Kâr tek seferlik olabilir

        F/K'nın paydası geçmiş kârdır. Şirket bir gayrimenkul satıp tek seferlik
        büyük kâr yazdıysa F/K yapay olarak düşer, ertesi yıl normale döner.

        ### PD/DD ve sektör yapısı

        Defter değeri, fiziksel varlığı çok olan şirketlerde (fabrika, arsa) anlamlıdır.
        Değeri markasından veya yazılımından gelen bir şirkette defter değeri gerçeği
        eksik anlatır — bu yüzden PD/DD yüksek çıkar.
        """,
        deep: """
        ## Oranların arkasındaki mekanik

        ### F/K aslında ne ölçer?

        F/K'yı tersine çevirirsen (K/F) **kazanç verimi** elde edersin. F/K'sı 10 olan
        bir şirketin kazanç verimi %10'dur: bugünkü kâr seviyesi sabit kalsaydı,
        ödediğin parayı kârla geri almak yaklaşık 10 yıl sürerdi.

        Bu yorum, F/K'nın neden bir **beklenti göstergesi** olduğunu açıklar.
        Yüksek F/K, piyasanın gelecekteki kârın bugünkünden büyük olacağını
        fiyatladığı anlamına gelir — bu beklenti gerçekleşebilir de, gerçekleşmeyebilir de.

        ### Enflasyon ve muhasebe

        Yüksek enflasyon ortamında muhasebe kârı yanıltıcı olabilir: geçmişte ucuza
        alınmış stok bugünkü fiyattan satıldığında oluşan fark, gerçek bir
        performanstan çok fiyat artışının yansımasıdır. Türkiye'de bilanço okurken
        enflasyon muhasebesinin uygulanıp uygulanmadığı bu yüzden önemlidir.

        ### Borç görünmez

        F/K borcu hesaba katmaz. Aynı kârı üreten iki şirketten biri borçsuz,
        diğeri ağır borçluysa F/K'ları aynı görünebilir ama taşıdıkları risk
        çok farklıdır.

        ### Oranların ortak sınırı

        Hepsi **geçmiş veriye** dayanır ve tek bir sayıya indirgenmiş özetlerdir.
        Bir şirketin hikâyesini — rekabeti, yönetimi, sektör dinamiğini — bir orana
        sığdırmak mümkün değildir. Oranlar, araştırmanın **başladığı** yerdir,
        bittiği yer değil.
        """,
        live: """
        ## Senin portföyünde

        Portföyünün **{{stock_weight}}**'i hisse senedinde.

        Bu dersteki oranları kendi hisselerin üzerinde görmek için **Hisse** sekmesini
        kullanabilirsin; oradaki kartlar aynı rakamların ne anlattığını açıklar.
        """,
        example: """
        ## Örnek: aynı F/K, farklı hikâye

        İki şirketin de F/K'sı **8**:

        - **Birinci şirket:** kârı son üç yıldır istikrarlı, borcu düşük. F/K 8,
          piyasanın bu şirketten büyük bir büyüme beklemediğini düşündürür.
        - **İkinci şirket:** geçen yıl bir fabrikasını satmış ve bu tek seferlik
          gelir kârı şişirmiş. Bu gelir olmasaydı F/K yaklaşık 20 olacaktı.

        Aynı rakam, iki tamamen farklı durum. F/K'yı gördüğünde sorulacak ilk soru
        şudur: **bu kâr sürdürülebilir mi?**

        > Buradaki şirketler kurgusaldır ve hiçbiri diğerine tercih edilmesi gereken
        > bir seçenek olarak sunulmamaktadır.
        """,
        trap: """
        ## Sık yapılan hata

        **"F/K düşükse ucuzdur, alınır."**

        Düşük F/K çoğu zaman piyasanın bir sorun sezdiğinin işaretidir. "Ucuz"
        görünen şirket, kârı düşmek üzere olduğu için ucuz olabilir. Buna
        **değer tuzağı** denir.

        İkinci hata: **tek bir orana bakıp karar vermek.** F/K, PD/DD ve temettü
        verimi aynı şirketin farklı yüzleridir; biri güzel görünürken diğeri
        uyarı veriyor olabilir.

        Üçüncüsü: **sektör farkını unutmak.** Bir bankanın PD/DD'siyle bir yazılım
        şirketininkini yan yana koymak, iki farklı ölçü biriminde konuşmaktır.

        Son olarak, bu oranlar **ne yapman gerektiğini söylemez.** Sana şirketin
        rakamlarının ne anlattığını gösterir; kararın ve sorumluluğun sana aittir.
        """,
        figure: "ratio-context",
        source: """
        ## Bu bilgiler nereden geliyor?

        **Şirket verileri.** Hisse başına kâr, öz kaynak ve kâr payı gibi kalemler
        şirketlerin **KAP**'ta (Kamuyu Aydınlatma Platformu) yayımladığı finansal
        tablolardan okunur — [kap.org.tr](https://www.kap.org.tr). Sermaye piyasası
        tanımları ve raporlama kuralları **SPK** mevzuatında yer alır —
        [spk.gov.tr](https://www.spk.gov.tr).

        **Örnek sayılar kurgusaldır.** Bu dersteki oranlar ve "A şirketi" gibi
        etiketler **seçilmiş örneklerdir**. Gerçek şirket adı, sembolü veya güncel
        oranı bilinçli olarak kullanılmaz — bir hisseyi işaret etmiş olmamak için.

        **Eşik verilmez.** "Şu değerin altındaki F/K ucuzdur" türü bir sınır bu derste
        yoktur; böyle bir eşik sektöre, döneme ve şirketin durumuna göre değişir.

        **Bu bir yatırım tavsiyesi değildir.** Ders oranların ne anlama geldiğini
        anlatır; hangi hissenin alınıp satılacağına dair yönlendirme yapmaz.
        """);

    // ── Ders 4 — Risk ve Getiri ──────────────────────────────────────────────

    public static IEnumerable<LessonSection> Lesson4(Guid id) => Blocks(id,
        intro: """
        ## Bu derste ne öğreneceksin?

        Bu dersi bitirdiğinde şunları yapabileceksin:

        - Riski "kaybetmek" değil **sonucun belirsizliği** olarak tanımlamak
        - Oynaklığı bir örnek üzerinde okumak
        - Yüksek getiri vaadinin neden yüksek belirsizlikle geldiğini açıklamak
        - "Garantili yüksek getiri" ifadesinin neden kendi içinde çeliştiğini
          göstermek

        Dersin sonunda kısa bir test var.
        """,
        core: """
        ## Yüksek getiri, yüksek belirsizlik

        Bir yatırımın yüksek getiri "vaat etmesi", o getirinin gerçekleşmeme —
        hatta zarar etme — ihtimalinin de yüksek olması demektir. Risk ve beklenen
        getiri genelde birlikte hareket eder.

        Yatırımda **risk**, "kötü bir şey olma ihtimali" değil, sonucun ne kadar
        **oynak ve belirsiz** olduğudur. Düşük oynaklıkta sonuç tahmin edilebilir
        ama küçüktür; yüksek oynaklıkta sonuç geniş bir aralığa yayılır.

        "Garantili yüksek getiri" bir çelişkidir. Getiri yüksekse riski birileri
        taşıyor demektir; görünmüyorsa saklanıyordur.

        Doğru soru "en yüksek getiri hangisi?" değil, **"bu getiri için ne kadar
        oynaklığa katlanabilirim?"** sorusudur.
        """,
        context: """
        ## Riski nasıl düşünmeli?

        ### Oynaklık = sonucun yayılımı

        Yıllık ortalama %10 getiren iki yatırım düşün. Biri her yıl %8 ile %12
        arasında gidip geliyor; diğeri bir yıl %60 kazanıp ertesi yıl %40
        kaybediyor. Ortalamaları benzer, **yaşanan deneyim** tamamen farklı.

        ### Vade riski değiştirir

        Aynı varlık, bir yıllığına tutulduğunda çok riskli, on yıllığına
        tutulduğunda daha ölçülü davranabilir. Çünkü kısa vadede fiyatı duygular
        ve haberler sürüklerken, uzun vadede altta yatan üretkenlik daha belirleyici
        olur. Bu yüzden "risk" sorusu her zaman **"ne kadar süreyle?"** sorusuyla
        birlikte sorulur.

        ### Paraya ne zaman ihtiyacın var?

        En büyük risklerden biri, paraya ihtiyaç duyduğun anda varlığın düşükte
        olmasıdır. Altı ay sonra kullanacağın parayla, yirmi yıl sonrası için
        ayırdığın para aynı şey değildir — aynı varlık birinde makul, diğerinde
        yersiz olabilir.

        ### Likidite de bir risktir

        Bazı varlıklar istediğin anda, istediğin fiyattan satılamaz. Alıcı bulmanın
        zor olması ya da alış-satış makasının açık olması, tabloda görünmeyen bir
        risktir.
        """,
        deep: """
        ## Risk primi ve kayıpların asimetrisi

        ### Risk primi

        Yatırımcılar belirsizliğe bedava katlanmaz. Daha belirsiz bir yatırımın,
        insanları ikna edebilmek için daha yüksek **beklenen** getiri sunması
        gerekir. Aradaki bu fark **risk primi** olarak adlandırılır.

        Kritik kelime "beklenen"dir: bu bir söz değil, bir ortalamadır. Gerçekleşen
        sonuç beklenenin çok altında da çıkabilir — riskin tanımı zaten budur.

        ### Ortalama, yaşananı anlatmaz

        Bir yatırım "yılda ortalama %15 getirdi" cümlesi, o yolun düz olduğunu
        söylemez. Yol boyunca %50 düşüş yaşanmış olabilir. Ortalamalar geçmişi
        özetlerken en sancılı kısmı gizler.

        ### Kayıpların asimetrisi

        %50 kaybettikten sonra başa dönmek için %100 kazanmak gerekir. Kayıp ve
        kazanç simetrik değildir; bu asimetri, büyük düşüşlerden kaçınmayı
        matematiksel olarak değerli kılar.

        ### Riski taşıyabilmek, katlanabilmekten farklıdır

        İki ayrı soru vardır: **mali olarak** kaldırabilir misin (paraya ne zaman
        ihtiyacın var?) ve **duygusal olarak** taşıyabilir misin (düşüşte uykun
        kaçar mı?). İkisi farklı cevaplar verebilir ve zayıf olan hangisiyse
        gerçek sınırın odur.

        > Bu ders bir risk profili çıkarmaz ve sana hangi varlığı tutman gerektiğini
        > söylemez. Amacı, riskin ne anlama geldiğini kendi başına değerlendirebilmen.
        """,
        live: """
        ## Senin portföyünde

        Portföyünün **{{concentration_top2}}**'si en büyük iki kalemde toplanmış;
        **{{cash_weight}}**'i nakitte duruyor.

        Bu iki sayı, dersin anlattığı iki farklı riski gösterir: yoğunlaşma tek bir
        olayın seni orantısız etkileme ihtimalini, nakit oranı ise paraya ihtiyaç
        duyduğunda ne kadar esnek olduğunu anlatır.
        """,
        example: """
        ## Örnek: aynı ortalama, farklı yolculuk

        İki yatırımın da beş yıllık ortalama getirisi yıllık **%15**.

        - **A yatırımı:** %14, %16, %15, %14, %16.
        - **B yatırımı:** %70, −%35, %60, −%20, %35.

        Hesap makinesinde benzer görünürler. Ama B'yi tutan kişi ikinci yıl
        portföyünün üçte birini kaybettiğini gördü ve dördüncü yıl bunu tekrar
        yaşadı. Çoğu kişi bu yolculuğu tamamlayamaz — en kötü anda satar ve
        ortalamayı hiç göremez.

        Risk, tablodaki sayı değil, **yolda yaşadığındır**.

        > A ve B birer hesaplama örneğidir; belirli yatırım araçlarını temsil
        > etmez ve biri diğerine üstün gösterilmemektedir.
        """,
        trap: """
        ## Sık yapılan hata

        **"Geçen yıl çok kazandırdı, demek ki güvenli."**

        Geçmiş getiri, gelecekteki getirinin garantisi değildir; üstelik yüksek
        geçmiş getiri çoğu zaman yüksek oynaklığın işaretidir.

        İkinci hata: **riski yalnızca "para kaybetme" olarak görmek.** Enflasyonun
        altında kalmak da bir risktir — "güvenli" görünen bir tercih, alım gücünü
        sessizce eritebilir. Risk almamak da bir risktir.

        Üçüncüsü: **kendi katlanma sınırını piyasa sakinken ölçmek.** Herkes
        düşüşte soğukkanlı kalacağını düşünür; gerçek sınav ancak düşüş
        yaşandığında olur.
        """,
        figure: "volatility-paths",
        source: """
        ## Bu bilgiler nereden geliyor?

        **Kavramlar.** Risk ve getirinin birlikte hareket etmesi finans literatürünün
        yerleşik gözlemidir. Burada matematiksel tanım yerine **sezgi** verildi:
        oynaklık, sonucun ne kadar geniş bir aralıkta savrulabildiğidir.

        **Örnek sayılar kurgusaldır.** Dersteki getiri ve dalgalanma rakamları
        anlatımı somutlaştırmak için **seçilmiştir**; geçmiş bir dönemin gerçek verisi
        ya da gelecek için bir beklenti değildir.

        **Geçmiş getiri geleceği göstermez.** Bu ders bilinçli olarak hiçbir varlığın
        gelecekteki getirisi hakkında bir şey söylemez — böyle bir cümle kurulamaz.

        **Bu bir yatırım tavsiyesi değildir.** Ders riskin ne olduğunu anlatır; ne
        kadar risk alman gerektiğini söylemez.
        """);

    // ── Ders 5 — Bileşik Getiri ──────────────────────────────────────────────

    public static IEnumerable<LessonSection> Lesson5(Guid id) => Blocks(id,
        intro: """
        ## Bu derste ne öğreneceksin?

        Bu dersi bitirdiğinde şunları yapabileceksin:

        - Bileşik getiriyi "getirinin de getiri getirmesi" olarak tanımlamak
        - Çok yıllı büyümeyi hesaplamak
        - Kazancı yeniden yatırmanın (çekmemenin) etkisini göstermek
        - Neden en güçlü değişkenin **süre** olduğunu fark etmek

        Dersin sonunda kısa bir test var.
        """,
        core: """
        ## Kazancın da kazanması

        Bileşik getiri, kazancının üzerine de kazanç binmesidir. Yalnızca ana paran
        değil, geçmiş getirilerin de çalışmaya başlar — ve bu etki zamanla hızlanır.

        100.000 ₺ yılda %20 büyürse: birinci yılın sonunda 120.000 ₺, ikinci yılın
        sonunda 144.000 ₺ olur. İkinci yılın artışı 20.000 değil **24.000 ₺**'dir;
        aradaki fark, önceki yılın kârının da çalışmasından gelir.

        Bileşik etkinin iki bileşeni vardır: **süre** ve **kazancı yeniden
        yatırmak**. İkisinden biri eksikse etki büyük ölçüde kaybolur.

        Bu yüzden bileşik getiriye çoğu zaman "zamanın armağanı" denir — en
        belirleyici değişkeni süredir.
        """,
        context: """
        ## Etki nasıl birikir?

        Bileşik büyümede değer her dönem bir önceki değerin üzerine çarpılarak
        ilerler:

        > son değer = başlangıç × (1 + getiri) üzeri dönem sayısı

        Yılda %20 ile 100.000 ₺'nin seyri: 120.000 → 144.000 → 172.800 → 207.360.
        Her yılın artışı bir öncekinden büyüktür, çünkü taban büyümektedir.

        ### Süre neden bu kadar baskın?

        Bileşik büyümede son yıllar, ilk yıllardan çok daha fazla katkı yapar.
        Yukarıdaki örnekte birinci yılın katkısı 20.000 ₺ iken dördüncü yılınki
        34.560 ₺'dir. Aynı getiri oranı, daha büyük bir taban üzerinde çalıştığı
        için daha çok üretir.

        Pratik sonucu şudur: aynı miktarı **erken** yatırmak, geç yatırmaya kıyasla
        orantısız bir fark yaratır — çünkü paranın bileşiklenecek daha çok yılı olur.

        ### Ters yönde de çalışır

        Bileşik etki tarafsızdır. Maliyetler, komisyonlar ve gider oranları da her
        yıl tabandan düşerek bileşiklenir. Yıllık küçük görünen bir gider oranı,
        uzun vadede toplam getirinin fark edilir bir kısmını götürebilir.

        ### Ve enflasyon da bileşiklenir

        Fiyatlar da bileşik olarak artar. Bu yüzden bileşik getirinin gerçek gücünü
        görmek için **reel** getiriyle düşünmek gerekir (bkz. birinci ders).
        """,
        deep: """
        ## Süre, sıra ve süreklilik

        ### Düzenlilik, büyük tutarı yenebilir

        Tek seferlik büyük bir yatırım ile küçük ama düzenli katkılar
        karşılaştırıldığında, yeterince uzun sürede düzenli katkılar çoğu zaman
        öne geçer. Sebep basittir: her yeni katkı kendi bileşiklenme süresini
        başlatır ve toplam katkı zamanla büyük tutarı aşar.

        ### Sıra riski

        Aynı ortalama getiri, farklı **sırayla** gerçekleştiğinde farklı sonuç
        verebilir — özellikle düzenli para yatırıyor veya çekiyorsan. Düşüşlerin
        erken mi geç mi yaşandığı, birikimin son değerini değiştirir. Ortalamaya
        bakmak bu etkiyi gizler.

        ### Kesintiler bileşiklenmeyi bozar

        Bileşik etkinin en kırılgan yanı sürekliliktir. Yolun ortasında çıkıp
        tekrar girmek, yalnızca o dönemin getirisini değil, o getirinin sonraki
        yıllarda üreteceği bileşik katkıyı da siler. Kaybedilen şey bir yılın
        getirisi değil, o yılın **geleceğe taşınan etkisidir**.

        ### 72 kuralı

        Bir tutarın kabaca kaç yılda ikiye katlanacağını tahmin etmek için 72'yi
        yıllık getiri oranına bölebilirsin. %12 getiride yaklaşık 6 yıl, %8'de
        yaklaşık 9 yıl. Kesin bir hesap değildir; büyüklük mertebesini hızlıca
        görmeye yarayan bir zihinsel kısayoldur.

        > Bu hesaplar geçmişe ve varsayıma dayalı örneklerdir; gelecekteki bir
        > getiriyi öngörmez veya vaat etmez.
        """,
        live: """
        ## Senin portföyünde

        Portföyünün bugünkü toplam değeri **{{total_value}}**; maliyetinin üzerine
        eklenen nominal getirin **{{return_ratio}}**.

        Bileşik etkinin bu tabloda görünmeyen kısmı **süredir**: bu getirinin kaç yıla
        yayıldığı, aynı oranın gelecekte ne üreteceğini belirleyen asıl değişkendir.
        """,
        example: """
        ## Örnek: erken başlamanın farkı

        İki kişi de yılda **%15** getiren bir birikim düşünelim (varsayım).

        - **Erken başlayan:** 10 yıl boyunca her yıl 12.000 ₺ ayırıyor, sonra hiç
          eklemiyor ve 10 yıl daha bekliyor. Toplam kendi katkısı **120.000 ₺**.
        - **Geç başlayan:** ilk 10 yıl hiç ayırmıyor, sonraki 10 yıl her yıl
          12.000 ₺ ayırıyor. Toplam kendi katkısı da **120.000 ₺**.

        Yirminci yılın sonunda erken başlayanın birikimi belirgin şekilde daha
        büyüktür — üstelik cebinden aynı parayı çıkarmışlardır. Tek fark, ilk
        kişinin parasının **bileşiklenecek daha çok yılı** olmasıdır.

        > Buradaki %15 sabit getiri gerçek bir beklenti değil, etkiyi göstermek
        > için seçilmiş bir varsayımdır.
        """,
        trap: """
        ## Sık yapılan hata

        **"Az parayla başlamanın anlamı yok, önce birikeyim."**

        Bileşik etkide en değerli girdi tutar değil **zamandır**. Beklemek, en çok
        işe yarayacak yılları harcamak demektir.

        İkinci hata: **kazancı düzenli olarak çekmek.** Getiriyi her yıl dışarı
        aldığında bileşiklenecek bir şey kalmaz; büyüme doğrusal hâle gelir ve
        dersin anlattığı etki hiç oluşmaz.

        Üçüncüsü: **küçük maliyetleri önemsiz saymak.** Yıllık ufak bir gider oranı
        da tıpkı getiri gibi bileşiklenir ve uzun vadede beklenenden büyük bir
        toplam tutar.

        Son olarak: **bileşik getiriyi bir garanti sanmak.** Formül, getirinin her
        yıl aynı olduğunu varsayar; gerçekte getiriler dalgalanır, bazı yıllar
        negatif olur. Bileşik etki bir doğa kanunu değil, sürekliliğe bağlı
        bir **mekanizmadır**.
        """,
        figure: "compound-curve",
        source: """
        ## Bu bilgiler nereden geliyor?

        **Hesap yöntemi.** Bileşik büyüme sabit bir formülle hesaplanır; uygulamadaki
        tüm parasal hesaplar **kodda**, tam hassasiyetle yapılır. Bir yapay zekâ modeli
        bu rakamları **üretmez**, yalnızca hazır sonuçları anlatır.

        **Örnek sayılar kurgusaldır.** Dersteki yıllık oranlar (%20 gibi) mekanizmayı
        göstermek için **seçilmiş yuvarlak sayılardır** — bir beklenti, hedef ya da
        gerçekleşmiş getiri değildir.

        **Enflasyon aynı mekanizmayla çalışır.** Bileşik etki paranın erimesinde de
        geçerlidir; resmî fiyat ölçümü **TÜİK** TÜFE'dir —
        [tuik.gov.tr](https://www.tuik.gov.tr).

        **Bu bir yatırım tavsiyesi değildir.** Ders zamanın etkisini anlatır; ne kadar
        süre yatırımda kalman gerektiğini söylemez.
        """);

    // ══ SET 0 — İLK ADIMLAR (sıfır bilgi) ════════════════════════════════════
    // Tam zenginlik (16 §5 S0 künyeleri): 6-10 figür/ders, çok panelli anlatı,
    // işlenmiş örnekler. Set 1'in aksine figür ANLATIM aşamalarında da olabilir
    // (16 §6.1 "her anlatım aşamasında görsel hedeftir"). Sayılar KURGUSAL/etiketli;
    // enstrüman adı YOK (soyut "A/B yolu"), tavsiye/tahmin YOK (CLAUDE.md §2).

    // ── S0-L1 — Yatırım nedir, ne değildir? ──────────────────────────────────
    public static IEnumerable<LessonSection> LessonS0L1(Guid id) => Build(id,

        Intro("""
        ## Bu derste ne öğreneceksin?

        Bu ders sıfırdan başlar — hiçbir ön bilgi gerektirmez. Bitirdiğinde
        şunları yapabileceksin:

        - **Saklamak**, **biriktirmek** ve **yatırmak** eylemlerini birbirinden ayırmak
        - Bir işin "yatırım" sayılması için gereken iki unsuru bir örnekte göstermek
        - Yatırımı bir şans oyunundan ayıran farkı — kazancın **nereden geldiğini** —
          açıklamak
        - **(İleri)** Getirinin neden var olduğunu, yani neden birileri sana para
          kazandırma sözü verir, çerçeveleyebilmek

        Dersin sonunda kısa bir test var; testi geçince bir sonraki ders açılır.
        """),

        Core("""
        ## Üç ayrı eylem: saklamak, biriktirmek, yatırmak

        Günlük dilde hepsine "para" deriz ama üç farklı şey yaparız:

        - **Saklamak** — parayı olduğu yerde tutmak (çekmece, kasa, vadesiz hesap).
          Rakam sabit kalır; ona bir şey **yapmazsın**.
        - **Biriktirmek** — geliri gideri aşınca kenara koymak. Bu bir **alışkanlık**:
          yatırılacak para buradan çıkar.
        - **Yatırmak** — biriktirdiğin parayı, **değer üretmesi umuduyla** bir
          kullanıma vermek. Karşılığında bir **getiri** beklersin, ama bu getiri
          **belirsizdir**.

        Kısaca: biriktirmek parayı **hazırlar**, yatırmak onu **çalıştırır**, saklamak
        ise sadece **bekletir**. Bu ders "çalıştırmak"ın ne demek olduğunu anlatır.
        """, "three-actions"),

        Ex("""
        ## Adım adım: 10.000 ₺'nin üç yolu

        Diyelim elinde **10.000 ₺** var (örnek bir tutar). Üç yol seçebilirsin;
        bir yıl sonra ne olduğuna bakalım:

        - **A — Çekmecede sakladın.** Bir yıl sonra hâlâ 10.000 ₺. Rakam değişmedi.
          Ama fiyatlar arttıysa, o parayla artık daha **az** şey alabilirsin.
        - **B — Vadeli hesaba koydun (örnek oran %45).** Bir yıl sonra 14.500 ₺.
          Getiri baştan **belliydi**; bankaya borç verdin, faizini aldın.
        - **C — Bir işe ortak oldun.** Sonuç **belirsizdi**: iş iyi giderse 13.000 ₺
          de olabilirdi, kötü giderse 8.000 ₺ de. Getiri **sözü** vardı, **garantisi** yok.

        Dikkat: yalnızca **B** ve **C** paraya bir iş yaptırdı. Aralarındaki fark ise
        **belirsizlik**: B'nin sonucu baştan biliniyordu, C'ninki bilinmiyordu. İşte
        "yatırım" kelimesi asıl **C** gibi durumlar için, yani sonucun önceden
        bilinmediği yerde anlamlıdır.
        """, "ten-thousand-three-paths"),

        Core("""
        ## Bir işi "yatırım" yapan iki unsur

        Yukarıdaki C'ye tekrar bak. Onu yatırım yapan iki şey vardı:

        1. **Sermaye bir kullanıma verilir.** Paran boşta durmaz; bir işin, bir
           varlığın parçası olur — orada bir şey **üretmesi** beklenir.
        2. **Getiri belirsizdir.** Ne kadar, hatta olup olmayacağı önceden bilinmez.
           Bu belirsizliğe **risk** denir.

        İki unsur birden yoksa o şey yatırım değildir: çekmecedeki para (unsur 1 yok)
        saklamaktır; sonucu baştan belli olan işlem ise (unsur 2 yok) daha çok bir
        **ödünç** ilişkisidir. Yatırım, ikisinin birlikte olduğu yerdedir.
        """, "capital-to-use"),

        Ex("""
        ## "Parayı çalıştırmak" tam olarak ne demek?

        Soyut bir laf gibi duruyor; somutlaştıralım. Bir fırına ortak olduğunu
        düşün. Verdiğin para çekmecede beklemez; şu somut şeylere **dönüşür**:

        - Un, maya, elektrik → **girdi**
        - Fırının fırını, tezgâhı → **üretim aracı**
        - Ekmek yapılır, satılır → **gelir**
        - Gelirden payına düşen → **sana dönen getiri**

        Yani "para çalışıyor" derken kastedilen budur: paran, bir değer üreten
        sürecin parçası olur. Getirin, o süreçten **üretilen** değerin bir
        parçasıdır — havadan gelmez, bir yerde bir iş yapılır.
        """, "money-at-work"),

        Trap("""
        ## Tuzak: "Yatırım = kazanç garantisi"

        En yaygın yanılgı bu. Oysa gördük ki yatırımın **tanımında** belirsizlik var
        (ikinci unsur). Getiri bir **olasılıktır**, bir **söz** değil.

        Birisi sana "yatırım" deyip **garantili** ve **yüksek** bir kazanç
        vaat ediyorsa, iki kelimeden biri yanlıştır: ya garantili değildir, ya da
        beklediğin kadar yüksek değildir. Bu ikisi aynı anda ve sürekli bir arada
        olamaz — nedenini birazdan "getiri neden var?" bölümünde göreceksin.
        """, "no-guarantee"),

        Ctx("""
        ## Yatırım, spekülasyon, şans oyunu: kazanç nereden geliyor?

        Üçü de "para koy, fazlasını al" gibi görünür ama kazancın **kaynağı** farklıdır:

        - **Yatırım** — kazanç, **üretilen değerden** gelir. Fırın ekmek üretir,
          şirket mal satar; ortaya **yeni** bir değer çıkar ve sen ondan pay alırsın.
        - **Spekülasyon** — kazanç, çoğunlukla **fiyatın kısa vadeli
          dalgalanmasından** beklenir. Değer üretilmesini beklemezsin; "ucuza alıp
          pahalıya satmayı" umarsın.
        - **Şans oyunu** — kazanç **sıfır toplamlıdır**: birinin kazandığı, tam
          olarak başkalarının kaybettiğidir. Ortada üretilen yeni bir değer yoktur.

        Kritik ayrım: yatırımda pastanın kendisi **büyüyebilir** (herkes kazanabilir);
        şans oyununda pasta sabittir, yalnızca el değiştirir.
        """, "value-vs-zero-sum"),

        ExDeep("""
        ## Aynı varlık, iki farklı davranış

        İlginç olan şu: aynı varlıkla hem yatırım hem spekülasyon yapılabilir —
        fark, **davranıştadır**.

        - **10 yıl tutmak:** Bir şirkete ortak olup yıllarca elde tutan kişi,
          şirketin **ürettiği** değerden (büyüme, kâr payı) pay alır. Bu bir
          yatırımdır.
        - **2 gün tutmak:** Aynı varlığı alıp iki gün sonra fiyatı çıktı diye satan
          kişi, üretilen değerden değil, **fiyat farkından** kazanmayı umar. Bu
          davranış spekülasyona yakındır.

        Ders sana hangisinin "doğru" olduğunu söylemez — ikisinin **farklı şeyler**
        olduğunu fark etmeni ister. Kazancının kaynağını bilmek, riskini de bilmenin
        ilk adımıdır.
        """, "hold-vs-flip"),

        Trap("""
        ## Tuzak: "Kısa vadede çok kazanan iyi yatırımcıdır"

        Bir kişi iki günde büyük kazandıysa iyi yatırımcı mıdır? Şart değil.
        **Sonuç** ile **süreç** aynı şey değildir.

        Yazı-tura atıp üst üste beş kez kazanan biri "yetenekli" değildir; sadece
        o seferlik şanslıdır. Kısa vadeli tek bir sonuç, kararın **iyi** mi yoksa
        sadece **şanslı** mı olduğunu söylemez. İyi süreç uzun vadede ortaya çıkar;
        tek bir parlak sonuç, arkasındaki riski gizleyebilir.
        """),

        Deep("""
        ## Getiri neden var? — fırsat maliyeti ve risk primi

        Peki neden birileri paranı kullanıp sana getiri sözü versin? İki nedenden:

        - **Fırsat maliyeti** — Parayı bugün onlara verirsen, o parayı bugün
          kullanmaktan **vazgeçersin**. Bu vazgeçişin bir bedeli vardır; getirinin
          bir kısmı bunun karşılığıdır.
        - **Risk primi** — Sonuç belirsizse (yatırımın ikinci unsuru), bu
          belirsizliğe katlanman için **ekstra** bir beklenti gerekir. Kimse
          karşılığında fazladan bir şey ummadan riske girmez.

        Bu yüzden güvenli bir yol (örnek %45) ile riskli bir yolun **beklenen**
        getirisi farklıdır: riskli yol, üstlendiğin belirsizlik için bir **prim**
        vaat eder. Ve tam da bu yüzden "garantili yüksek getiri" bir çelişkidir:
        garanti varsa risk yoktur, risk yoksa prim de olmaz.
        """, "risk-premium-intro"),

        Live("""
        ## Senin portföyünde

        Şu an takip ettiğin portföyde **{{holding_count}}** kalem var. Bu dersten
        sonra her birine şu gözle bakabilirsin: bu kalem bir **değer üretiminden**
        mi pay veriyor (ortaklık), yoksa bir **ödünç** ilişkisi mi (faiz), yoksa
        değerini **fiyat dalgalanmasından** mı bekliyorum?

        Sorunun cevabı, o kalemin getirisinin nereden geldiğini — ve dolayısıyla
        riskini — anlamanın ilk adımıdır.
        """),

        Src("""
        ## Bu bilgiler nereden geliyor?

        **Tanımlar.** "Yatırım", "sermaye piyasası aracı" gibi kavramların resmî
        çerçevesi **SPK**'nın (Sermaye Piyasası Kurulu) yatırımcı bilgilendirme
        yayınlarına ve **TCMB** terimler sözlüğüne dayanır —
        [spk.gov.tr](https://www.spk.gov.tr) · [tcmb.gov.tr](https://www.tcmb.gov.tr).

        **Örnek sayılar kurgusaldır.** Bu dersteki 10.000 ₺, %45 gibi rakamların
        tamamı anlatımı somutlaştırmak için **seçilmiş örneklerdir** — gerçek
        piyasa verisi değildir. Gerçek veri yalnızca "Senin portföyünde" bölümünde,
        kendi kayıtlarından gelir; oradaki sayılar da **kodda** hesaplanır, bir dil
        modeli tarafından üretilmez.

        **Bu bir yatırım tavsiyesi değildir.** Ders hiçbir varlık, ürün ya da
        davranışı önermez; "şuna ortak ol", "şunu al" demez. Yalnızca kavramların
        ne anlama geldiğini gösterir — kararlar senindir.
        """));

    // ── S0-L2 — Paranın haritası: gelir, gider, birikim ──────────────────────
    public static IEnumerable<LessonSection> LessonS0L2(Guid id) => Build(id,

        Intro("""
        ## Bu derste ne öğreneceksin?

        Yatırılacak para gökten inmez — bir yerden **artması** gerekir. Bu ders
        onun haritasını çıkarır. Bitirdiğinde şunları yapabileceksin:

        - Geliri, **zorunlu** gideri, **isteğe bağlı** gideri ve birikimi
          birbirinden ayırmak
        - Birikim oranını (birikim ÷ gelir) hesaplamak
        - Yatırılacak paranın tam olarak **nereden çıktığını** kendi cümlenle
          anlatmak
        - **(İleri)** Erken dönemde birikim oranının, getiri oranından neden daha
          güçlü bir kaldıraç olduğunu karşılaştırmak

        Dersin sonunda kısa bir test var. **Not:** bu ders sana "şu kadar biriktir"
        demez — yalnızca haritayı ve araçları verir; oran senin kararın.
        """),

        Core("""
        ## Para nereye gidiyor? Üç kova

        Her ay eline geçen paraya **gelir** diyelim. Gelir üç kovaya dağılır:

        - **Zorunlu gider** — ertelenemeyen, olmazsa olmaz harcamalar (kira,
          fatura, temel gıda).
        - **İsteğe bağlı gider** — hayatı güzelleştiren ama ertelenebilir
          harcamalar (dışarıda yemek, abonelikler, keyif).
        - **Birikim** — hiçbir kovaya gitmeyip kenara ayrılan kısım.

        Yatırım hep bu üçüncü kovadan başlar. Zorunlu ve isteğe bağlı giderler
        geliri tümüyle yerse, üçüncü kova boş kalır ve yatırılacak hiçbir şey olmaz.
        Yani yatırımın ilk adımı borsada değil, bu haritada atılır.
        """, "three-buckets"),

        Ex("""
        ## Adım adım: bir aylık dağılım

        Diyelim aylık gelirin **30.000 ₺** (örnek bir tutar). Bir ay şöyle geçti:

        - Zorunlu gider: **18.000 ₺** (kira, fatura, temel gıda)
        - İsteğe bağlı gider: **9.000 ₺** (yemek, keyif, abonelik)
        - Kalan: 30.000 − 18.000 − 9.000 = **3.000 ₺**

        Bu 3.000 ₺ senin **birikimin** — yatırıma dönüşebilecek tek kısım. Görüldüğü
        gibi 30.000 liralık gelirin yalnızca küçük bir dilimi kenara kaldı. Haritayı
        çizmek, bu dilimin nereden büyüyebileceğini de gösterir: iki gider kovasından
        biri küçülürse üçüncü kova büyür.
        """, "monthly-split"),

        Core("""
        ## Birikim oranı: tek bir sayı

        "3.000 lira" tek başına bir şey söylemez — geliri farklı iki kişide çok
        farklı anlamlar taşır. Bu yüzden mutlak tutar yerine **oran** kullanılır:

        > birikim oranı = birikim ÷ gelir

        Örnekteki kişide: 3.000 ÷ 30.000 = **0,10**, yani **%10**. Bu oran, gelirin
        ne kadarını geleceğe aktardığını tek bakışta gösterir ve farklı gelirleri
        **karşılaştırılabilir** kılar. Yüksek gelir tek başına yeterli değildir;
        önemli olan gelirin **ne kadarının** kaldığıdır.
        """, "savings-rate-bar"),

        Ex("""
        ## Adım adım: aynı gelir, iki oran

        İki kişinin de geliri aylık 30.000 ₺. Ama birikim oranları farklı:

        - **Kişi A** — %10 biriktiriyor → ayda 3.000 ₺
        - **Kişi B** — %20 biriktiriyor → ayda 6.000 ₺

        On iki ay sonra (getiriyi bir kenara bırakıp yalnız biriken tutara bakalım):

        - A: 3.000 × 12 = **36.000 ₺**
        - B: 6.000 × 12 = **72.000 ₺**

        Aynı gelir, aynı süre — ama biriken tutar **iki katı**. Daha yatırım
        yapmadan, sadece haritadaki oran değişti. İşte bu yüzden birikim oranı
        yolculuğun ilk ve en kontrol edilebilir değişkenidir.
        """, "two-savers"),

        Trap("""
        ## Tuzak: "Önce harca, kalanı biriktiririm"

        En yaygın plan budur ve en sık başarısız olandır. Sıra şöyle işler: gelir
        gelir → harcamalar yapılır → **kalanı** biriktiririm.

        Sorun şu: harcama, kendini dolduracak kadar genişleme eğilimindedir. Ay
        sonunda "kalan" çoğu zaman sıfıra yakındır — kötü niyetten değil, sıranın
        kendisinden. Biriktirmeyi **artığa** bırakmak, onu her ay en zayıf halkaya
        bağlamaktır.
        """, "leftover-trap"),

        Ctx("""
        ## Sırayı ters çevirmek: önce kendine ayır

        Tuzağın çözümü sırayı değiştirmektir: gelir gelir → **önce birikim ayrılır**
        → kalanla yaşanır. Buna genelde "önce kendine öde" denir.

        Fark psikolojiktir ama güçlüdür: birikim artık bir **artık** değil, bir
        **öncelik**tir. Kalanla yaşamak, çoğu insanda harcamayı kendiliğinden o
        çerçeveye sığdırır. Otomatik bir talimatla ayın ilk günü ayırmak, bu sırayı
        iradene bağlı olmaktan çıkarır.

        Bu bir **yöntemdir**, bir zorunluluk değil — hangi oranın sana uyduğuna
        yalnızca sen karar verirsin.
        """, "pay-yourself-first"),

        Ex("""
        ## Düzensiz gelir: oran yerine aralık

        Ya gelir her ay değişiyorsa (serbest çalışan, esnaf)? O zaman tek bir oran
        yerine bir **aralık** düşünmek daha gerçekçidir: bol aylarda daha çok, kıt
        aylarda daha az ayırmak.

        Bir yaklaşım, kıt ayları da karşılayan bir **tampon** (acil durum fonu — bir
        sonraki ders) kurup, bol ayların fazlasını oraya aktarmaktır. Böylece
        düzensiz gelir, düzenli bir birikime çevrilebilir. Buradaki amaç kesin bir
        rakam değil, dalgalanmaya dayanıklı bir **alışkanlık**.
        """),

        Trap("""
        ## Tuzak: "Zam alınca birikim kendiliğinden artar"

        Sezgi şöyle der: gelirim artarsa, biriktirdiğim de artar. Gerçekte çoğu zaman
        olan farklıdır — gelir artınca **harcama da onunla birlikte büyür** ve
        birikim oranı yerinde sayar. Buna "yaşam tarzı enflasyonu" denir.

        Yeni bir gelir geldiğinde oran kendiliğinden yükselmez; onu yükseltmek
        **bilinçli** bir karardır. Zammın bir kısmını doğrudan birikim kovasına
        yönlendirmezsen, birkaç ay içinde o para da harcamaların içinde erir.
        """, "lifestyle-creep"),

        Deep("""
        ## Oran mı, getiri mi? Erken dönemin kaldıracı

        Yeni başlayan biri iki şeyi büyütebilir: **birikim oranını** ya da yatırımın
        **getiri oranını**. Hangisi daha güçlü?

        Başlangıçta biriken tutar küçüktür. Küçük bir tutarın getirisi de küçüktür —
        %10 yerine %20 getiri, 3.000 liralık birikimde yılda yalnızca birkaç yüz
        lira fark eder. Oysa birikim oranını %10'dan %20'ye çıkarmak, biriken tutarı
        **doğrudan ikiye katlar** (örnekte 36.000 → 72.000).

        Yani yolun başında **kaldıracın oran tarafındadır**: kontrolün daha fazla,
        etkisi daha büyük. Getiri, biriken tutar büyüdükçe (ileriki yıllarda,
        bileşik etkiyle — Set 1) öne çıkar. İkisi düşman değil; sırası önemli.
        """, "rate-vs-return-lever"),

        Live("""
        ## Senin portföyünde

        Şu an portföyünün **{{cash_weight}}**'i nakitte duruyor. Nakit, henüz bir
        kovaya yerleşmemiş, esnek kısımdır: acil durum tamponu da olabilir, sıradaki
        yatırımın hammaddesi de.

        Bu dersten sonra o nakde şu soruyla bakabilirsin: bu, bilinçli ayrılmış bir
        **tampon** mu, yoksa "önce harca kalanı biriktir" tuzağından artakalan mı?
        Cevap, senin haritanın ne kadarını kontrol ettiğini gösterir.
        """),

        Src("""
        ## Bu bilgiler nereden geliyor?

        **Kavramlar evrenseldir.** Gelir, gider, birikim oranı ve "önce kendine öde"
        gibi kavramlar kişisel finans yazınının ortak dilidir; belirli bir kuruma
        değil, genel muhasebe mantığına dayanır. Birikim oranı basit bir bölme
        işlemidir (birikim ÷ gelir).

        **Örnek sayılar kurgusaldır.** 30.000 ₺, %10, %20 gibi rakamların tamamı
        anlatımı somutlaştırmak için **seçilmiş örneklerdir** — bir gelir/harcama
        önerisi değildir. Gerçek veri yalnızca "Senin portföyünde" bölümünde, kendi
        kayıtlarından gelir ve **kodda** hesaplanır.

        **Bu bir yatırım tavsiyesi değildir** — bir bütçe tavsiyesi de değildir.
        Ders hedef bir birikim oranı **dayatmaz** ("gelirinin şu kadarını biriktir"
        demez); yalnızca oranın nasıl hesaplandığını ve hangi çerçevelerin
        bulunduğunu gösterir. Karar senindir.
        """));

    // ── S0-L3 — Acil durum fonu ve borç ──────────────────────────────────────
    // 🔴 İ4: borç ödemek mi yatırım mı KARAR VERMEZ; kesin maliyet ↔ belirsiz
    // getiri asimetrisini ÇERÇEVE olarak verir (16 §5 S0-L3, CLAUDE.md §2).
    public static IEnumerable<LessonSection> LessonS0L3(Guid id) => Build(id,

        Intro("""
        ## Bu derste ne öğreneceksin?

        Yatırıma başlamadan önce iki basamak vardır: bir **tampon** kurmak ve varsa
        pahalı **borcu** anlamak. Bu dersi bitirdiğinde şunları yapabileceksin:

        - Acil durum fonunun ne işe yaradığını — varlık satmadan bir şoku
          karşılamak — açıklamak
        - Bir borcun aylık oranından **yıllık** maliyetine geçmeyi hesaplamak
        - Bir lirayı borca mı yatırıma mı vermenin **fırsat maliyeti** çerçevesini
          kurmak (karar vermeden)
        - **(İleri)** Kesin bir maliyetle belirsiz bir getirinin neden doğrudan
          karşılaştırılamayacağını göstermek

        Dersin sonunda kısa bir test var. **Not:** bu ders "borcunu öde" ya da
        "yatırım yap" demez — yalnızca çerçeveyi ve hesabı verir; karar senindir.
        """),

        Core("""
        ## Şok nedir?

        Hayat düz gitmez. Beklenmedik ve **ertelenemez** bir gider her an çıkabilir:
        bozulan bir diş, kaybolan bir iş, aniden gereken bir onarım.

        Bunlara **şok** diyelim. Şokun iki huyu vardır: ne zaman geleceği belli
        değildir ve genelde beklemez. İşte acil durum fonu — kısaca **tampon** —
        bu şokları karşılamak için önceden ayrılmış, elinin altındaki paradır.

        Tamponun amacı zengin etmek değil; bir şok geldiğinde seni **kötü bir karar
        vermek zorunda bırakmamaktır**. Nedenini birazdan göreceğiz.
        """, "shock-event"),

        Ex("""
        ## Adım adım: aynı şok, iki kişi

        Diyelim ikisinin de aniden **20.000 ₺**'lik bir gideri çıktı (örnek bir tutar).

        - **Tamponu olan kişi:** Kenarda ayrılmış parası var. Şoku oradan karşılar;
          yatırımlarına **hiç dokunmaz**. Ertesi gün hayat kaldığı yerden devam eder.
        - **Tamponu olmayan kişi:** Nakdi yok. Şoku karşılamak için elindeki bir
          varlığı **satmak zorunda** kalır — hem de fiyatı iyi olsun olmasın,
          **zamanlamayı seçemeden**. Belki de tam da satmak istemeyeceği bir anda.

        Fark, paranın miktarında değil, **hazır olup olmamasında**. Tampon, seni
        "mecburen sat" durumundan kurtaran şeydir. Yatırımın ilk koruması budur.
        """, "with-without-buffer"),

        Core("""
        ## Tamponun üç özelliği

        Her para "tampon" olamaz. İyi bir tampon üç şeyi birden taşır:

        - **Erişilebilir** — şok geldiğinde hemen ulaşabilmelisin (günler değil,
          saatler). Kilitli, vadesi dolmamış paraya tampon denmez.
        - **Oynamayan** — değeri şok anında dalgalanmamalı. Tam ihtiyacın olduğu gün
          düşmüş olabilecek bir varlık, tampon işini göremez.
        - **Ayrı** — günlük harcama hesabından ayrı dursun ki farkında olmadan
          erimesin.

        Bu üç özellik, tamponu bir **yatırımdan** ayırır. Yatırım büyümeyi hedefler;
        tampon **hazır olmayı** hedefler. İkisi farklı işler için vardır.
        """, "buffer-traits"),

        Trap("""
        ## Tuzak: "Acil durum fonu da getiri getirsin"

        Çok doğal bir istek: "Madem para duruyor, bari kazandırsın." Ama tampondan
        getiri beklemek, onu tampon yapan özelliği — **oynamamayı ve
        erişilebilirliği** — feda etmek demektir.

        Getiri peşinde tamponu dalgalı ya da kilitli bir yere koyarsan, şok tam da
        değerin düştüğü ya da paraya ulaşamadığın bir güne denk gelebilir. O anda
        elinde tampon değil, bir sorun daha olur.

        Tamponun "getirisi" para kazandırmak değildir; sana **kötü zamanlamayla
        satış yaptırmamaktır**. Bu koruma, çoğu zaman küçük bir getiriden değerlidir.
        """, "liquidity-traded-away"),

        Core("""
        ## Borcun maliyeti: aylık oran ≠ yıllık maliyet

        Bir borcun "aylık %4 faiz" gibi bir oranı olur. Küçük görünür — ama yıla
        yayıldığında çok daha büyür, çünkü faiz **faizin de üstüne** biner (bileşik).

        Kaba bir çarpım (12 × %4 = %48) gerçeği **eksik** gösterir. Doğrusu her ay
        oranın bir önceki ayın borcuna eklenmesidir: (1 + 0,04) on iki kez çarpılır.

        Aylık oran ne kadar yüksekse, kaba çarpım ile gerçek yıllık maliyet
        arasındaki fark o kadar açılır. Bir borcu değerlendirirken bakılacak sayı
        **yıllık gerçek maliyettir**, aylık küçük oran değil.
        """, "monthly-to-yearly"),

        Ex("""
        ## Adım adım: aylık %4 borç yılda ne eder?

        Aylık **%4** oranlı bir borcu ele alalım (örnek bir oran).

        - **Kaba çarpım:** 12 × %4 = **%48**. (Yanıltıcı — bileşiği yok sayar.)
        - **Gerçek (bileşik):** (1 + 0,04) her ay çarpılır → (1,04)¹² ≈ **1,60**.
          Yani yıllık maliyet yaklaşık **%60**.

        Aradaki **~12 puan**, faizin faize binmesinden gelir. Aylık oranı %5 yapsan
        fark daha da açılırdı: (1,05)¹² ≈ 1,80, yani ≈ **%80** — kaba çarpım ise
        yalnızca %60 derdi.

        Ders şunu göstermek için: küçük görünen aylık bir oran, yıllık **gerçek**
        maliyette hiç de küçük olmayabilir.
        """, "debt-cost-ladder"),

        Ctx("""
        ## Fırsat maliyeti: bir lira iki işi aynı anda yapamaz

        Elinde bir lira var. Onu ya bir borcu azaltmakta ya bir yatırımda
        kullanabilirsin — ama **ikisinde birden değil**. Birini seçmek, diğerinden
        **vazgeçmek** demektir. Vazgeçtiğin şeyin değerine **fırsat maliyeti** denir.

        Bu bir "şunu yap" kuralı değil, bir **düşünme çerçevesidir**: her lira için
        "bu para başka nerede ne yapardı?" diye sormak. Borcu azaltmak, o borcun
        yıllık maliyeti kadar bir "kaçınılmış gider" sağlar; yatırım ise belirsiz
        bir getiri **umudu** sağlar.

        Dikkat: bu ikisi aynı cinsten değil — bir sonraki adımda tam da bu farka
        bakacağız.
        """, "one-lira-two-jobs"),

        ExDeep("""
        ## Karşılaştırma çerçevesi: neyi neyle kıyaslıyorsun?

        Diyelim bir borcun yıllık gerçek maliyeti **%60** (kesin: ödemezsen bu gider
        kesinlikle işler). Bir yatırımın ise "belki %40 kazandırır, belki %10
        kaybettirir" (belirsiz).

        Bu iki sayıyı yan yana koyup "%60 > %40, demek ki borcu azaltmak daha
        mantıklı" demek **kolaycı** olur — çünkü:

        - Borcun %60'ı **kesin** bir maliyettir; olur ya da olmaz değil, **olur**.
        - Yatırımın %40'ı bir **olasılıktır**; gerçekleşebilir de, tersine dönebilir de.

        Kesin bir sayıyı belirsiz bir sayıyla doğrudan karşılaştırmak, elmayla armut
        kıyaslamaya benzer. Çerçeve şunu söyler: **önce türünü ayırt et** — kesin mi,
        olasılık mı — sonra kararı sen ver.
        """),

        Trap("""
        ## Tuzak: "Getiri faizden yüksekse borç iyidir"

        En yaygın kısayol: "Yatırım %40 kazandırır, borç %60'a mal olur — bekleyip
        yatırımdan çıkanla borcu öderim." Kulağa mantıklı gelir ama **belirsizliği
        unutur**.

        Borcun %60'ı **kesin** işler. Yatırımın %40'ı ise yalnızca bir **beklenti** —
        tersine dönerse hem yatırımdan kaybeder hem kesin borç maliyetini ödersin.
        Kesin bir gideri, gerçekleşmesi garanti olmayan bir gelirle kapatmayı
        planlamak, iki riski üst üste bindirir.

        Bu ders hangisinin doğru olduğunu **söylemez**. Yalnızca şunu hatırlatır:
        kesin bir maliyeti belirsiz bir getiriyle kıyaslarken, **belirsizliğin
        kendisi bir maliyettir** ve hesaba katılmalıdır.
        """, "certain-vs-uncertain"),

        Deep("""
        ## Nakit tutmanın iki yüzü: erime ve hazır olma

        Nakit tutmanın bir **maliyeti** vardır: enflasyon onu yavaşça eritir (bunu
        Ders 4'te göreceğiz). Bu yüzden "tüm paranı nakitte tut" iyi bir fikir
        değildir.

        Ama nakdin bir de **değeri** vardır: hemen kullanılabilir olmak, yani
        **likidite**. Şok anında satış yapmak zorunda kalmamak, kaçırılan getiriden
        daha pahalıya mal olabilir.

        Yani nakit ne tümüyle "kayıp" ne tümüyle "güvenli"dir; iki yönü birden
        taşır. Doğru soru "nakit iyi mi kötü mü?" değil, **"ne kadarı tampon için
        gerekli, ne kadarı boşta eriyor?"** sorusudur. İkisini ayırmak, haritanı
        netleştirir.
        """, "two-costs-of-cash"),

        Live("""
        ## Senin portföyünde

        Şu an portföyünün **{{cash_weight}}**'i nakitte. Bu dersten sonra o nakde iki
        soruyla bakabilirsin: ne kadarı bir şoku karşılayacak **tampon** (erişilebilir,
        oynamayan, ayrı), ne kadarı ise sadece **boşta** bekleyip yavaşça eriyen kısım?

        İki soru aynı nakde bakar ama farklı şeyler söyler: biri koruma, diğeri
        fırsat maliyeti. Ayrımı görmek, kararın senin olmasını sağlar.
        """),

        Src("""
        ## Bu bilgiler nereden geliyor?

        **Kavramlar evrenseldir.** Acil durum fonu, fırsat maliyeti ve borcun
        bileşik maliyeti kişisel finans ile temel finans matematiğinin ortak
        dilidir; belirli bir kuruma değil, aritmetiğe dayanır. Aylık orandan yıllık
        maliyete geçiş **bileşik faiz formülüdür** ((1 + i)ⁿ − 1) ve bu dersteki
        hesap **kodda** yapılır, bir dil modeli tarafından üretilmez.

        **Örnek sayılar kurgusaldır.** 20.000 ₺, aylık %4, %60 gibi rakamların
        tamamı mekanizmayı göstermek için **seçilmiş örneklerdir** — gerçek bir
        borç, faiz ya da getiri değildir. Gerçek veri yalnızca "Senin portföyünde"
        bölümünde, kendi kayıtlarından gelir.

        **Bu bir yatırım tavsiyesi değildir** — bir borç tavsiyesi de değildir.
        Ders "borcunu öde" ya da "yatırım yap" **demez**; kesin maliyet ile belirsiz
        getiriyi ayırt etmenin çerçevesini verir. Kararı — kendi sayıların ve
        durumunla — sen verirsin.
        """));

    // ── S0-L4 — Bekleyen para neden erir? ────────────────────────────────────
    // Enflasyon SEZGİSİ (formülsüz — reel getiri formülü S1-L1'e ait, §2.3).
    // 6. aşama ETKİLEŞİMLİ (enflasyon kaydırıcısı, T6.18): figür anahtarı
    // "inflation-slider" → istemcide saf/deterministik bileşen; düşerse ders
    // diğer statik figürlerle çalışır (15 §6.2, SC-E23).
    public static IEnumerable<LessonSection> LessonS0L4(Guid id) => Build(id,

        Intro("""
        ## Bu derste ne öğreneceksin?

        Para çekmecede beklerken rakamı değişmez — ama **alabildikleri** değişir.
        Bu dersi bitirdiğinde şunları yapabileceksin:

        - Enflasyonu "aynı sepetin fiyatının artması" olarak tanımlamak
        - Cüzdanındaki **tutar** ile o paranın **alım gücünü** birbirinden ayırmak
        - Fiyat endeksinin (TÜFE) ne ölçtüğünü ve neden **senin** sepetinden farklı
          olabileceğini açıklamak
        - Verilen bir oran ve süre için alım gücünün nasıl eridiğini **kaydırıcıyla**
          okumak

        Bu ders **formül içermez** — reel getiriyi hesaplamayı ileride (Set 1)
        göreceksin. Burada sezgiyi kuruyoruz. Sonunda kısa bir test var.
        """),

        Core("""
        ## Aynı sepet, iki tarih

        Bir alışveriş sepeti düşün: ekmek, süt, ulaşım, biraz da eğlence. Bu sepetin
        bir **fiyatı** vardır ve zamanla değişir.

        - **Geçen yıl:** sepet 1.000 ₺'ydi.
        - **Bu yıl:** aynı sepet — aynı ekmek, aynı süt — artık 1.400 ₺.

        Sepet değişmedi; **fiyatı** değişti. İşte **enflasyon** budur: aynı sepetin
        fiyatının zamanla artması. Örnekte bir yılda %40 artmış.

        Dikkat: burada senin paran hiç konuşmadı bile. Enflasyon bir **fiyat**
        olgusudur — parana ne olduğunu birazdan göreceğiz.
        """, "same-basket-two-dates"),

        Ex("""
        ## Adım adım: elindeki 1.000 ₺ ne alır?

        Diyelim geçen yıl elinde tam **1.000 ₺** vardı ve onu harcamayıp beklettin.

        - **Geçen yıl:** 1.000 ₺ ile sepetin **tamamını** alabilirdin (sepet 1.000 ₺).
        - **Bu yıl:** sepet 1.400 ₺ oldu. Elindeki 1.000 ₺ ile artık sepetin ancak
          **1.000 / 1.400 ≈ %71'ini** alabilirsin.

        Rakam olarak paran hâlâ 1.000 ₺ — hiç azalmadı. Ama **alabildiği** yaklaşık
        üçte bir azaldı. Beklettiğin para, sen hiçbir şey yapmadan **eridi**. İşte
        enflasyonun sana dokunduğu yer burasıdır.
        """, "basket-price-up"),

        Core("""
        ## Tutar bir şey, alım gücü başka bir şey

        Buradan iki ayrı kavram çıkar ve karıştırılmaları en yaygın hatadır:

        - **Tutar** — cüzdandaki rakam. 1.000 ₺ hep 1.000 ₺'dir; kendiliğinden
          değişmez.
        - **Alım gücü** — o parayla gerçekte kaç sepet alabildiğin. Fiyatlar
          arttıkça bu **düşer**.

        Enflasyon tutarı değil, **alım gücünü** aşındırır. Bu yüzden "param duruyor,
        kaybetmedim" hissi yanıltıcıdır: rakam durur, ama alım gücü sessizce erir.

        Yatırımın en temel gerekçelerinden biri budur — parayı, en azından bu
        erimeye karşı **çalıştırmak**. Ama bu ders bir çözüm önermez; önce **sorunu**
        net görmek gerekir.
        """, "amount-vs-power"),

        Trap("""
        ## Tuzak: "Param aynı kaldı, demek ki kaybetmedim"

        En sinsi yanılgı budur çünkü rakam seni haklı çıkarır gibi görünür: hesabında
        hâlâ aynı sayı yazar. Ama "kayıp" sadece rakamın küçülmesi değildir.

        Alım gücü düştüyse, o parayla artık daha az şey alabiliyorsun demektir — bu
        da bir kayıptır, sadece **görünmez** bir kayıp. Enflasyon, hırsız gibi
        kapıyı kırmaz; cüzdanın içinde, fark etmeden çalışır.

        Doğru soru "rakamım aynı mı?" değil, **"bu parayla hâlâ aynı şeyleri alabiliyor
        muyum?"** sorusudur.
        """, "standing-still"),

        Ex("""
        ## Kendin dene: enflasyon kaydırıcısı

        Aşağıdaki araçta iki şeyi oynatabilirsin: yıllık enflasyon **oranı** ve kaç
        **yıl** beklendiği. Bugünkü 100 ₺'nin alım gücünün, o oran **olursa** kaç
        yılda ne kadar eriyeceğini canlı görürsün.

        Birkaç şeyi dene: oranı yükselt — erime hızlanır. Süreyi uzat — erime derinleşir.
        Küçük görünen bir oran bile, yıllar üst üste binince alım gücünü ciddi biçimde
        düşürür.

        **Not:** araç bir **tahmin** değildir; "şu oran **olursa** ne olur" der,
        "şu oran olacak" demez. Sayılar senin oynattığın varsayımlardır.
        """, "inflation-slider"),

        Ctx("""
        ## Fiyat endeksi (TÜFE): sepet nasıl kurulur?

        Bir ülkede milyonlarca farklı fiyat var. "Enflasyon %40" derken hangi
        fiyat? İşte burada **fiyat endeksi** devreye girer.

        İstatistik kurumu (Türkiye'de **TÜİK**) tipik bir hanenin aldığı mal ve
        hizmetlerden bir **sepet** oluşturur — gıda, konut, ulaşım, giyim… — ve her
        kalemin ne kadar ağırlık taşıdığını belirler. Sonra bu sepetin fiyatının
        zaman içindeki değişimini ölçer. **TÜFE** (Tüketici Fiyat Endeksi) bu
        ölçümün adıdır.

        Yani "resmî enflasyon", bu **ortalama sepetin** fiyat değişimidir. Ortalama
        kelimesi önemli — bir sonraki adımda nedenini göreceğiz.
        """, "index-basket"),

        ExDeep("""
        ## Kişisel sepet: neden herkesin enflasyonu farklı?

        TÜFE **ortalama** bir sepeti ölçer. Ama senin sepetin ortalamadan farklıysa,
        **hissettiğin** enflasyon da farklı olur.

        - **Kirada oturan biri:** bütçesinin büyük kısmı kira. Kiralar hızlı arttıysa,
          onun kişisel enflasyonu resmî ortalamanın **üstünde** olur.
        - **Ev sahibi biri:** kira ödemez; onun sepetinde konutun ağırlığı düşük.
          Aynı dönemde onun enflasyonu ortalamanın **altında** kalabilir.

        İkisi de aynı ülkede, aynı yıl yaşıyor — ama farklı sepetler farklı sonuçlar
        verir. TÜFE yanlış değildir; **ortalamadır**. Kendi enflasyonun, kendi
        harcama sepetine bağlıdır.
        """, "personal-basket"),

        Trap("""
        ## Tuzak: "Resmî oran benim enflasyonum"

        Resmî TÜFE'yi duyup "demek benim param da tam bu kadar eridi" demek doğal ama
        eksiktir. Az önce gördük: resmî oran **ortalama** sepetin oranıdır.

        Senin harcamaların ortalamadan farklıysa — çok kira ödüyorsan, ya da geliri
        ağırlıkla belirli bir kaleme gidiyorsa — hissettiğin erime resmî orandan
        yukarı ya da aşağı sapabilir. Ayrıca enflasyon **her kalemi eşit etkilemez**:
        bazı fiyatlar hızlı, bazıları yavaş artar.

        Resmî oran iyi bir **pusuladır**, ama senin tam adresin değildir. Kendi
        sepetine bakmak, haritanı netleştirir.
        """),

        Deep("""
        ## Bileşik erime: yıllar üst üste binince

        Enflasyon tek yılda küçük görünebilir — ama etkisi **üst üste biner**, tıpkı
        borcun bileşik maliyeti gibi (önceki ders). İkinci yılın erimesi, birinci
        yıldan **kalan** alım gücünün üzerine işler.

        Örneğin yıllık %40 erimeyle: bir yıl sonra 100 ₺'nin alım gücü ≈ 71 ₺, iki
        yıl sonra ≈ 51 ₺, üç yıl sonra ≈ 36 ₺. Her yıl aynı **oran**, ama azalan bir
        tabana uygulandığı için alım gücü giderek hızlanan biçimde düşer.

        Bu yüzden "yılda sadece biraz" diye küçümsenen bir oran, birkaç yılda alım
        gücünün önemli bir kısmını götürebilir. Zaman, enflasyonun tarafındadır.
        """, "compounded-erosion"),

        ExDeep("""
        ## Adım adım: alım gücü kaç yılda yarıya iner?

        "Alım gücüm ne zaman yarıya iner?" sorusunun kaba bir cevabı vardır. Yıllık
        erime oranını **70'e böl**, yaklaşık yıl sayısını bulursun (bu, bileşik
        büyümenin bilinen bir kısayoludur).

        - **%10 erimeyle:** 70 / 10 ≈ **7 yıl**.
        - **%35 erimeyle:** 70 / 35 ≈ **2 yıl**.
        - **%70 erimeyle:** 70 / 70 ≈ **1 yıl**.

        Oran yükseldikçe "yarıya inme" süresi hızla kısalır. Kaydırıcıda da bunu
        görebilirsin: yüksek oranda çubuk çok daha çabuk çöker. Bu bir tahmin değil,
        verilen oranın **matematiksel** sonucudur.
        """),

        Live("""
        ## Senin portföyünde

        Şu an portföyünün **{{cash_weight}}**'i nakitte bekliyor. Bu ders tam da o
        nakitle ilgili: rakamı sabit görünse de, alım gücü enflasyon oranında
        sessizce erir.

        Bu, "nakit kötü" demek değildir — bir önceki derste gördük ki nakit aynı
        zamanda bir **tampon** ve **likidite** kaynağıdır. Ama boşta, ihtiyaç
        fazlası bekleyen nakit için erime gerçek bir maliyettir. Ne kadarı tampon,
        ne kadarı boşta — ayrımı yine sen yaparsın.
        """),

        Src("""
        ## Bu bilgiler nereden geliyor?

        **Fiyat endeksi tanımı.** Tüketici Fiyat Endeksi (TÜFE), sepet kapsamı ve
        yayın takvimi **TÜİK** (Türkiye İstatistik Kurumu) tarafından belirlenir —
        [tuik.gov.tr](https://www.tuik.gov.tr). Enflasyon kavramının çerçevesi bu
        resmî ölçüme dayanır.

        **Örnek sayılar kurgusaldır.** 1.000 ₺, 1.400 ₺, %40 gibi rakamların tamamı
        sezgiyi kurmak için **seçilmiş örneklerdir** — gerçek bir dönemin TÜFE'si
        değildir. Kaydırıcıdaki değerler de senin oynattığın **varsayımlardır**;
        hesap istemcide, **kodda** yapılır (bileşik erime), bir dil modeli üretmez.

        **Bu bir yatırım tavsiyesi değildir** — bir gelecek tahmini de değildir.
        Enflasyon burada **geçmiş/olası** bir olgu olarak anlatılır; "enflasyon şu
        olacak" ya da "şuna yatır" **denmez**. Kaydırıcı "şu oran **olursa**" der.
        Karar senindir.
        """));

    // ── 2-5. derslerin mini testleri (T6.1) ──────────────────────────────────
    // Ders 1'inki T5E.2'de geldi. Her soruda eğitici `Explanation` var; doğru şık
    // ve açıklama YALNIZCA deneme sonucunda açılır (T5E.3 sözleşmesi).

    internal sealed record SeedQuestion(
        QuizQuestionType Type,
        QuizDifficulty Difficulty,
        string Prompt,
        string Explanation,
        (string Text, bool IsCorrect)[] Options);

    public static IEnumerable<(string LessonKey, string QuizKey, string Title, SeedQuestion[] Questions)>
        RemainingQuizzes()
    {
        // ── Set 0 · Ders 1 — Yatırım nedir, ne değildir? (9 soru / 3 zorluk) ──
        yield return ("lesson-s0l1", "quiz-s0l1", "Yatırım Nedir — Mini Test",
        [
            // ── Kolay: kavramı tanıyor mu? ───────────────────────────────────
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Easy,
                "Parayı vadesiz hesapta hiç dokunmadan bekletmek hangi eylemdir?",
                "Paraya bir iş yaptırmıyorsun, sadece bekletiyorsun — bu saklamaktır. Yatırmak, paranın " +
                "değer üretmesi umuduyla bir kullanıma verilmesidir; biriktirmek ise yatırılacak parayı hazırlamaktır.",
                [("Yatırmak", false), ("Saklamak", true), ("Biriktirmek", false), ("Spekülasyon", false)]),

            new SeedQuestion(QuizQuestionType.TrueFalse, QuizDifficulty.Easy,
                "Bir işin \"yatırım\" sayılması için getirisinin baştan garanti olması gerekir.",
                "Tam tersi: yatırımın tanımında belirsizlik (risk) vardır. Getiri bir olasılıktır, bir söz değil. " +
                "Sonucu baştan belli olan işlem daha çok bir ödünç ilişkisidir.",
                [("Doğru", false), ("Yanlış", true)]),

            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Easy,
                "Yatırımda kazanç esas olarak nereden gelir?",
                "Yatırımda kazanç, üretilen değerden gelir — şirket mal satar, fırın ekmek üretir, ortaya yeni " +
                "bir değer çıkar ve sen ondan pay alırsın. Şans oyununda ise ortada üretilen yeni bir değer yoktur.",
                [("Başkalarının kaybından", false),
                 ("Üretilen değerden alınan paydan", true),
                 ("Sadece fiyatın rastgele oynamasından", false),
                 ("Bankanın verdiği hediyeden", false)]),

            // ── Orta: kavramı kullanabiliyor mu? ─────────────────────────────
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Medium,
                "Bir kişi bir varlığı sabah alıp öğleden sonra fiyatı çıktı diye satıyor. Bu davranış neye yakındır?",
                "Kazancı üretilen değerden değil, kısa vadeli fiyat farkından bekliyor — bu spekülasyona yakındır. " +
                "Aynı varlıkla yıllarca ortak kalıp üretilen değerden pay almak ise yatırıma yakın olurdu. Fark davranıştadır.",
                [("Yatırıma — uzun vadeli değer üretimi", false),
                 ("Spekülasyona — fiyat farkından kazanç beklentisi", true),
                 ("Saklamaya — paraya dokunmama", false),
                 ("Biriktirmeye — kenara koyma", false)]),

            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Medium,
                "Hangi durumda bir iş \"yatırım\"ın iki unsurunu birden taşır?",
                "Yatırımın iki unsuru: (1) sermaye bir kullanıma verilir, (2) getiri belirsizdir. Çekmecedeki para " +
                "birinci unsuru taşımaz; sonucu baştan belli işlem ikinciyi taşımaz. İkisi birlikte olduğunda yatırımdır.",
                [("Para çekmecede duruyor, değeri sabit", false),
                 ("Para bir işe konmuş ve sonuç belirsiz", true),
                 ("Para vadeli hesapta, getiri baştan belli", false),
                 ("Para hiç harcanmadan bekletiliyor", false)]),

            new SeedQuestion(QuizQuestionType.TrueFalse, QuizDifficulty.Medium,
                "Şans oyununda toplam kazanç ile toplam kayıp birbirine eşittir (sıfır toplamlı).",
                "Doğru. Şans oyununda birinin kazandığı, tam olarak başkalarının kaybettiğidir; ortada üretilen yeni " +
                "değer yoktur, pasta sabittir. Yatırımda ise pasta büyüyebilir — herkes birden kazanabilir.",
                [("Doğru", true), ("Yanlış", false)]),

            // ── Zor: kavramı başka bağlama taşıyabiliyor mu? ─────────────────
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Hard,
                "Bir ürün \"garantili ve yıllık %90 getiri\" diye pazarlanıyor. Buradaki mantık hatası nedir?",
                "Getiri bir risk primi içerir: belirsizliğe katlanana verilen ekstra. Garanti varsa risk yoktur, " +
                "risk yoksa yüksek prim de olmaz. \"Garantili\" ve \"çok yüksek\" aynı anda sürekli var olamaz — " +
                "biri gerçek değildir. Bu, dolandırıcılığın klasik işaretidir.",
                [("Hiçbir hata yok, mümkündür", false),
                 ("Garanti ile yüksek getiri aynı anda olamaz — risk primi çelişkisi", true),
                 ("Sadece oran biraz yüksek, gerisi doğru", false),
                 ("Getiri aylık verilseydi doğru olurdu", false)]),

            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Hard,
                "İki günde büyük kazanan biri için hangisi doğrudur?",
                "Kısa vadeli tek bir sonuç, kararın iyi mi yoksa sadece şanslı mı olduğunu söylemez — sonuç ile süreç " +
                "aynı şey değildir. Yazı-turada üst üste kazanmak yetenek değildir. İyi süreç uzun vadede belli olur.",
                [("Kesinlikle yetenekli bir yatırımcıdır", false),
                 ("Tek sonuç sürecin iyi mi şanslı mı olduğunu göstermez", true),
                 ("Yöntemi herkes kopyalamalıdır", false),
                 ("Bundan sonra hep kazanır", false)]),

            new SeedQuestion(QuizQuestionType.MultipleChoice, QuizDifficulty.Hard,
                "Aşağıdakilerden hangileri getirinin VAR OLMA sebebidir? (birden fazla)",
                "Getiri iki şeyin karşılığıdır: parayı bugün kullanmaktan vazgeçmek (fırsat maliyeti) ve belirsizliğe " +
                "katlanmak (risk primi). \"Bankanın cömertliği\" ya da \"paranın kendiliğinden çoğalması\" gerçek " +
                "sebepler değildir — getirinin arkasında hep bir vazgeçiş ve bir risk vardır.",
                [("Fırsat maliyeti — bugün kullanmaktan vazgeçmek", true),
                 ("Risk primi — belirsizliğe katlanmak", true),
                 ("Paranın kendiliğinden çoğalması", false),
                 ("Bankanın cömertliği", false)]),
        ]);

        // ── Set 0 · Ders 2 — Paranın haritası (9 soru / 3 zorluk) ────────────
        yield return ("lesson-s0l2", "quiz-s0l2", "Paranın Haritası — Mini Test",
        [
            // ── Kolay ────────────────────────────────────────────────────────
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Easy,
                "Kira ve fatura hangi kovaya girer?",
                "Kira ve fatura ertelenemeyen, olmazsa olmaz harcamalardır — zorunlu gider. İsteğe bağlı gider " +
                "ertelenebilen keyif harcamalarıdır; birikim ise hiçbir kovaya gitmeyip kenara ayrılan kısımdır.",
                [("İsteğe bağlı gider", false), ("Zorunlu gider", true), ("Birikim", false), ("Gelir", false)]),

            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Easy,
                "Birikim oranı nasıl hesaplanır?",
                "Birikim oranı = birikim ÷ gelir. Mutlak tutar (\"3.000 lira\") tek başına anlam taşımaz; oran, " +
                "gelirin ne kadarının geleceğe aktarıldığını gösterir ve farklı gelirleri karşılaştırılabilir kılar.",
                [("Gelir ÷ gider", false),
                 ("Birikim ÷ gelir", true),
                 ("Gider ÷ birikim", false),
                 ("Zorunlu gider ÷ gelir", false)]),

            new SeedQuestion(QuizQuestionType.TrueFalse, QuizDifficulty.Easy,
                "Yatırılacak para, gelir-gider haritasındaki birikim kovasından çıkar.",
                "Doğru. Yatırım hep üçüncü kovadan (birikim) başlar. Zorunlu ve isteğe bağlı giderler geliri " +
                "tümüyle yerse birikim kovası boş kalır ve yatırılacak hiçbir şey olmaz.",
                [("Doğru", true), ("Yanlış", false)]),

            // ── Orta ─────────────────────────────────────────────────────────
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Medium,
                "Gelir 40.000 ₺, zorunlu gider 24.000 ₺, isteğe bağlı gider 10.000 ₺. Birikim oranı nedir?",
                "Birikim = 40.000 − 24.000 − 10.000 = 6.000 ₺. Oran = 6.000 ÷ 40.000 = 0,15, yani %15. " +
                "Önce birikim tutarı bulunur, sonra gelire bölünür.",
                [("%10", false), ("%15", true), ("%24", false), ("%40", false)]),

            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Medium,
                "İki kişinin de geliri 30.000 ₺. A %10, B %20 biriktiriyor. 12 ay sonra (getiri hariç) " +
                "biriken tutarlar nasıl olur?",
                "A: 3.000 × 12 = 36.000 ₺; B: 6.000 × 12 = 72.000 ₺ — tam iki katı. Aynı gelir ve süreyle, " +
                "yalnızca oran farkı biriken tutarı ikiye katlıyor. Henüz hiç yatırım getirisi işin içinde değil.",
                [("İkisi de yaklaşık aynı birikir", false),
                 ("A 36.000, B 72.000 — B iki katı", true),
                 ("A 36.000, B 54.000", false),
                 ("Oran fark etmez, tutarlar eşit", false)]),

            new SeedQuestion(QuizQuestionType.TrueFalse, QuizDifficulty.Medium,
                "Gelir artınca birikim oranı kendiliğinden yükselir.",
                "Yükselmez. Gelir artınca harcama da onunla büyüme eğilimindedir (yaşam tarzı enflasyonu) ve oran " +
                "yerinde sayabilir. Oranı yükseltmek — örneğin zammın bir kısmını doğrudan birikime yönlendirmek — " +
                "bilinçli bir karardır.",
                [("Doğru", false), ("Yanlış", true)]),

            // ── Zor ──────────────────────────────────────────────────────────
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Hard,
                "Yolun başında biriken tutar küçükken hangi kaldıraç daha güçlüdür?",
                "Başlangıçta biriken tutar küçük olduğu için getiri farkının (ör. %10→%20) mutlak etkisi küçüktür. " +
                "Oysa birikim oranını %10'dan %20'ye çıkarmak biriken tutarı DOĞRUDAN ikiye katlar. Erken dönemde " +
                "kaldıraç oran tarafındadır; getiri, tutar büyüdükçe (bileşik etkiyle) öne çıkar.",
                [("Getiri oranı — küçük tutarda bile belirleyicidir", false),
                 ("Birikim oranı — tutarı doğrudan büyütür, kontrol daha fazla", true),
                 ("İkisi de aynı etkiyi yapar", false),
                 ("Hiçbiri; başlangıçta ikisi de önemsizdir", false)]),

            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Hard,
                "\"Önce harca, kalanı biriktir\" planı neden sık başarısız olur?",
                "Harcama, kendini dolduracak kadar genişleme eğilimindedir; biriktirmeyi \"kalan\"a bırakmak onu her ay " +
                "en zayıf halkaya bağlar ve ay sonunda kalan çoğu zaman sıfıra yakındır. Çözüm sırayı ters çevirmektir: " +
                "önce birikimi ayırıp kalanla yaşamak.",
                [("Faizler düşük olduğu için", false),
                 ("Harcama genişleyip \"kalan\"ı sıfıra yaklaştırdığı için", true),
                 ("Gelir her zaman yetersiz olduğu için", false),
                 ("Bankalar biriktirmeye izin vermediği için", false)]),

            new SeedQuestion(QuizQuestionType.MultipleChoice, QuizDifficulty.Hard,
                "Düzensiz geliri (serbest çalışan) düzenli birikime çevirmek için hangileri işe yarar? (birden fazla)",
                "Tek bir sabit oran yerine bol/kıt aylara göre bir aralık düşünmek ve kıt ayları karşılayan bir tampon " +
                "(acil durum fonu) kurup bol ayların fazlasını oraya aktarmak, dalgalanmayı düzenli birikime çevirir. " +
                "\"Gelir düzenli olana kadar hiç biriktirmemek\" ise fırsatı tümden kaçırır.",
                [("Bol/kıt aylara göre bir aralık belirlemek", true),
                 ("Kıt ayları karşılayan bir tampon kurmak", true),
                 ("Bol ayların fazlasını tampona aktarmak", true),
                 ("Gelir düzenli olana kadar hiç biriktirmemek", false)]),
        ]);

        // ── Set 0 · Ders 3 — Acil durum fonu ve borç (9 soru / 3 zorluk) ─────
        yield return ("lesson-s0l3", "quiz-s0l3", "Acil Durum Fonu ve Borç — Mini Test",
        [
            // ── Kolay ────────────────────────────────────────────────────────
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Easy,
                "Acil durum fonunun (tampon) temel amacı nedir?",
                "Tamponun amacı zengin etmek değil, bir şok geldiğinde seni varlık satmak zorunda BIRAKMAMAKTIR. " +
                "Böylece kötü bir zamanlamada satış yapmak zorunda kalmazsın. Getiri onun işi değildir.",
                [("Mümkün olan en yüksek getiriyi sağlamak", false),
                 ("Bir şoku, varlık satmadan karşılayabilmek", true),
                 ("Vergi avantajı elde etmek", false),
                 ("Enflasyondan tamamen korunmak", false)]),

            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Easy,
                "İyi bir tamponun taşıması gereken üç özellik hangisidir?",
                "İyi bir tampon erişilebilir (hemen ulaşılır), oynamayan (değeri şok anında dalgalanmaz) ve " +
                "ayrı (günlük harcamadan ayrı) olmalıdır. Bu üç özellik onu bir yatırımdan ayırır: yatırım büyümeyi, " +
                "tampon hazır olmayı hedefler.",
                [("Yüksek getirili, kilitli, riskli", false),
                 ("Erişilebilir, oynamayan, ayrı", true),
                 ("Uzun vadeli, dalgalı, gizli", false),
                 ("Büyük, karmaşık, çeşitlendirilmiş", false)]),

            new SeedQuestion(QuizQuestionType.TrueFalse, QuizDifficulty.Easy,
                "Aylık %4 faizli bir borç yılda kabaca 12 × %4 = %48'e mal olur.",
                "Yanlış — bu kaba çarpım bileşiği yok sayar. Gerçekte her ay oran bir önceki borcun üstüne biner: " +
                "(1,04)¹² ≈ 1,60, yani yıllık maliyet yaklaşık %60'tır. Kaba çarpım gerçeği eksik gösterir.",
                [("Doğru", false), ("Yanlış", true)]),

            // ── Orta ─────────────────────────────────────────────────────────
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Medium,
                "Aylık %4 oranlı bir borcun yaklaşık YILLIK gerçek maliyeti nedir?",
                "Aylık oran her ay bileşiklenir: (1 + 0,04)¹² ≈ 1,60 → yaklaşık %60. Kaba çarpım (%48) ise bileşiği " +
                "atladığı için ~12 puan eksik gösterir. Aylık oran yükseldikçe bu fark açılır.",
                [("Yaklaşık %48 — 12 × %4", false),
                 ("Yaklaşık %60 — (1,04)¹² bileşik", true),
                 ("Tam %4 — yıl boyu sabit", false),
                 ("Yaklaşık %24 — yılda iki kez", false)]),

            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Medium,
                "Tamponunu dalgalı, değeri oynayan bir yere koyarsan hangi risk doğar?",
                "Getiri peşinde tamponun oynamama özelliğini feda edersin. Şok tam da değerin düştüğü bir güne denk " +
                "gelirse, elinde tampon değil bir sorun daha olur — koruma amacı boşa gider.",
                [("Hiçbir risk — getiri her zaman iyidir", false),
                 ("Şok anında değeri düşük olabilir ve koruma işlevini göremez", true),
                 ("Vergi artar", false),
                 ("Borç faizi yükselir", false)]),

            new SeedQuestion(QuizQuestionType.TrueFalse, QuizDifficulty.Medium,
                "Bir lirayı hem borcu azaltmakta hem yatırımda AYNI ANDA kullanabilirsin.",
                "Kullanamazsın. Birini seçmek diğerinden vazgeçmektir; vazgeçtiğinin değerine fırsat maliyeti denir. " +
                "Bu, kararın kendisi değil, kararı düşünmenin çerçevesidir.",
                [("Doğru", false), ("Yanlış", true)]),

            // ── Zor ──────────────────────────────────────────────────────────
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Hard,
                "Borcun yıllık KESİN maliyeti %60, bir yatırımın BELİRSİZ beklenen getirisi %40. " +
                "\"%60 > %40, öyleyse borcu azaltmak kesin daha iyi\" çıkarımının sorunu nedir?",
                "İki sayı aynı cinsten değil: borcun %60'ı KESİN (olur), yatırımın %40'ı bir OLASILIK (olabilir de " +
                "olmaz da). Kesin bir maliyeti belirsiz bir getiriyle doğrudan kıyaslamak yanıltıcıdır. Ders hangisinin " +
                "doğru olduğunu söylemez; önce türü (kesin mi, olasılık mı) ayırt etmeni ister.",
                [("Hiçbir sorun yok, çıkarım doğru", false),
                 ("Kesin bir maliyet ile belirsiz bir getiri doğrudan kıyaslanıyor", true),
                 ("Yüzdeler yanlış hesaplanmış", false),
                 ("Borç her zaman iyidir", false)]),

            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Hard,
                "\"Yatırımdan çıkanla sonra borcu öderim\" planının gizli riski nedir?",
                "Borcun maliyeti KESİN işler; yatırımın getirisi ise yalnızca bir beklentidir. Yatırım tersine dönerse " +
                "hem yatırımdan kaybeder hem kesin borç maliyetini ödersin — iki risk üst üste biner. Belirsizliğin " +
                "kendisi bir maliyettir ve hesaba katılmalıdır.",
                [("Risk yok — plan garantili", false),
                 ("Yatırım tersine dönerse hem kayıp hem kesin borç maliyeti üst üste biner", true),
                 ("Borç kendiliğinden kapanır", false),
                 ("Enflasyon borcu eritir", false)]),

            new SeedQuestion(QuizQuestionType.MultipleChoice, QuizDifficulty.Hard,
                "Nakit tutmak için aşağıdakilerden hangileri DOĞRUDUR? (birden fazla)",
                "Nakdin iki yüzü vardır: enflasyon onu yavaşça eritir (maliyet) AMA hemen kullanılabilir olması " +
                "(likidite) şok anında satış yapmaktan kurtarır (değer). Bu yüzden \"tümüyle kayıp\" da \"tümüyle " +
                "güvenli\" de değildir; doğru soru ne kadarının tampon, ne kadarının boşta olduğudur.",
                [("Enflasyon nakdi zamanla eritir (bir maliyet)", true),
                 ("Likidite şok anında satış zorunluluğundan kurtarır (bir değer)", true),
                 ("Bir kısmı tampon, bir kısmı boşta olabilir — ayırmak gerekir", true),
                 ("Tüm parayı nakitte tutmak en iyisidir", false)]),
        ]);

        // ── Set 0 · Ders 4 — Bekleyen para neden erir? (9 soru / 3 zorluk) ───
        yield return ("lesson-s0l4", "quiz-s0l4", "Bekleyen Para Neden Erir? — Mini Test",
        [
            // ── Kolay ────────────────────────────────────────────────────────
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Easy,
                "Enflasyon en iyi nasıl tanımlanır?",
                "Enflasyon, aynı sepetin (aynı mal ve hizmetlerin) fiyatının zamanla artmasıdır. Sepet değişmez; " +
                "fiyatı değişir. Bu bir fiyat olgusudur — paranın kendisiyle değil, fiyatlarla ilgilidir.",
                [("Paranın hesapta kendiliğinden çoğalması", false),
                 ("Aynı sepetin fiyatının zamanla artması", true),
                 ("Döviz kurunun yükselmesi", false),
                 ("Maaşların artması", false)]),

            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Easy,
                "\"Tutar\" ile \"alım gücü\" arasındaki fark nedir?",
                "Tutar cüzdandaki rakamdır (1.000 ₺ hep 1.000 ₺). Alım gücü ise o parayla gerçekte kaç şey " +
                "alabildiğindir. Enflasyon tutarı değiştirmez ama alım gücünü düşürür.",
                [("İkisi aynı şeydir", false),
                 ("Tutar rakam, alım gücü o parayla alınabilen şeydir", true),
                 ("Tutar gelecekteki değer, alım gücü bugünküdür", false),
                 ("Alım gücü sadece dövizde geçerlidir", false)]),

            new SeedQuestion(QuizQuestionType.TrueFalse, QuizDifficulty.Easy,
                "Hesabındaki rakam aynı kaldıysa, enflasyona rağmen hiçbir şey kaybetmemişsindir.",
                "Yanlış. Rakam aynı kalsa bile fiyatlar arttıysa o parayla artık daha az şey alabilirsin — bu " +
                "görünmez bir kayıptır. Doğru soru \"rakamım aynı mı?\" değil, \"aynı şeyleri hâlâ alabiliyor muyum?\"dur.",
                [("Doğru", false), ("Yanlış", true)]),

            // ── Orta ─────────────────────────────────────────────────────────
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Medium,
                "Geçen yıl 1.000 ₺ olan sepet bu yıl 1.400 ₺ oldu. Elindeki 1.000 ₺ ile bu yıl sepetin ne kadarını alırsın?",
                "1.000 / 1.400 ≈ 0,71, yani sepetin yaklaşık %71'ini. Paran rakam olarak azalmadı ama alabildiği " +
                "yaklaşık üçte bir düştü — beklettiğin para sen hiçbir şey yapmadan eridi.",
                [("Tamamını (%100)", false),
                 ("Yaklaşık %71'ini", true),
                 ("Yaklaşık %140'ını", false),
                 ("Yaklaşık %40'ını", false)]),

            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Medium,
                "Tüketici Fiyat Endeksi (TÜFE) neyi ölçer?",
                "TÜFE, tipik bir hanenin aldığı mal ve hizmetlerden oluşan ORTALAMA bir sepetin fiyatının zaman " +
                "içindeki değişimini ölçer. \"Resmî enflasyon\" bu ortalama sepetin oranıdır — tek bir ürünün değil.",
                [("Sadece altının fiyatını", false),
                 ("Ortalama bir tüketim sepetinin fiyat değişimini", true),
                 ("Bankaların faiz oranını", false),
                 ("Döviz kurunu", false)]),

            new SeedQuestion(QuizQuestionType.TrueFalse, QuizDifficulty.Medium,
                "Resmî TÜFE oranı herkesin hissettiği enflasyonu tam olarak yansıtır.",
                "Yansıtmaz. TÜFE ortalama bir sepeti ölçer; senin sepetin ortalamadan farklıysa (örneğin çok kira " +
                "ödüyorsan) hissettiğin enflasyon sapabilir. Resmî oran iyi bir pusuladır ama tam adresin değildir.",
                [("Doğru", false), ("Yanlış", true)]),

            // ── Zor ──────────────────────────────────────────────────────────
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Hard,
                "İkisinin de nominal geliri aynı ama biri kirada, diğeri ev sahibi. Enflasyon ikisini neden farklı etkiler?",
                "Herkesin sepeti farklıdır. Kiracının bütçesinde kira büyük ağırlık taşır; kiralar hızlı arttıysa " +
                "kişisel enflasyonu ortalamanın ÜSTÜNDE olur. Ev sahibi kira ödemez, konut ağırlığı düşüktür; " +
                "enflasyonu ortalamanın ALTINDA kalabilir. TÜFE yanlış değil, ortalamadır.",
                [("Enflasyon herkesi tam olarak eşit etkiler", false),
                 ("Farklı harcama sepetleri farklı kişisel enflasyon verir", true),
                 ("Sadece gelir farkı önemlidir", false),
                 ("Ev sahibi enflasyondan hiç etkilenmez", false)]),

            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Hard,
                "Yıllık %40 enflasyonla 100 ₺'nin alım gücü: 1 yıl sonra ≈71 ₺, 2 yıl sonra ≈51 ₺, 3 yıl sonra ≈36 ₺. " +
                "Bu neyi gösterir?",
                "Erime bileşiktir: her yıl aynı ORAN, ama azalan bir tabana uygulanır. Bu yüzden alım gücü doğrusal " +
                "değil, giderek hızlanan biçimde düşer. \"Yılda sadece biraz\" diye küçümsenen oran, birkaç yılda " +
                "alım gücünün önemli kısmını götürür — zaman enflasyonun tarafındadır.",
                [("Erime her yıl eşit miktarda (doğrusal) olur", false),
                 ("Erime bileşiktir; azalan tabana binerek hızlanır", true),
                 ("Üç yıl sonra alım gücü artmaya başlar", false),
                 ("Enflasyon ikinci yıl durur", false)]),

            new SeedQuestion(QuizQuestionType.MultipleChoice, QuizDifficulty.Hard,
                "Enflasyon kaydırıcısı hakkında hangileri DOĞRUDUR? (birden fazla)",
                "Araç bir tahmin değildir: \"şu oran OLURSA ne olur\" der, \"şu oran olacak\" demez. Hesap istemcide " +
                "saf ve deterministiktir (aynı girdi → aynı çıktı) ve varsayımsaldır. Gelecekteki gerçek enflasyonu " +
                "bilmez ve bir varlık önermez.",
                [("\"Şu oran olursa\" der, gelecek tahmini yapmaz", true),
                 ("Değerler senin oynattığın varsayımlardır", true),
                 ("Aynı girdi her zaman aynı sonucu verir (deterministik)", true),
                 ("Gelecekteki gerçek enflasyon oranını söyler", false)]),
        ]);

        yield return ("lesson-cesitlendirme", "quiz-cesitlendirme", "Çeşitlendirme — Mini Test",
        [
            // ── Kolay ────────────────────────────────────────────────────────
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Easy,
                "Bir portföyde en büyük iki varlık toplam ağırlığın %84'ünü oluşturuyor. Bu neyi gösterir?",
                "Bu bir yoğunlaşma göstergesidir: portföyün kaderi ağırlıklı olarak iki kaleme bağlıdır. " +
                "Yoğunlaşma tek başına \"yanlış\" demek değildir; farkında olunması gereken bir risk dağılımıdır.",
                [("Portföyün iyi çeşitlendirildiğini", false),
                 ("Değerin az sayıda kalemde toplandığını — yüksek yoğunlaşma", true),
                 ("Portföyün kesinlikle zarar edeceğini", false),
                 ("Kalem sayısının yetersiz olduğunu", false)]),

            new SeedQuestion(QuizQuestionType.TrueFalse, QuizDifficulty.Easy,
                "Aynı sektörden on farklı hisse tutmak, portföyü iyi çeşitlendirilmiş yapar.",
                "Kalem sayısı çeşitlendirme demek değildir. Aynı sektördeki şirketler benzer sebeplerle " +
                "birlikte hareket etme eğilimindedir; sayıca on, davranışça bir olabilirler.",
                [("Doğru", false), ("Yanlış", true)]),

            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Easy,
                "Çeşitlendirmenin temel amacı nedir?",
                "Çeşitlendirme riski YOK ETMEZ, farklı kaynaklara YAYAR. Amaç, bütün varlıkların aynı anda " +
                "aynı yöne hareket etme ihtimalini azaltmaktır — tek bir olayın seni orantısız vurmasını engellemek.",
                [("Riski tamamen ortadan kaldırmak", false),
                 ("Getiriyi garantilemek", false),
                 ("Riski farklı kaynaklara yaymak", true),
                 ("Mümkün olduğunca çok kalem toplamak", false)]),

            // ── Orta ─────────────────────────────────────────────────────────
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Medium,
                "İki portföyün de beş kalemi var. Birincide ağırlıklar %70/10/8/7/5, ikincide %25/22/20/18/15. " +
                "En büyük kalem %30 değer kaybederse ne olur?",
                "Etki ağırlıkla orantılıdır: birincide 0,70 × %30 ≈ %21 kayıp, ikincide 0,25 × %30 ≈ %7,5. " +
                "Aynı olay, aynı kalem sayısı — ama yoğunlaşma farkı etkiyi üç katına çıkarıyor.",
                [("İkisi de yaklaşık aynı oranda düşer", false),
                 ("Birinci ≈ %21, ikinci ≈ %7,5 düşer", true),
                 ("Birinci ≈ %30, ikinci ≈ %30 düşer", false),
                 ("Kalem sayıları eşit olduğu için fark olmaz", false)]),

            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Medium,
                "Çeşitlendirme hangi riski azaltmaz?",
                "Çeşitlendirme varlığa/şirkete özgü riski azaltır. Genel ekonomik daralma gibi SİSTEME özgü " +
                "riskler her şeyi aynı anda etkilediği için çeşitlendirmeyle ortadan kaldırılamaz.",
                [("Tek bir şirketin kötü yönetilmesi riskini", false),
                 ("Tüm piyasayı aynı anda etkileyen genel ekonomik şok riskini", true),
                 ("Tek bir varlığın değer kaybetmesi riskini", false),
                 ("Belirli bir sektöre özgü sorun riskini", false)]),

            new SeedQuestion(QuizQuestionType.TrueFalse, QuizDifficulty.Medium,
                "Portföyünü bir kez dengeli kurduysan, işlem yapmadığın sürece dengeli kalır.",
                "Kalmaz. İyi giden varlık büyür ve ağırlığı artar; hiçbir işlem yapmasan bile portföy " +
                "KENDİLİĞİNDEN yoğunlaşır. Çeşitlendirme bir kerelik kurulum değil, süregelen bir durumdur.",
                [("Doğru", false), ("Yanlış", true)]),

            // ── Zor ──────────────────────────────────────────────────────────
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Hard,
                "Bir portföyde dört kalem var: vadeli mevduat, yerel hisse, yerel tahvil fonu, TL likit fon. " +
                "Bu portföyün en büyük çeşitlendirme zaafı nedir?",
                "Varlık türü ekseninde dört kutu görünür ama PARA BİRİMİ ekseninde tek kutu vardır: dördü de TL. " +
                "TL'nin alım gücünü etkileyen bir gelişme dördünü birden aynı yönde etkiler.",
                [("Kalem sayısının az olması", false),
                 ("Dördünün de aynı para birimine bağlı olması", true),
                 ("Fon sayısının fazla olması", false),
                 ("Mevduatın getirisinin düşük olması", false)]),

            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Hard,
                "İki varlığın korelasyonu +1'e çok yakınsa, ikisini birlikte tutmak ne sağlar?",
                "+1'e yakın korelasyon \"neredeyse hep birlikte hareket ederler\" demektir. İkisini birlikte " +
                "tutmak riski bölmez; aynı riski iki parçaya yazmış olursun. Yumuşatma için farklı sebeplerle " +
                "hareket eden (korelasyonu düşük) varlıklar gerekir.",
                [("Toplam oynaklığı belirgin şekilde düşürür", false),
                 ("Neredeyse hiçbir yumuşatma sağlamaz — tek varlık gibi davranır", true),
                 ("Getiriyi iki katına çıkarır", false),
                 ("Sistematik riski ortadan kaldırır", false)]),

            new SeedQuestion(QuizQuestionType.MultipleChoice, QuizDifficulty.Hard,
                "Aşağıdakilerden hangileri çeşitlendirmeyi gerçekten artırır? (birden fazla)",
                "Gerçek çeşitlilik FARKLI SEBEPLERLE hareket etmekten gelir: farklı para birimi, farklı varlık " +
                "türü, farklı ekonomiye bağlılık. Aynı sektörde kalem eklemek ya da aynı türden ikinci bir fon " +
                "almak kalem sayısını artırır, ekseni artırmaz.",
                [("Farklı para biriminde bir kalem eklemek", true),
                 ("Farklı bir varlık türü eklemek (örn. yalnız hisse varken altın)", true),
                 ("Aynı sektörden ikinci bir hisse eklemek", false),
                 ("Farklı bir ekonomiye bağlı bir kalem eklemek", true)]),
        ]);

        yield return ("lesson-fk-pddd", "quiz-fk-pddd", "F/K ve PD/DD — Mini Test",
        [
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Easy,
                "Bir şirketin F/K oranı sektör ortalamasının belirgin şekilde altında. Bu tek başına ne söyler?",
                "Düşük F/K iki zıt durumun işareti olabilir: piyasa şirketi gözden kaçırmış olabilir ya da " +
                "kârın düşeceğini bekliyor olabilir. Oran hangisi olduğunu söylemez — yalnızca sorulacak soruyu gösterir.",
                [
                    ("Hissenin kesinlikle ucuz olduğunu", false),
                    ("Tek başına yeterli değildir; sebebi araştırılmalıdır", true),
                    ("Şirketin kesinlikle zarar ettiğini", false),
                    ("Hissenin alınması gerektiğini", false),
                ]),
            new SeedQuestion(QuizQuestionType.TrueFalse, QuizDifficulty.Easy,
                "F/K oranı şirketin borç yükünü de hesaba katar.",
                "F/K yalnızca fiyat ve kârı karşılaştırır; borcu görmez. Aynı kârı üreten borçsuz bir şirketle " +
                "ağır borçlu bir şirketin F/K'sı aynı görünebilir ama taşıdıkları risk çok farklıdır.",
                [("Doğru", false), ("Yanlış", true)]),
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Easy,
                "PD/DD oranı neyi karşılaştırır?",
                "PD/DD, şirketin borsadaki piyasa değerini muhasebe defterindeki öz kaynağına oranlar. " +
                "1'in üzerinde olması piyasanın şirkete defter değerinden fazlasını biçtiğini gösterir.",
                [
                    ("Hisse fiyatını hisse başına kâra", false),
                    ("Piyasa değerini defterdeki öz kaynağa", true),
                    ("Temettüyü hisse fiyatına", false),
                    ("Kârı borca", false),
                ]),
        ]);

        yield return ("lesson-risk-getiri", "quiz-risk-getiri", "Risk ve Getiri — Mini Test",
        [
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Easy,
                "Yatırımda \"risk\" en doğru nasıl tanımlanır?",
                "Risk, sonucun ne kadar oynak ve belirsiz olduğudur — yalnızca \"para kaybetme\" değil, " +
                "sonucun geniş bir aralığa yayılmasıdır. Bu yüzden yüksek beklenen getiri, yüksek belirsizlikle birlikte gelir.",
                [
                    ("Kesin olarak para kaybetmek", false),
                    ("Sonucun ne kadar oynak ve belirsiz olduğu", true),
                    ("Yatırımın süresi", false),
                    ("Komisyon oranı", false),
                ]),
            new SeedQuestion(QuizQuestionType.TrueFalse, QuizDifficulty.Easy,
                "Parayı enflasyonun altında getiri veren bir yerde tutmak risksizdir.",
                "Risk almamak da bir risktir. Enflasyonun altında kalan getiri, rakam büyüse bile alım gücü " +
                "kaybıdır — bu sessiz erime de bir risk türüdür.",
                [("Doğru", false), ("Yanlış", true)]),
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Easy,
                "%50 değer kaybeden bir yatırımın başa dönmesi için ne kadar kazanması gerekir?",
                "Kayıp ve kazanç simetrik değildir: 100'den 50'ye düşen bir değerin tekrar 100 olması için " +
                "%100 artması gerekir. Bu asimetri, büyük düşüşlerden kaçınmayı matematiksel olarak değerli kılar.",
                [
                    ("%50", false),
                    ("%75", false),
                    ("%100", true),
                    ("%150", false),
                ]),
        ]);

        yield return ("lesson-bilesik", "quiz-bilesik", "Bileşik Getiri — Mini Test",
        [
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Easy,
                "100.000 ₺ yılda %20 büyürse ikinci yılın sonunda ne olur?",
                "Birinci yıl 120.000 ₺, ikinci yıl bunun %20'si eklenir: 144.000 ₺. İkinci yılın artışı 20.000 değil " +
                "24.000 ₺'dir — fark, önceki yılın kârının da çalışmasından gelir.",
                [
                    ("140.000 ₺", false),
                    ("144.000 ₺", true),
                    ("120.000 ₺", false),
                    ("124.000 ₺", false),
                ]),
            new SeedQuestion(QuizQuestionType.TrueFalse, QuizDifficulty.Easy,
                "Bileşik etkiden yararlanmak için kazancın yeniden yatırılması gerekir.",
                "Getiriyi her dönem dışarı çekersen bileşiklenecek bir şey kalmaz; büyüme doğrusal hâle gelir. " +
                "Bileşik etkinin iki bileşeni süre ve kazancı yeniden yatırmaktır.",
                [("Doğru", true), ("Yanlış", false)]),
            new SeedQuestion(QuizQuestionType.SingleChoice, QuizDifficulty.Easy,
                "Bileşik büyümede en belirleyici değişken hangisidir?",
                "Getiri oranı önemlidir ama bileşik etkide asıl çarpan süredir: son yıllar ilk yıllardan çok daha " +
                "fazla katkı yapar, çünkü aynı oran daha büyük bir taban üzerinde çalışır.",
                [
                    ("Başlangıçtaki tutarın büyüklüğü", false),
                    ("Paranın yatırımda kaldığı süre", true),
                    ("İşlem sayısı", false),
                    ("Varlık türü", false),
                ]),
        ]);
    }
}
