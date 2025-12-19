namespace AvitoSparesParser.Commands.ExtractCataloguePageItems;

public static class ExtractCataloguePagesItemDecorating
{
    extension(IExtractCataloguePagesItemCommand inner)
    {
        public IExtractCataloguePagesItemCommand UseLogging(Serilog.ILogger logger) =>
            new ExtractCataloguePagesItemCommandLogging(logger, inner);
    }
}