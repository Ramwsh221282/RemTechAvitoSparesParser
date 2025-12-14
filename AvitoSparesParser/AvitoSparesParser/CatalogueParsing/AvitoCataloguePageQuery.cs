namespace AvitoSparesParser.CatalogueParsing;

public sealed record AvitoCataloguePageQuery(
    bool ProcessedOnly = false, 
    bool UnprocessedOnly = false,
    int? RetryThreshold = null,
    bool WithLock = false);
