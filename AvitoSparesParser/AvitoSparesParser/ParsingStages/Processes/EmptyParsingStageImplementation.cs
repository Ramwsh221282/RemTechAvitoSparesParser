namespace AvitoSparesParser.ParsingStages.Processes;

public static class EmptyParsingStageImplementation
{
    extension(ParserStageProcess)
    {
        public static ParserStageProcess Empty => (_, _) => Task.CompletedTask;

    }
}
