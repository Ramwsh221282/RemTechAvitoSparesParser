using AvitoSparesParser.AvitoSpareContext;

namespace AvitoSparesParser.Commands.ExtractConcretePageItem;

public sealed class ExtractConcretePageItemLogging(Serilog.ILogger logger, IExtractConcretePageItemCommand inner) : IExtractConcretePageItemCommand
{
    private Serilog.ILogger Logger { get; } = logger;
    
    public async Task<AvitoSpare> Extract(AvitoSpare spare)
    {
        Logger.Information("Extracting concrete item.");

        try
        {
            AvitoSpare extracted = await inner.Extract(spare);
            AvitoSpareCatalogueRepresentation catalogue = extracted.CatalogueRepresentation;
            AvitoSpareConcreteRepresentation concrete = extracted.ConcreteRepresentation;
            Logger.Information("""
                               Concrete item info:
                               Id: {Id}
                               Url: {Url}
                               Price/NDS: {Price} {IsNds}
                               Oem: {Oem}
                               Address: {Address}
                               Photos: {PhotosLength}
                               Title: {Title} 
                               Type: {Type}
                               """,
                extracted.Id,
                catalogue.Url,
                catalogue.Price,
                catalogue.IsNds,
                catalogue.Oem,
                catalogue.Address,
                catalogue.Photos.Length,
                concrete.Title,
                concrete.Type);
            return extracted;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error at extracting concrete item: {Url}", spare.CatalogueRepresentation.Url);
            throw;
        }
    }
}