namespace AvitoSparesParser.Commands.PrepareAvitoPage;

public static class PrepareAvitoPageCommandDecorating
{
    extension(IPrepareAvitoPageCommand command)
    {
        public IPrepareAvitoPageCommand UseLogging(Serilog.ILogger logger)
        {
            return new PrepareAvitoPageCommandLogging(logger, command);
        }
    }
}