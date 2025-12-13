namespace AvitoSparesParser.ParserProcessStarting;

public sealed record ProcessingParserLink(
    Guid Id,
    Guid ParserId,    
    string Url,
    bool CatalogueFetched,
    int RetryCount
);
