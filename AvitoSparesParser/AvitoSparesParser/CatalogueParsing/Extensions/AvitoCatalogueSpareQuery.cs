namespace AvitoSparesParser.CatalogueParsing.Extensions;

public sealed record AvitoCatalogueSpareQuery(
    bool UnprocessedOnly = false,
    int? RetryLimitThreshold = null,
    bool WithLock = false,
    int? Limit = null
    );
