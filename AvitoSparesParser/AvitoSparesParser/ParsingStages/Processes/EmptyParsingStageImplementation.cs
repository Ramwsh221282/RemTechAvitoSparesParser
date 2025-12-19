namespace AvitoSparesParser.ParsingStages.Processes;

public static class EmptyParsingStageImplementation
{
    extension(ParserStageProcess)
    {
        public static ParserStageProcess Empty => (deps, _) =>
        {
            Serilog.ILogger logger = deps.Logger.ForContext<ParserStageProcess>();
            logger.Information("Empty stage executed.");
            return Task.CompletedTask;
        };

    }
}
