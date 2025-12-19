using AvitoFirewallBypass;
using AvitoSparesParser.AvitoSpareContext;
using AvitoSparesParser.CatalogueParsing;
using AvitoSparesParser.Commands.ExtractCataloguePageItems;
using AvitoSparesParser.Commands.ExtractConcretePageItem;
using AvitoSparesParser.Commands.ExtractPagedUrls;
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
        
        AvitoCataloguePage[] pages = await new ExtractPagedUrlsCommand(() => browser.GetPage(), Bypasses)
            .UseLogging(Logger)
            .Extract(TargetUrl);
        
        await browser.DisposeAsync();
        Assert.NotEmpty(pages);
        
    }
    
    [Fact]
    private async Task Extract_Catalogue_Page_Items()
    {
        IBrowser browser = await Browsers.ProvideBrowser();
        AvitoCataloguePage[] pages = await new ExtractPagedUrlsCommand(() => browser.GetPage(), Bypasses)
            .UseLogging(Logger)
            .Extract(TargetUrl);

        foreach (AvitoCataloguePage page in pages)
        {
            AvitoSpare[] items = await new ExtractCataloguePagesItemCommand(() => browser.GetPage(), Bypasses)
                .UseLogging(Logger)
                .Extract(page);
            
            Assert.NotEmpty(items);
        }
    }
    
    [Fact]
    private async Task Extract_Concrete_Item()
    {
        IBrowser browser = await Browsers.ProvideBrowser();
        
        AvitoCataloguePage[] pages = await new ExtractPagedUrlsCommand(() => browser.GetPage(), Bypasses)
            .UseLogging(Logger)
            .Extract(TargetUrl);

        foreach (AvitoCataloguePage page in pages)
        {
            AvitoSpare[] items = await new ExtractCataloguePagesItemCommand(() => browser.GetPage(), Bypasses)
                .UseLogging(Logger)
                .Extract(page);

            foreach (AvitoSpare cataologueSpare in items)
            {
                AvitoSpare concrete = await new ExtractConcretePageItemCommand(() => browser.GetPage(), Bypasses)
                    .UseLogging(Logger)
                    .Extract(cataologueSpare);
                int a = 0;
            }
        }
    }
}