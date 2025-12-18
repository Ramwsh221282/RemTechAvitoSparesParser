using AvitoSparesParser.CatalogueParsing;
using AvitoSparesParser.CatalogueParsing.Extensions;
using AvitoSparesParser.Commands.Common;
using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace AvitoSparesParser.Commands.ExtractPagedUrls;

public sealed class ExtractPagedUrlsCommand(
    Func<Task<IPage>> pageSource, 
    AvitoBypassFactory bypassFactory
    ) : IExtractPagedUrlsCommand
{
    public async Task<AvitoCataloguePage[]> Extract(string initialUrl)
    {
        AvitoCataloguePage[] result = await pageSource()
            .Then(p => p.PerformQuickNavigation(initialUrl, timeout: 1500))
            .Then(p => p.BypassFirewallIfPresent(bypassFactory))
            .Then(WaitForPagination)
            .Reduce(ExtractPaginationUrls);
        return result;
    }

    private static Task WaitForPagination(IPage page) => 
        page.ResilientWaitForSelector("nav[aria-label='Пагинация']", attempts: 3);

    private static async Task<AvitoCataloguePage[]> ExtractPaginationUrls(IPage page)
    {
        string[] urls = await page.EvaluateFunctionAsync<string[]>(@"
                                   () => {
                                   const currentUrl = window.location.href;
                                   const paginationSelector = document.querySelector('nav[aria-label=""Пагинация""]');
                                   if (!paginationSelector) return [currentUrl];
                                   const paginationGroupSelector = paginationSelector.querySelector('ul[data-marker=""pagination-button""]');
                                   if (!paginationGroupSelector) return [currentUrl];
                                   return Array.from(paginationGroupSelector.querySelectorAll('span[class=""styles-module-text-Z0vDE""]'))
                                               .map(s => s.innerText)
                                               .map(p => currentUrl + '&p=' + p);
                                   }
                                   ");
        return [.. urls.Select(AvitoCataloguePage.New)];
    }
}