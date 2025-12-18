using AvitoFirewallBypass;
using AvitoSparesParser.CatalogueParsing;
using AvitoSparesParser.Commands.ExtractPagedUrls;
using AvitoSparesParser.Commands.HoverCatalogueItemImages;
using AvitoSparesParser.Commands.PrepareAvitoPage;
using Microsoft.Extensions.DependencyInjection;
using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace Tests.ParsingCommandsTests;

public sealed class ParsingCommandTest(IntegrationalTestsFixture fixture) : IClassFixture<IntegrationalTestsFixture>
{
    private BrowserFactory Browsers { get; } = fixture.Services.GetRequiredService<BrowserFactory>();
    private AvitoBypassFactory Bypasses { get; } = fixture.Services.GetRequiredService<AvitoBypassFactory>();
    private Serilog.ILogger Logger { get; } = fixture.Services.GetRequiredService<Serilog.ILogger>();

    private const string TargetUrl = 
        "https://www.avito.ru/all/zapchasti_i_aksessuary/zapchasti/dlya_gruzovikov_i_spetstehniki/texnika_dlia_lesozagotovki-ASgBAgICA0QKJKwJjGT46w7G1oED?cd=1&f=ASgBAgICBEQKJKwJjGSexw346j_46w7G1oED";

    [Fact]
    private async Task Extract_Catalogue_Paged_Urls()
    {
        IBrowser browser = await Browsers.ProvideBrowser();
        
        await new PrepareAvitoPageCommand(() => browser.GetPage(), Bypasses, async p => await p.ScrollBottom())
            .UseLogging(Logger)
            .Prepare(() => TargetUrl);
        
        AvitoCataloguePage[] pages = await new ExtractPagedUrlsCommand(() => browser.GetPage(), Bypasses)
            .UseLogging(Logger)
            .Extract(TargetUrl);
        
        await browser.DisposeAsync();
        Assert.NotEmpty(pages);
    }
    
    [Fact]
    private async Task Extract_Catalogue_Page_Items()
    {
        var installed = new BrowserFetcher().GetInstalledBrowsers();
        await Browsers.LoadBrowser();
        IBrowser browser = await Browsers.ProvideBrowser();
        
        await new PrepareAvitoPageCommand(() => browser.GetPage(), Bypasses, async p => await p.ScrollBottom())
            .UseLogging(Logger)
            .Prepare(() => TargetUrl);
        
        AvitoCataloguePage[] pages = await new ExtractPagedUrlsCommand(() => browser.GetPage(), Bypasses)
            .UseLogging(Logger)
            .Extract(TargetUrl);
        
        await new HoverCatalogueItemImagesCommand(() => browser.GetPage())
            .UseLogging(Logger)
            .Hover();

        const string javaScript = @"() => {
                    const photoExtractFn = (item) => {
                                    const photoListSelector = item.querySelector('ul.photo-slider-list-R0jle');
                                    if (!photoListSelector) return [];
                                    return Array.from(photoListSelector.querySelectorAll('li')).map(s => {
                                        const photo = s.querySelector('img');
                                        if (!photo) return '';
                                        const srcSet = photo.getAttribute('srcset');
                                        if (!srcSet) return '';
                                        const splittedParts = srcSet.split(',');
                                        return splittedParts[splittedParts.length-1].split(' ')[0];
                                    });
                                };

                                const itemSelectors = Array.from(document.querySelectorAll('div[data-marker=""item""]'));
                                const data = itemSelectors.map((i) => {
                                      // url extraction    
                                      const urlValue = ""https://avito.ru"" + i.querySelector('h2[itemprop=""name""]')
                                                                                .querySelector('a[itemprop=""url""]')
                                                                                .getAttribute(""href"");
                                      // price and is nds extraction
                                      const priceSelector = i.querySelector('p[data-marker=""item-price""]');
                                      const priceValue = priceSelector.querySelector('meta[itemprop=""price""]').getAttribute(""content"");
                                      const isNds = priceSelector.innerText.includes(""НДС"");
                                      // id
                                      const idValue = ""avito_vehicle_"" + i.getAttribute(""data-item-id"");
                                      // address
                                      const address = i.querySelector('div[data-marker=""item-location""]').querySelector('span[title]').innerText;
                                      // photos
                                      const photos = photoExtractFn(i);     
                                      // oem extraction
                                      const oemValue = i.querySelector('p[data-marker=""item-oem-number""]')?.innerText;
                                                                
                                      return { url: urlValue, price: priceValue, isNds: isNds, id: idValue, address: address, photos: photos, oem: oemValue }
                                  });
                                  return data; 
                            }
";

        IPage page = await browser.GetPage();
        JsonData[] data = await page.EvaluateFunctionAsync<JsonData[]>(javaScript);
        JsonData[] withNoPhotos = [..data.Where(d => d.Photos != null && d.Photos.All(string.IsNullOrWhiteSpace))];
        int a = 0;
    }

    private sealed class JsonData
    {
        public string? Url { get; set; }
        public string? Price { get; set; }
        public bool IsNds { get; set; }
        public string? Id { get; set; }
        public string? Address { get; set; }
        public string[]? Photos { get; set; }
        public string? Oem { get; set; }

        private bool AllPropertiesSet()
        {
            return !string.IsNullOrWhiteSpace(Url) &&
                   PriceIsNotZero() &&
                   !string.IsNullOrWhiteSpace(Id) &&
                   !string.IsNullOrWhiteSpace(Address) &&
                   Photos != null &&
                   !string.IsNullOrWhiteSpace(Oem);
        }

        private bool PriceIsNotZero()
        {
            return !string.IsNullOrWhiteSpace(Price) && Price != "0";
        }
    }
}