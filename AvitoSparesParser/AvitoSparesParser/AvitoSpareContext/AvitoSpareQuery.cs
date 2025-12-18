namespace AvitoSparesParser.AvitoSpareContext;

public sealed record AvitoSpareQuery(
    bool ProcessedOnly = false,
    bool UnprocessedOnly = false,
    bool WithLock = false, 
    int? Limit = null,
    int? RetryCountThreshold = null,
    bool ConcreteOnly = false,
    bool CatalogueOnly = false
);