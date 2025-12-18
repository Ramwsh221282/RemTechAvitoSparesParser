namespace AvitoSparesParser.Commands.HoverCatalogueItemImages;

public static class HoverCatalogueItemImagesDecorating
{
    extension(IHoverCatalogueItemImagesCommand command)
    {
        public IHoverCatalogueItemImagesCommand UseLogging(Serilog.ILogger logger)
            => new HoverCatalogueItemImagesLogging(logger, command);
    }
}