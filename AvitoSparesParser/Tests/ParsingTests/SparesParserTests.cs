using AvitoFirewallBypass;
using Microsoft.Extensions.DependencyInjection;
using ParsingSDK.Parsing;
using ParsingSDK.TextProcessing;
using PuppeteerSharp;
using Tests.ParsingTests.CatalogueParsing;
using Tests.ParsingTests.Common;
using Tests.ParsingTests.ConcreteItemParsing.AvitoSpares;
using Tests.ParsingTests.ConcreteItemParsing.AvitoSpares.Extensions;
using Tests.ParsingTests.ConcreteItemParsing.AvitoSpareSinking;
using Tests.ParsingTests.ConcreteItemParsing.AvitoWebPages;
using Tests.ParsingTests.ConcreteItemParsing.AvitoWebPages.Extensions;

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
         AvitoSpare[] successSpares = [..spares.Where(s => s.HasValue).Select(s => s.Value)];
        await browser.DestroyAsync();
         Assert.NotEmpty(successSpares);
        await successSpares.InvokeForEach(s => s.Texts.InvokeForEach(t => new AsyncSpareTextFile(transformer.TransformText(t), sparePathFn(s)).Write()));
    }

    [Fact]
    private void Write_Texts()
    {
        string resultsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "results");

        using StreamReader reader = File.OpenText("Вся номенклатура.txt");
        int limit = 20;
        int counter = 0;
        while (!reader.EndOfStream)
        {
            if (counter > limit)
                break;
            
            string? line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.Contains("\t"))
            {
                string saveFileName1 = Guid.NewGuid() + ".txt";
                string saveFileName2 = Guid.NewGuid() + ".txt";
                string saveFilePath1 = Path.Combine(resultsPath, saveFileName1);
                string saveFilePath2 = Path.Combine(resultsPath, saveFileName2);
                string[] contents = line.Split("\t");
                using StreamWriter sw1 = File.CreateText(saveFilePath1);
                using StreamWriter sw2 = File.CreateText(saveFilePath2);
                sw1.WriteLine(contents[0].Trim());
                sw2.WriteLine(contents[1].Trim());
            }

            counter++;
        }

        int a = 0;
    }
}