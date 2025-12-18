using AvitoSparesParser.CatalogueParsing;
using AvitoSparesParser.CatalogueParsing.Extensions;
using AvitoSparesParser.Common;
using AvitoSparesParser.ParserProcessStarting;
using AvitoSparesParser.ParserProcessStarting.Extensions;
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
            Serilog.ILogger logger = deps.Logger.ForContext<ParserStageProcess>();
            await using NpgSqlSession session = new(deps.NpgSql);
            await session.UseTransaction(ct);

            ParsingStageQuery stageQuery = new(Name: ParsingStageConstants.PAGINATION, WithLock: true);
            Maybe<ParsingStage> stage = await ParsingStage.GetStage(session, stageQuery, ct);
            if (!stage.HasValue) return;

            ProcessingParserLinkQuery linksQuery = new(
                OnlyNotFetched: true,
                RetryCountThreshold: 5,
                WithLock: true
                );
            ProcessingParserLink[] links = await IEnumerable<ProcessingParserLink>.QueryMany(session, linksQuery, ct);
            if (links.Length == 0)
            {
                ParsingStage catalogueStage = stage.Value.ToCatalogueStage();
                await catalogueStage.Update(session, ct);
                await session.UnsafeCommit(ct);
                return;
            }

            IBrowser browser = await deps.Browsers.ProvideBrowser();

            for (int i = 0; i < links.Length; i++)
            {
                ProcessingParserLink link = links[i];
                logger.Information("Extracting catalogue pages for link url: {Url}", link.Url);

                try
                {
                    await (await browser.GetPage()).QuickNavigate(link.Url);
                    if (!await deps.Bypasses.Create(await browser.GetPage()).Bypass())
                        throw new InvalidOperationException("Bypass failed.");
                    await (await browser.GetPage()).ScrollBottom();
                    IElementHandle[] paginationElements = await GetPaginationElements(await browser.GetPage());
                    int currentPage = await GetCurrentPageFromPaginationContainer(paginationElements);
                    int lastPage = await GetMaxPageFromPaginationContainer(paginationElements);
                    AvitoCataloguePage[] pages = CreateCataloguePages(link.Url, currentPage, lastPage);
                    logger.Information("Extracted {Count} catalogue pages for link url: {Url}", pages.Length, link.Url);
                    await pages.AddMany(session);
                    link.Marker.MarkProcessed();
                }
                catch (Exception ex)
                {
                    deps.Logger.Error(ex, "Error for processing link url: {Url}", link.Url);
                    link.Counter.Increase();
                }
                finally
                {
                    links[i] = link;
                }
            }

            await browser.DestroyAsync();
            await links.UpdateMany(session);

            try
            {
                await session.UnsafeCommit(ct);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error at committing transaction.");
            }
        };

        private static async Task<IElementHandle[]> GetPaginationElements(IPage page)
        {
            Maybe<IElementHandle> element = await page.GetElementRetriable("nav[aria-label='Пагинация']");
            if (!element.HasValue) return [];
            IElementHandle[] paginationElements = await element.Value.GetElements("li");
            if (paginationElements.Length == 0) return [];
            return paginationElements;
        }

        private static async Task<int> GetCurrentPageFromPaginationContainer(IElementHandle[] elements)
        {
            int currentPage = 0;
            foreach (IElementHandle element in elements)
            {
                Maybe<IElementHandle> selectedPage = await element.GetElementRetriable("span[aria-current='page']");
                if (!selectedPage.HasValue) continue;
                Maybe<IElementHandle> pageNumberElement = await selectedPage.Value.GetElementRetriable("span.styles-module-text-Z0vDE");
                if (!pageNumberElement.HasValue) continue;
                Maybe<string> pageNumberText = await pageNumberElement.Value.GetElementInnerText();
                if (!pageNumberText.HasValue) continue;
                currentPage = int.Parse(pageNumberText.Value);
                break;
            }

            return currentPage;
        }

        private static async Task<int> GetMaxPageFromPaginationContainer(IElementHandle[] elements)
        {
            int maxPage = 0;
            foreach (IElementHandle element in elements)
            {
                Maybe<IElementHandle> pageNumberElement = await element.GetElementRetriable("span.styles-module-text-Z0vDE");
                if (!pageNumberElement.HasValue) continue;
                Maybe<string> pageNumberText = await pageNumberElement.Value.GetElementInnerText();
                if (!pageNumberText.HasValue) continue;
                if (!int.TryParse(pageNumberText.Value, out int maxPageValue)) continue;
                if (maxPage < maxPageValue) maxPage = maxPageValue;
            }

            return maxPage;
        }

        private static AvitoCataloguePage[] CreateCataloguePages(
            string originUrl,
            int currentPage,
            int maxPage
        )
        {
            int pageCounter = currentPage;
            List<AvitoCataloguePage> pages = new(pageCounter + 1);
            while (pageCounter <= maxPage)
            {
                string urlValue = $"{originUrl}&p={pageCounter}";
                AvitoCataloguePage page = AvitoCataloguePage.New(urlValue);
                pages.Add(page);
                pageCounter++;
            }

            return [.. pages];
        }
    }
}
