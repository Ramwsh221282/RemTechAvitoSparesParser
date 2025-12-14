using AvitoSparesParser.CatalogueParsing;
using AvitoSparesParser.CatalogueParsing.Extensions;
using AvitoSparesParser.ParsingStages.Extensions;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace AvitoSparesParser.ParsingStages.Processes;

public static class CatalogueItemsExtractingProcess
{
    extension(ParserStageProcess)
    {
        public static ParserStageProcess CatalogueItemsExtracting => async (deps, ct) =>
        {
            Serilog.ILogger logger = deps.Logger.ForContext<ParserStageProcess>();
            await using NpgSqlSession session = new(deps.NpgSql);
            await session.UseTransaction(ct);

            ParsingStageQuery stageQuery = new(Name: ParsingStageConstants.CATALOGUE, WithLock: true);
            Maybe<ParsingStage> stage = await ParsingStage.GetStage(session, stageQuery, ct);
            if (!stage.HasValue) return;

            AvitoCataloguePageQuery pagesQuery = new(UnprocessedOnly: true, RetryThreshold: 5, WithLock: true);
            AvitoCataloguePage[] pages = await IEnumerable<AvitoCataloguePage>.GetMany(session, pagesQuery, ct);
            if (pages.Length == 0)
            {
                ParsingStage concreteItemsStage = stage.Value.ToConcreteItemsStage();
                await concreteItemsStage.Update(session, ct);
                await session.UnsafeCommit(ct);
                logger.Information("Switched to concrete items stage.");
                return;
            }

            for (int i = 0; i < pages.Length; i++)
            {
                AvitoCataloguePage page = pages[i];
                logger.Information("Extracting catalogue items from {Url}", page.Url);
                try
                {
                    AvitoCatalogueSpare[] catalogueItems = await page.SparesArray(deps.Browsers, deps.Bypasses);
                    await catalogueItems.PersistMany(session);
                    page.Marker.MarkProcessed();
                    logger.Information("Fetched {Count} items from page: {Url}", catalogueItems.Length, page.Url);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error at extracting catalogue items from {Url}", page.Url);
                    page.Counter.Increase();
                }
                finally
                {
                    pages[i] = page;
                }
            }

            await pages.UpdateMany(session);

            try
            {
                await session.UnsafeCommit(ct);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to commit transaction.");
            }

        };
    }
}
