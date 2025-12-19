using AvitoSparesParser.Common;

namespace AvitoSparesParser.ParserStartConfiguration;

public sealed record ProcessingParserLink(
    Guid Id,
    string Url,
    RetryCounter Counter,
    ProcessedMarker Marker
) : IHasRetryCounter, IHasProcessedMarker;