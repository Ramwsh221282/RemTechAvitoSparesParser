using AvitoSparesParser.CatalogueParsing;
using AvitoSparesParser.CatalogueParsing.Extensions;
using AvitoSparesParser.Common;
using AvitoSparesParser.ConcreteItemParsing.AvitoSpares;
using AvitoSparesParser.ConcreteItemParsing.AvitoSpares.Extensions;
using AvitoSparesParser.ConcreteItemParsing.AvitoWebPages;
using AvitoSparesParser.ParsingStages.Extensions;
using ParsingSDK.Parsing;
using ParsingSDK.TextProcessing;
using PuppeteerSharp;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace AvitoSparesParser.ParsingStages.Processes;

public static class ConcreteItemExtractingProcess
{
    extension(ParserStageProcess)
    {
        public static ParserStageProcess ConcreteItems => async (deps, ct) =>
        {
            var logger = deps.Logger.ForContext<ParserStageProcess>();
            await using NpgSqlSession session = new(deps.NpgSql);
            await session.UseTransaction(ct);

            ParsingStageQuery query = new(Name: ParsingStageConstants.CONCRETE_ITEMS, WithLock: true);
            var stage = await ParsingStage.GetStage(session, query, ct);
            if (!stage.HasValue) return;

            AvitoCatalogueSpareQuery spareQuery = new(true, 5, true, 20);
            var spares = await AvitoCatalogueSpare.GetMany(session, spareQuery, ct);
            if (spares.Length == 0)
            {
                var finalization = stage.Value.ToFinalizationStage();
                await finalization.Update(session, ct);
                await session.UnsafeCommit(ct);
                logger.Information("Switched to finalization stage.");
                return;
            }

            IBrowser browser = await deps.Browsers.ProvideBrowser(false);
            ITextTransformer textTransformer = deps.TextTransformerBuilder
                .UsePunctuationCleaner()
                .UseNewLinesCleaner()
                .UseSpacesCleaner()
                .Build();

            AvitoSpareRequirements requirements = new(AsyncSparePropertyFactory<AvitoSpareWebPage>.TitleRequirement,
                AsyncSparePropertyFactory<AvitoSpareWebPage>.IsNdsRequirement,
                AsyncSparePropertyFactory<AvitoSpareWebPage>.OemRequirement,
                AsyncSparePropertyFactory<AvitoSpareWebPage>.PriceRequirement,
                AsyncSparePropertyFactory<AvitoSpareWebPage>.TypeRequirement,
                AsyncSparePropertyFactory<AvitoSpareWebPage>.WithTextTransforming(textTransformer, AsyncSparePropertyFactory<AvitoSpareWebPage>.AddressRequirement));

            List<AvitoSpare> results = [];
            
            for (var i = 0; i < spares.Length; i++)
            {
                AvitoCatalogueSpare catalogueItem = spares[i];

                try
                {
                    AvitoSpareConstructionDependencies constructionDeps = new(catalogueItem, browser, deps.Bypasses);
                    Maybe<AvitoSpare> spare = await AvitoSpare.TryExtract(constructionDeps, requirements);
                    if (spare.HasValue) results.Add(spare.Value);
                    IHasProcessedMarker.MarkProcessed(catalogueItem);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to parse concrete item with url: {Url}. Attempt: {Attempt}",
                        catalogueItem.Metadata.Url, catalogueItem.Counter.Value);
                    IHasRetryCounter.Increase(catalogueItem);
                }
                finally
                {
                    spares[i] = catalogueItem;
                }
            }
            
            await spares.UpdateMany(session);
            await results.PersistMany(session);

            try
            {
                await session.UnsafeCommit(ct);
                logger.Information("Parsed {0} concrete items.", results.Count);
            }
            catch(Exception ex)
            {
                logger.Error(ex, "Failed to commit transaction");
            }
        };
    }
}