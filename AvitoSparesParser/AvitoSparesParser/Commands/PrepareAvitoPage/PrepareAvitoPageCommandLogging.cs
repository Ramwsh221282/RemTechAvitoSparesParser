namespace AvitoSparesParser.Commands.PrepareAvitoPage;

public sealed class PrepareAvitoPageCommandLogging(Serilog.ILogger logger, IPrepareAvitoPageCommand inner) 
    : IPrepareAvitoPageCommand
{
    private Serilog.ILogger Logger { get; } = logger.ForContext<IPrepareAvitoPageCommand>();
    
    public async Task Prepare(Func<string> urlSource)
    {
        Logger.Information("Preparing avito page: {url}", urlSource);

        try
        {
            await inner.Prepare(urlSource);
            Logger.Information("Prepared avito page: {Url}.", urlSource);
        }
        catch(Exception ex)
        {
            Logger.Fatal(ex, "Error at preparing avito catalogue page.");
            throw;
        }
    }
}