using AvitoSparesParser.CatalogueParsing;
using AvitoSparesParser.CatalogueParsing.Extensions;
using AvitoSparesParser.Commands.ExtractPagedUrls;
using AvitoSparesParser.ParserStartConfiguration;
using AvitoSparesParser.ParserStartConfiguration.Extensions;
using AvitoSparesParser.ParsingStages.Extensions;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace AvitoSparesParser.ParsingStages.Processes;

public static class CataloguePagesCollectingProcess
{
    extension(ParserStageProcess)
    {
        public static ParserStageProcess Pagination => async (deps, ct) =>
        {
            deps.Deconstruct(
                out NpgSqlConnectionFactory npgSql, 
                out Serilog.ILogger dLogger, 
                out AvitoBypassFactory factory, 
                out BrowserFactory browsers, 
                out _);
            
            Serilog.ILogger logger = dLogger.ForContext<ParserStageProcess>();
            await using NpgSqlSession session = new(npgSql);
            await session.UseTransaction(ct);
            
            Maybe<ParsingStage> stage = await GetPaginationStage(session, ct);
            if (!stage.HasValue) return;
            
            ProcessingParserLink[] links = await GetParserLinksForPagedUrlsExtraction(session, ct);
            if (CanSwitchNextStage(links))
            {
                await SwitchNextStage(stage.Value, session, logger, ct);
                return;
            }

            await ProcessPagedUrlExtraction(links, browsers, factory, session, logger);
            await FinishTransaction(session, logger, ct);
        };
    }
    
    private static async Task<Maybe<ParsingStage>> GetPaginationStage(NpgSqlSession session, CancellationToken ct)
    {
        ParsingStageQuery stageQuery = new(Name: ParsingStageConstants.PAGINATION, WithLock: true);
        Maybe<ParsingStage> stage = await ParsingStage.GetStage(session, stageQuery, ct);
        return stage;
    }

    private static async Task<ProcessingParserLink[]> GetParserLinksForPagedUrlsExtraction(
        NpgSqlSession session,
        CancellationToken ct)
    {
        ProcessingParserLinkQuery linksQuery = new(OnlyNotFetched: true, RetryCountThreshold: 5, WithLock: true);
        ProcessingParserLink[] links = await IEnumerable<ProcessingParserLink>.QueryMany(session, linksQuery, ct);
        return links;
    }

    private static bool CanSwitchNextStage(ProcessingParserLink[] links)
    {
        return links.Length == 0;
    }

    private static async Task SwitchNextStage(
        ParsingStage stage, 
        NpgSqlSession session, 
        Serilog.ILogger logger, 
        CancellationToken ct)
    {
        ParsingStage catalogueStage = stage.ToCatalogueStage();
        await catalogueStage.Update(session, ct);
        await session.UnsafeCommit(ct);
        logger.Information("Switched to: {Stage}", catalogueStage.Name);
    }

    private static async Task ProcessPagedUrlExtraction(
        ProcessingParserLink[] links, 
        BrowserFactory browsers, 
        AvitoBypassFactory bypassFactory, 
        NpgSqlSession session,
        Serilog.ILogger logger)
    {
        IBrowser browser = await browsers.ProvideBrowser();
        
        for (int i = 0; i < links.Length; i++)
        {
            ProcessingParserLink link = links[i];
            
            IExtractPagedUrlsCommand command = new ExtractPagedUrlsCommand(() => browser.GetPage(), bypassFactory)
                .UseLogging(logger);
            
            links[i] = await ProcessPagedUrlExtractionFromParserLink(link, command, session);
        }
        
        await links.UpdateMany(session);
        await browser.DestroyAsync();
    }
    
    private static async Task<ProcessingParserLink> ProcessPagedUrlExtractionFromParserLink(
        ProcessingParserLink link, 
        IExtractPagedUrlsCommand command, 
        NpgSqlSession session)
    {
        try
        {
            AvitoCataloguePage[] pages = await command.Extract(link.Url);
            await pages.AddMany(session);
            link.Marker.MarkProcessed();
        }
        catch (Exception)
        {
            link.Counter.Increase();
        }
        
        return link;
    }

    private static async Task FinishTransaction(NpgSqlSession session, Serilog.ILogger logger, CancellationToken ct)
    {
        try
        {
            await session.UnsafeCommit(ct);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error at committing transaction.");
        }
    }
}
