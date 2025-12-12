using AvitoFirewallBypass;

using AvitoSparesParser.CatalogueParsing;
using AvitoSparesParser.Common;
using AvitoSparesParser.ConcreteItemParsing.AvitoSpares;
using AvitoSparesParser.ConcreteItemParsing.AvitoSpares.Extensions;
using AvitoSparesParser.ConcreteItemParsing.AvitoSpareSinking;
using AvitoSparesParser.ConcreteItemParsing.AvitoWebPages;
using AvitoSparesParser.ConcreteItemParsing.AvitoWebPages.Extensions;

using Microsoft.Extensions.DependencyInjection;

using ParsingSDK.Parsing;
using ParsingSDK.TextProcessing;

using PuppeteerSharp;

namespace Tests.ParsingTests;

public sealed class SparesParserTests(SparesParsingFixture fixture) : IClassFixture<SparesParsingFixture>
{
    private readonly IServiceProvider _sp = fixture.Services;

    [Fact]
    private async Task Parse_Single_Advertisement()
    {
        const string url = "https://www.avito.ru/all/zapchasti_i_aksessuary/zapchasti-ASgBAgICAUQKJA?cd=1&q=ponsse";
        BrowserFactory browsers = _sp.GetRequiredService<BrowserFactory>();
        AvitoBypassFactory bypasses = _sp.GetRequiredService<AvitoBypassFactory>();
        AvitoCataloguePage page = AvitoCataloguePage.New(url);
        AvitoCatalogueSpare[] items = await page.SparesArray(browsers, bypasses);
        Assert.NotEmpty(items);
    }

    [Fact]
    private async Task Parse_Advertisement_Pages()
    {
        const string url = "https://www.avito.ru/all/zapchasti_i_aksessuary/zapchasti-ASgBAgICAUQKJA?cd=1&q=ponsse";
        BrowserFactory browsers = _sp.GetRequiredService<BrowserFactory>();
        AvitoBypassFactory bypasses = _sp.GetRequiredService<AvitoBypassFactory>();
        TextTransformerBuilder transformerBuilder = _sp.GetRequiredService<TextTransformerBuilder>();

        ITextTransformer transformer = transformerBuilder
            .UsePunctuationCleaner()
            .UseNewLinesCleaner()
            .UseEmojiCleaner()
            .UseSpacesCleaner()
            .Build();

        AvitoCataloguePage page = AvitoCataloguePage.New(url);
        AvitoCatalogueSpare[] items = await page.SparesArray(browsers, bypasses);

        AvitoSpareRequirements<AvitoSpareWebPage> reqs = new(
          Prices: p => p.ExtractPriceInformation(),
          Texts: [
              p => p.ExtractTitleToTextList(),
              p => p.ExtractCharacteristicsToTextList(),
              p => p.ExtractDescriptionPartsToTextList()
            ]
        );

        string resultsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "results");
        Directory.CreateDirectory(resultsPath);

        IBrowser browser = await browsers.ProvideBrowser(headless: false);

        Func<AvitoSpare, string> sparePathFn = sp => Path.Combine(resultsPath, $"{Guid.NewGuid()}.txt");
        Func<AvitoCatalogueSpare, AvitoSpareConstructionDependencies> depFn = spare => new(spare, browser, bypasses);

        Maybe<AvitoSpare>[] spares = await items.MapArrayAsync(i => AvitoSpare.TryExtract(depFn(i), reqs));
        AvitoSpare[] successSpares = [.. spares.Where(s => s.HasValue).Select(s => s.Value)];
        await browser.DestroyAsync();
        Assert.NotEmpty(successSpares);
        await successSpares.InvokeForEach(s => s.Texts.InvokeForEach(t => new AsyncSpareTextFile(transformer.TransformText(t), sparePathFn(s)).Write()));
    }
}