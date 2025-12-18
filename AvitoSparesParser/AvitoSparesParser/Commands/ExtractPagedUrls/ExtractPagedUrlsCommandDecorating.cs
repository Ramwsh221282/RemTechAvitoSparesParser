namespace AvitoSparesParser.Commands.ExtractPagedUrls;

public static class ExtractPagedUrlsCommandDecorating
{
    extension(IExtractPagedUrlsCommand command)
    {
        public IExtractPagedUrlsCommand UseLogging(Serilog.ILogger logger)
        {
            return new ExtractPagedUrlsCommandLogging(logger, command);
        }
    }
}