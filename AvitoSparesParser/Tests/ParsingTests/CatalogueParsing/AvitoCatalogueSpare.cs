using Tests.ParsingTests.Common;

namespace Tests.ParsingTests.CatalogueParsing;

public sealed record AvitoCatalogueSpare(
    AvitoCatalogueItemMetadata Metadata,
    PlainJsonStringArray Photos,
    RetryCounter Counter,
    ProcessedMarker Marker) : 
    IHasRetryCounter, 
    IHasProcessedMarker;