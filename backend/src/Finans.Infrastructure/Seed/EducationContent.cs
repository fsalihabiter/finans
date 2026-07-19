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
/// <item><b>MiniMarkdown alt kümesi</b>: yalnız <c>##</c>/<c>###</c> başlık, <c>- </c> liste,
///   <c>&gt; </c> alıntı, <c>**kalın**</c>, düz paragraf. <b>Tablo/link/kod YOK</b> —
///   renderer desteklemiyor (tablo+link T6.8'de gelir).</item>
/// <item><b>TR biçim</b>: binlik nokta, ondalık virgül (422.970,50 ₺).</item>
/// <item>Sayılar <b>statik ve güvenli</b>; kullanıcının gerçek verisi buraya girmez —
///   o iş <see cref="SectionKind.LiveContext"/> bloğunda ve T6.2'de.</item>
/// </list>
/// </remarks>
internal static class EducationContent
{
    /// <summary>Bir dersin katmanlı bloklarını üretir (sıra: Core→Context→Deep→Example→Trap).</summary>
    private static IEnumerable<LessonSection> Blocks(
        Guid lessonId, string core, string context, string deep, string example, string trap)
    {
        var parts = new (string Body, DepthTier Tier, SectionKind Kind)[]
        {
            (core, DepthTier.Core, SectionKind.Explain),
            (context, DepthTier.Context, SectionKind.Explain),
            (deep, DepthTier.Deep, SectionKind.Explain),
            (example, DepthTier.Core, SectionKind.Example),
            (trap, DepthTier.Context, SectionKind.Trap),
        };

        var order = 1;
        foreach (var (body, tier, kind) in parts)
        {
            yield return new LessonSection
            {
                LessonId = lessonId,
                OrderIndex = order++,
                BodyMarkdown = body.Trim(),
                DepthTier = tier,
                Kind = kind,
            };
        }
    }

    // ── Ders 1 — Enflasyon ve Reel Getiri ────────────────────────────────────

