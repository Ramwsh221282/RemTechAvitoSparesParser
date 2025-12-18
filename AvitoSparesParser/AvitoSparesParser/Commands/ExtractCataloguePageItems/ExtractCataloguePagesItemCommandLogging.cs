using AvitoSparesParser.AvitoSpareContext;
using AvitoSparesParser.CatalogueParsing;

namespace AvitoSparesParser.Commands.ExtractCataloguePageItems;

public sealed class ExtractCataloguePagesItemCommandLogging(Serilog.ILogger logger, IExtractCataloguePagesItemCommand inner) : IExtractCataloguePagesItemCommand
{
    private Serilog.ILogger Logger { get; } = logger.ForContext<ExtractCataloguePagesItemCommandLogging>();
    public async Task<AvitoSpare[]> Extract(AvitoCataloguePage page)
    {
        Logger.Information("Extracting catalogue page items for url: {Url}", page.Url);
        try
        {
            AvitoSpare[] items = await inner.Extract(page);
            
            Logger.Information("Extracted catalogue page items. Length: {Length}", items.Length);
            foreach (AvitoSpare item in items)
            {
                AvitoSpareCatalogueRepresentation representation = item.CatalogueRepresentation;
                Logger.Information("""
                                   Id: {Id}
                                   Url: {Url}
                                   Price/NDS: {Price} {IsNds}
                                   Oem: {Oem}
                                   Address: {Address}
                                   Photos: {PhotosLength}
                                   """,
                    item.Id,
                    representation.Url,
                    representation.Price,
                    representation.IsNds, 
                    representation.Oem,
                    representation.Address,
                    representation.Photos.Length);
            }
            
            return items;
        }
        catch (Exception e)
        {
            Logger.Error(e, "Error extracting catalogue page items for url: {Url}", page.Url);
            throw;
        }
    }
}