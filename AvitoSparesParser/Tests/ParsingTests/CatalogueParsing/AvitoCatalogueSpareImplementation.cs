using AvitoFirewallBypass;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using Tests.ParsingTests.ConcreteItemParsing.AvitoWebPages;

namespace Tests.ParsingTests.CatalogueParsing;

public static class AvitoCatalogueSpareImplementation
{
    extension(AvitoCatalogueSpare spare)
    {
        public async Task NavigatePage(IPage page)
        {
            await page.NavigatePage(spare.Metadata.Url);
        }

        public async Task<bool> NavigateWithBypassing(AvitoBypassFactory bypassFactory, IPage page)
        {
            await spare.NavigatePage(page);
            return await bypassFactory.Create(page).Bypass();
        }

        public async Task<Maybe<AvitoSpareWebPage>> BypassedSparePage(AvitoBypassFactory bypassFactory, IBrowser browser)
        {
            IPage page = await browser.GetPage();
            bool bypassed = await spare.NavigateWithBypassing(bypassFactory, page);
            if (!bypassed) return Maybe<AvitoSpareWebPage>.None();
            await page.ScrollBottom();
            return Maybe<AvitoSpareWebPage>.Some(new AvitoSpareWebPage(page));
        }
    }
}