using AvitoSparesParser.AvitoSpareContext;
using AvitoSparesParser.CatalogueParsing;

namespace AvitoSparesParser.Commands.ExtractCataloguePageItems;

public interface IExtractCataloguePagesItemCommand
{
    Task<AvitoSpare[]> Extract(AvitoCataloguePage page);
}