    public static IEnumerable<LessonSection> Lesson1(Guid id) => Blocks(id,
        core: """
        ## Rakam mı büyüdü, paran mı?

        Bir yıl önce 100.000 ₺'n vardı, bugün 140.000 ₺. Kazandın mı? Cevap, aynı
        dönemde fiyatların ne kadar arttığına bağlı.

        **Nominal getiri** cüzdanındaki rakamın değişimidir: burada %40.

        **Reel getiri** ise alım gücünün değişimidir — yani o parayla gerçekte
        daha fazla şey alabiliyor musun?

        Fiyatlar aynı dönemde %38 arttıysa, 140.000 ₺ ile bugün ancak bir yıl önce
        100.000 ₺ ile aldıklarının biraz fazlasını alabilirsin. Rakam büyük ölçüde
        büyüdü, alım gücün ise çok az.

        Yatırımda asıl soru şudur: **param büyüdü mü, yoksa sadece rakam mı?**
        """,
        context: """
        ## Nasıl hesaplanır?

        Reel getiri, nominal getiriyi enflasyona göre düzeltir:

        > reel getiri = (1 + nominal) / (1 + enflasyon) − 1

        Örnekteki rakamlarla: (1,40 / 1,38) − 1 ≈ **%1,4**.

        ### Neden çıkarma yetmez?

        Çoğu kişi %40 − %38 = %2 der. Bu kaba bir yaklaşımdır ve düşük enflasyonda
        kabul edilebilir sonuç verir. Ama enflasyon yükseldikçe iki yöntem arasındaki
        fark açılır, çünkü enflasyon getirinin **üzerine** değil, **içine** işler —
        kazandığın paranın da alım gücü erir.

        ### Hangi enflasyon?

        TÜFE (Tüketici Fiyat Endeksi) ortalama bir tüketim sepetini ölçer. Senin
        harcama sepetin bu ortalamadan farklıysa — kirada mı oturuyorsun, araba mı
        kullanıyorsun — hissettiğin enflasyon da farklı olur. Bu yüzden reel getiri
        tek bir mutlak gerçek değil, **hangi sepete göre** sorusuna bağlı bir ölçüdür.

        ### Dönem seçimi

        Aynı yatırım, seçtiğin başlangıç tarihine göre çok farklı reel getiriler
        gösterebilir. Bir dönemi öne çıkarıp diğerini görmezden gelmek, farkında
        olmadan kendini kandırmanın en kolay yoludur.
        """,
        deep: """
        ### Formülün arkasındaki mantık

        Nominal getiri paranın **miktarını**, enflasyon ise paranın **birim değerini**
        değiştirir. İkisi aynı anda çalıştığı için etkileri toplanmaz, çarpılır.

        Elindeki para (1 + nominal) katına çıkarken, bir birim malın fiyatı
        (1 + enflasyon) katına çıkar. Alabileceğin mal miktarı bu ikisinin
        **oranıdır** — bu yüzden formülde bölme vardır.

        ### Negatif reel getiri ne demek?

        Reel getiri eksiyse, hesabındaki rakam artmış olsa bile daha az şey
        alabiliyorsun demektir. Yüksek enflasyon dönemlerinde yüksek nominal
        getiriler olağanlaşır; %45 getiri kulağa büyük gelir ama enflasyon %50 ise
        alım gücün gerilemiştir.

        ### Maliyetler de reel getiriyi düşürür

        İşlem komisyonları, alış-satış makası ve varsa fon gider oranı nominal
        getiriden düşer. Reel getiriyi hesaplarken bunları göz ardı etmek, tabloyu
        olduğundan iyi gösterir.

        ### Neden bu kavram her şeyin temeli?

        Enflasyon, yatırımın "sıfır çizgisidir". Enflasyonun altında kalan her
        getiri, matematiksel olarak alım gücü kaybıdır — rakam büyürken servetin
        küçülür. Bu yüzden bir portföyü değerlendirirken ilk bakılacak yer nominal
        kâr değil, enflasyona göre nerede durduğudur.
        """,
        example: """
        ## Örnek: aynı yıl, üç farklı sonuç

        Diyelim ki bir yılda genel fiyat artışı **%50** oldu. Üç kişinin nominal
        getirisi şöyle:

        - **A yatırımı:** %45 nominal → reel ≈ **−%3,3**
        - **B yatırımı:** %50 nominal → reel = **%0** (tam başa baş)
        - **C yatırımı:** %70 nominal → reel ≈ **+%13,3**

        Üçü de "kâr etti" diyebilir; ancak yalnızca C'nin alım gücü arttı. A, hesap
        özetinde artı bakiye görmesine rağmen bir yıl öncesine göre daha az şey
        alabiliyor.

        > Buradaki A, B, C birer **hesaplama örneğidir** — belirli bir yatırım
        > aracını işaret etmez ve hiçbiri diğerinden "iyi" ilan edilmiyor. Amaç
        > yalnızca aynı enflasyon altında farklı nominal getirilerin nasıl farklı
        > reel sonuçlar verdiğini göstermek.
        """,
        trap: """
        ## Sık yapılan hata

        **"Yüksek nominal getiri = başarılı yatırım."**

        Yüksek enflasyon ortamında yüksek nominal getiriler olağandır — herkesin
        rakamı büyür. Bu ortamda %60 getiri, enflasyon %65 ise aslında bir kayıptır.

        İkinci yaygın hata, **kâr/zararı sadece lira olarak okumaktır.** "50.000 ₺
        kazandım" cümlesi, o 50.000 ₺'nin bugün ne aldığını söylemez.

        Üçüncüsü ise **enflasyonu yalnızca kötü haber olarak görmektir.** Enflasyon
        bir düşman değil, bir **ölçüm çizgisidir**: getirini karşılaştıracağın taban.
        Bu çizgiyi bilmeden bir yatırımın iyi mi kötü mü gittiğini söylemek mümkün
        değildir.
        """);

    // ── Ders 2 — Çeşitlendirme ───────────────────────────────────────────────

