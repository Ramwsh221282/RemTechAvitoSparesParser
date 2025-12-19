namespace AvitoSparesParser.Commands.HoverCatalogueItemImages;

public sealed class HoverCatalogueItemImagesLogging(
    Serilog.ILogger logger,
    IHoverCatalogueItemImagesCommand inner) : IHoverCatalogueItemImagesCommand
{
    private Serilog.ILogger Logger { get; } = logger;
    public async Task Hover()
    {
        Logger.Information("Hovering catalogue item images.");
        try
        {
            await inner.Hover();
            Logger.Information("Hovered catalogue item images.");
        }
        catch(Exception ex)
        {
            Logger.Error(ex, "Failed to hover catalogue item images.");
            throw; 
        }
    }
}