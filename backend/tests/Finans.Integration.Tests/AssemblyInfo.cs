// WebApplicationFactory<Program> tabanlı testler gerçek host ayağa kaldırır ve
// Serilog statik Log.Logger'ı (bootstrap + CloseAndFlush) paylaşır. Paralel
// koşumda bir host'un kapanışı diğerinin logger'ını kapatıp isteği bozabiliyor.
// Integration testlerini sıralı koştur (standart yaklaşım).
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