    public static IEnumerable<LessonSection> Lesson2(Guid id) => Blocks(id,
        core: """
        ## Ağırlık nerede toplanıyor?

        Bir portföyün değeri tek bir varlığa bağlıysa, o varlık düştüğünde
        portföyün tamamı birlikte düşer. Farklı davranan varlıkları bir arada
        tutmak, biri kötü giderken diğerlerinin dengeleme ihtimalini artırır.

        **Yoğunlaşma**, değerin az sayıda kalemde toplanmasıdır. Portföyünün
        %84'ü iki varlıktaysa, o iki varlığın ortak kaderi senin de kaderin olur.

        Çeşitlendirme riski **yok etmez** — farklı kaynaklara **yayar**. Amaç,
        bütün varlıklarının aynı anda aynı yöne hareket etme ihtimalini azaltmaktır.

        Bu, "şu kadar varlık iyidir" diye bir kural değil, bir farkındalıktır:
        ağırlığının nerede toplandığını bilmek.
        """,
        context: """
        ## Çeşitlendirme nasıl ölçülür?

        En basit ölçü **ağırlıktır**: her varlığın güncel değerinin portföy toplamına
        oranı. Yoğunlaşmayı görmenin pratik yolu, en büyük iki-üç kalemin toplam
        ağırlığına bakmaktır.

        > varlık ağırlığı = varlık güncel değeri / portföy toplam değeri

        ### Kalem sayısı yanıltabilir

        On farklı kalem tutuyor olman çeşitlendirdiğin anlamına gelmez. Onunun da
        aynı sektörde, aynı para biriminde ya da aynı ekonomik hikâyeye bağlı olması
        mümkündür. Bu durumda kalem sayısı çok, **gerçek çeşitlilik az**tır.

        ### Asıl mesele: birlikte hareket

        Çeşitlendirmenin işe yaraması için varlıkların **farklı sebeplerle** değer
        kazanıp kaybetmesi gerekir. Aynı anda aynı yöne giden iki varlık, tek bir
        varlık gibi davranır — sayıca iki, davranışça bir.

        ### Sınırı da var

        Aşırı dağılmak da bir maliyettir: takip etmesi zorlaşır, her kalem için işlem
        maliyeti doğar ve hiçbiri portföyde anlamlı bir yer tutmaz. Çeşitlendirme
        "ne kadar çok o kadar iyi" değil, **anlamlı farklılık** meselesidir.
        """,
        deep: """
        ### Neden riski azaltır?

        İki varlığın birlikte hareket etme eğilimine **korelasyon** denir. Tam
        birlikte hareket eden iki varlığı yan yana koymak riski azaltmaz; farklı
        sebeplerle hareket eden varlıklar ise birbirinin dalgalanmasını yumuşatır.

        Sezgisel olarak: bir varlık kötü haberle düşerken diğeri o haberden
        etkilenmiyorsa, portföyün toplam oynaklığı tek başına en oynak varlığından
        daha düşük olur. Çeşitlendirmenin "bedava öğle yemeği" diye anılmasının
        sebebi budur — getiriden ödün vermeden oynaklığı azaltabilir.

        ### Ortadan kaldırılamayan risk

        Çeşitlendirme **şirkete/varlığa özgü** riski azaltır: bir şirketin kötü
        yönetilmesi, bir madenin kapanması. Ancak **sisteme özgü** riski —
        genel ekonomik daralma, ülke çapında bir şok — azaltmaz, çünkü o risk
        her şeyi aynı anda etkiler.

        Bu ayrım önemlidir: hiçbir çeşitlendirme seviyesi seni "risksiz" yapmaz.

        ### Türkiye'ye özgü bir katman: para birimi

        Farklı varlık türlerine yayılmış görünen bir portföy, hepsi aynı para
        birimine bağlıysa hâlâ tek bir riske açıktır. Para birimi, çeşitlendirmede
        çoğu zaman gözden kaçan ama belirleyici bir eksendir.

        ### Yeniden dengeleme kavramı

        Zamanla iyi giden varlık büyür ve ağırlığı artar; portföy kendiliğinden
        yoğunlaşır. Ağırlıkların zaman içinde nasıl kaydığını izlemek, çeşitlendirmenin
        bir kerelik değil **süregelen** bir durum olduğunu gösterir.
        """,
        example: """
        ## Örnek: iki portföy, aynı kalem sayısı

        İki portföyün de beş kalemi var, ikisi de 100.000 ₺:

        **Birinci portföy** — ağırlıklar: %70, %10, %8, %7, %5.
        En büyük kalem tek başına portföyün üçte ikisinden fazlası. Bu kalem %30
        değer kaybederse portföy yaklaşık %21 küçülür.

        **İkinci portföy** — ağırlıklar: %25, %22, %20, %18, %15.
        En büyük kalem aynı %30'u kaybederse portföy yaklaşık %7,5 küçülür.

        İkisinin de "beş kalemi" var; ancak yoğunlaşmaları çok farklı. Kalem sayısı
        değil, **ağırlık dağılımı** belirleyicidir.

        > Bu örnek belirli varlıkları karşılaştırmıyor; yalnızca aynı sayıda kalemin
        > çok farklı yoğunlaşma üretebileceğini gösteriyor.
        """,
        trap: """
        ## Sık yapılan hata

        **"Çok kalemim var, o hâlde çeşitlendirdim."**

        Aynı sektörden on hisse, birbirinin yerine geçen on kalemdir. Çeşitlilik
        sayıda değil, **farklı davranışta**dır.

        İkinci yanılgı: **"Çeşitlendirirsem kaybetmem."** Çeşitlendirme kaybı
        engellemez; tek bir kötü olayın seni orantısız vurmasını engeller. Piyasanın
        tümü düştüğünde çeşitlendirilmiş portföy de düşer.

        Üçüncüsü ise sinsi olanı: **portföyün kendiliğinden yoğunlaşması.** En çok
        değerlenen varlık zamanla en büyük ağırlığa ulaşır. Hiçbir işlem yapmasan
        bile, iki yıl önce dengeli olan bir portföy bugün tek bir kaleme bağlı
        hâle gelmiş olabilir.
        """);

