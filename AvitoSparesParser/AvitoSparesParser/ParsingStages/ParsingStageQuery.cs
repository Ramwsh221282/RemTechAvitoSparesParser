namespace AvitoSparesParser.ParsingStages;

public sealed record ParsingStageQuery(
    Guid? Id = null, 
    string? Name = null, 
    bool WithLock = false);
