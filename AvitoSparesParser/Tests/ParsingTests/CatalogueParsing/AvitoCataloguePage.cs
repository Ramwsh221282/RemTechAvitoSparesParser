using Tests.ParsingTests.Common;

namespace Tests.ParsingTests.CatalogueParsing;

public sealed record AvitoCataloguePage(
    Guid Id,
    string Url,
    RetryCounter Counter,
    ProcessedMarker Marker
) : 
    IHasRetryCounter, 
    IHasProcessedMarker;