    // ── Ders 3 — F/K, PD/DD ──────────────────────────────────────────────────

    public static IEnumerable<LessonSection> Lesson3(Guid id) => Blocks(id,
        core: """
        ## Bir hisseyi okumanın rakamları

        **F/K (Fiyat / Kazanç)**, hisse fiyatının şirketin hisse başına kârına
        oranıdır. Sorduğu şey basittir: *şirketin 1 liralık kârı için kaç lira
        ödüyorum?*

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
        *değer tuzağı* denir.

        İkinci hata: **tek bir orana bakıp karar vermek.** F/K, PD/DD ve temettü
        verimi aynı şirketin farklı yüzleridir; biri güzel görünürken diğeri
        uyarı veriyor olabilir.

        Üçüncüsü: **sektör farkını unutmak.** Bir bankanın PD/DD'siyle bir yazılım
        şirketininkini yan yana koymak, iki farklı ölçü biriminde konuşmaktır.

        Son olarak, bu oranlar **ne yapman gerektiğini söylemez.** Sana şirketin
        rakamlarının ne anlattığını gösterir; kararın ve sorumluluğun sana aittir.
        """);

    // ── Ders 4 — Risk ve Getiri ──────────────────────────────────────────────

    public static IEnumerable<LessonSection> Lesson4(Guid id) => Blocks(id,
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
        """);

    // ── Ders 5 — Bileşik Getiri ──────────────────────────────────────────────

    public static IEnumerable<LessonSection> Lesson5(Guid id) => Blocks(id,
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
        """);

    // ── 2-5. derslerin mini testleri (T6.1) ──────────────────────────────────
    // Ders 1'inki T5E.2'de geldi. Her soruda eğitici `Explanation` var; doğru şık
    // ve açıklama YALNIZCA deneme sonucunda açılır (T5E.3 sözleşmesi).

    internal sealed record SeedQuestion(
        QuizQuestionType Type,
        string Prompt,
        string Explanation,
        (string Text, bool IsCorrect)[] Options);

