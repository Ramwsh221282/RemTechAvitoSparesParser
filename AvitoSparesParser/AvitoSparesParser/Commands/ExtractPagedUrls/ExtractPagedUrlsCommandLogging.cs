using AvitoSparesParser.CatalogueParsing;

namespace AvitoSparesParser.Commands.ExtractPagedUrls;

public sealed class ExtractPagedUrlsCommandLogging(
    Serilog.ILogger logger, 
    IExtractPagedUrlsCommand inner) 
    : IExtractPagedUrlsCommand
{
    private Serilog.ILogger Logger { get; } = logger.ForContext<IExtractPagedUrlsCommand>();
    
    public async Task<AvitoCataloguePage[]> Extract(string initialUrl)
    {
        Logger.Information("Extracting paged urls for {Url}", initialUrl);
        
        try
        {
            AvitoCataloguePage[] result = await inner.Extract(initialUrl);
            
            Logger.Information("Extracted {Count} paged urls for {Url}", result.Length, initialUrl);
            foreach (AvitoCataloguePage page in result)
                Logger.Information("Extracted page {Url}", page.Url);
            
            return result;
        }
        catch(Exception ex)
        {
            Logger.Fatal(ex, "Error extracting paged urls for {Url}", initialUrl);
            throw;
        }
    }
}