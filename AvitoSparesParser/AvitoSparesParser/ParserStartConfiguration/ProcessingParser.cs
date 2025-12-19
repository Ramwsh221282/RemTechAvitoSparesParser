namespace AvitoSparesParser.ParserStartConfiguration;

public sealed record ProcessingParser(
    Guid Id,
    string Domain,
    string Type,
    DateTime Entered,
    DateTime? Finished
);
