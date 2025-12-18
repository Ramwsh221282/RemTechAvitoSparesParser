using AvitoSparesParser.AvitoSpareContext;

namespace AvitoSparesParser.Commands.ExtractConcretePageItem;

public interface IExtractConcretePageItemCommand
{
    Task<AvitoSpare> Extract(AvitoSpare spare);
}