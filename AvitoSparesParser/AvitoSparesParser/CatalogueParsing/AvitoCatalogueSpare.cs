using AvitoSparesParser.Common;

namespace AvitoSparesParser.CatalogueParsing;

public sealed record AvitoCatalogueSpare(
    AvitoCatalogueItemMetadata Metadata,
    PlainJsonStringArray Photos,
    RetryCounter Counter,
    ProcessedMarker Marker) : 
    IHasRetryCounter, 
    IHasProcessedMarker;