namespace AvitoSparesParser.ParserStartConfiguration;

public sealed record ProcessingParserLinkQuery(
    bool OnlyFetched = false,
    bool OnlyNotFetched = false,
    int? RetryCountThreshold = null,
    bool WithLock = false
);
