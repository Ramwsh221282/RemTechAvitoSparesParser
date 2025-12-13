using AvitoSparesParser.Common;

namespace AvitoSparesParser.ParserProcessStarting;

public sealed record ProcessingParserLink(
    Guid Id,
    Guid ParserId,
    string Url,
    RetryCounter Counter,
    ProcessedMarker Marker
) : IHasRetryCounter, IHasProcessedMarker;