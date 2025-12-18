using AvitoSparesParser.CatalogueParsing;

namespace AvitoSparesParser.Commands.ExtractPagedUrls;

public interface IExtractPagedUrlsCommand
{
    Task<AvitoCataloguePage[]> Extract(string initialUrl);
}