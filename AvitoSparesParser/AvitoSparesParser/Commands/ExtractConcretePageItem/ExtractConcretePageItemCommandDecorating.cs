namespace AvitoSparesParser.Commands.ExtractConcretePageItem;

public static class ExtractConcretePageItemCommandDecorating
{
    extension(IExtractConcretePageItemCommand command)
    {
        public IExtractConcretePageItemCommand UseLogging(Serilog.ILogger logger)
        {
            return new ExtractConcretePageItemLogging(logger, command);
        }
    }
}