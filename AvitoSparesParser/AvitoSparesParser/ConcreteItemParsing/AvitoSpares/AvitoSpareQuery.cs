namespace AvitoSparesParser.ConcreteItemParsing.AvitoSpares;

public sealed record AvitoSpareQuery(
    bool ProcessedOnly = false,
    bool UnprocessedOnly = false,
    bool WithLock = false, 
    int? Limit = null
);