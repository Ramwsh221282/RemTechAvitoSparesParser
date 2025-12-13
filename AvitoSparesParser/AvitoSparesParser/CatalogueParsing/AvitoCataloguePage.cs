using AvitoSparesParser.Common;

namespace AvitoSparesParser.CatalogueParsing;

public sealed record AvitoCataloguePage(
    Guid Id,
    string Url,
    RetryCounter Counter,
    ProcessedMarker Marker
) :
    IHasRetryCounter,
    IHasProcessedMarker;