    public static IEnumerable<(string LessonKey, string QuizKey, string Title, SeedQuestion[] Questions)>
        RemainingQuizzes()
    {
        yield return ("lesson-cesitlendirme", "quiz-cesitlendirme", "Çeşitlendirme — Mini Test",
        [
            new SeedQuestion(QuizQuestionType.SingleChoice,
                "Bir portföyde en büyük iki varlık toplam ağırlığın %84'ünü oluşturuyor. Bu neyi gösterir?",
                "Bu bir yoğunlaşma göstergesidir: portföyün kaderi ağırlıklı olarak iki kaleme bağlıdır. " +
                "Yoğunlaşma tek başına \"yanlış\" demek değildir; farkında olunması gereken bir risk dağılımıdır.",
                [
                    ("Portföyün iyi çeşitlendirildiğini", false),
                    ("Değerin az sayıda kalemde toplandığını — yüksek yoğunlaşma", true),
                    ("Portföyün kesinlikle zarar edeceğini", false),
                    ("Kalem sayısının yetersiz olduğunu", false),
                ]),
            new SeedQuestion(QuizQuestionType.TrueFalse,
                "Aynı sektörden on farklı hisse tutmak, portföyü iyi çeşitlendirilmiş yapar.",
                "Kalem sayısı çeşitlendirme demek değildir. Aynı sektördeki şirketler benzer sebeplerle " +
                "birlikte hareket etme eğilimindedir; sayıca on, davranışça bir olabilirler.",
                [("Doğru", false), ("Yanlış", true)]),
            new SeedQuestion(QuizQuestionType.SingleChoice,
                "Çeşitlendirme hangi riski azaltmaz?",
                "Çeşitlendirme varlığa/şirkete özgü riski azaltır. Genel ekonomik daralma gibi sisteme özgü " +
                "riskler her şeyi aynı anda etkilediği için çeşitlendirmeyle ortadan kaldırılamaz.",
                [
                    ("Tek bir şirketin kötü yönetilmesi riskini", false),
                    ("Tüm piyasayı aynı anda etkileyen genel ekonomik şok riskini", true),
                    ("Tek bir varlığın değer kaybetmesi riskini", false),
                    ("Belirli bir sektöre özgü sorun riskini", false),
                ]),
        ]);

        yield return ("lesson-fk-pddd", "quiz-fk-pddd", "F/K ve PD/DD — Mini Test",
        [
            new SeedQuestion(QuizQuestionType.SingleChoice,
                "Bir şirketin F/K oranı sektör ortalamasının belirgin şekilde altında. Bu tek başına ne söyler?",
                "Düşük F/K iki zıt durumun işareti olabilir: piyasa şirketi gözden kaçırmış olabilir ya da " +
                "kârın düşeceğini bekliyor olabilir. Oran hangisi olduğunu söylemez — yalnızca sorulacak soruyu gösterir.",
                [
                    ("Hissenin kesinlikle ucuz olduğunu", false),
                    ("Tek başına yeterli değildir; sebebi araştırılmalıdır", true),
                    ("Şirketin kesinlikle zarar ettiğini", false),
                    ("Hissenin alınması gerektiğini", false),
                ]),
            new SeedQuestion(QuizQuestionType.TrueFalse,
                "F/K oranı şirketin borç yükünü de hesaba katar.",
                "F/K yalnızca fiyat ve kârı karşılaştırır; borcu görmez. Aynı kârı üreten borçsuz bir şirketle " +
                "ağır borçlu bir şirketin F/K'sı aynı görünebilir ama taşıdıkları risk çok farklıdır.",
                [("Doğru", false), ("Yanlış", true)]),
            new SeedQuestion(QuizQuestionType.SingleChoice,
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
            new SeedQuestion(QuizQuestionType.SingleChoice,
                "Yatırımda \"risk\" en doğru nasıl tanımlanır?",
                "Risk, sonucun ne kadar oynak ve belirsiz olduğudur — yalnızca \"para kaybetme\" değil, " +
                "sonucun geniş bir aralığa yayılmasıdır. Bu yüzden yüksek beklenen getiri, yüksek belirsizlikle birlikte gelir.",
                [
                    ("Kesin olarak para kaybetmek", false),
                    ("Sonucun ne kadar oynak ve belirsiz olduğu", true),
                    ("Yatırımın süresi", false),
                    ("Komisyon oranı", false),
                ]),
            new SeedQuestion(QuizQuestionType.TrueFalse,
                "Parayı enflasyonun altında getiri veren bir yerde tutmak risksizdir.",
                "Risk almamak da bir risktir. Enflasyonun altında kalan getiri, rakam büyüse bile alım gücü " +
                "kaybıdır — bu sessiz erime de bir risk türüdür.",
                [("Doğru", false), ("Yanlış", true)]),
            new SeedQuestion(QuizQuestionType.SingleChoice,
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
            new SeedQuestion(QuizQuestionType.SingleChoice,
                "100.000 ₺ yılda %20 büyürse ikinci yılın sonunda ne olur?",
                "Birinci yıl 120.000 ₺, ikinci yıl bunun %20'si eklenir: 144.000 ₺. İkinci yılın artışı 20.000 değil " +
                "24.000 ₺'dir — fark, önceki yılın kârının da çalışmasından gelir.",
                [
                    ("140.000 ₺", false),
                    ("144.000 ₺", true),
                    ("120.000 ₺", false),
                    ("124.000 ₺", false),
                ]),
            new SeedQuestion(QuizQuestionType.TrueFalse,
                "Bileşik etkiden yararlanmak için kazancın yeniden yatırılması gerekir.",
                "Getiriyi her dönem dışarı çekersen bileşiklenecek bir şey kalmaz; büyüme doğrusal hâle gelir. " +
                "Bileşik etkinin iki bileşeni süre ve kazancı yeniden yatırmaktır.",
                [("Doğru", true), ("Yanlış", false)]),
            new SeedQuestion(QuizQuestionType.SingleChoice,
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
