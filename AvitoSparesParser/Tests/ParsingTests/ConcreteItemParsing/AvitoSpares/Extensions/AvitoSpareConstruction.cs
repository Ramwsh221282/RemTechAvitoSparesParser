using AvitoFirewallBypass;
using ParsingSDK.Parsing;
using PuppeteerSharp;
using Tests.ParsingTests.CatalogueParsing;
using Tests.ParsingTests.Common;
using Tests.ParsingTests.ConcreteItemParsing.AvitoWebPages;

namespace Tests.ParsingTests.ConcreteItemParsing.AvitoSpares.Extensions;

public static class AvitoSpareConstruction
{
    extension(AvitoSpare)
    {
        public static Maybe<AvitoSpare> TryConstruct(AvitoCatalogueSpare spare, Maybe<PriceInformation> price, TextList text)
        {
            if (!price.HasValue) return Maybe<AvitoSpare>.None();
            if (text.Empty()) return  Maybe<AvitoSpare>.None();
            
            return Maybe<AvitoSpare>.Some(new AvitoSpare(spare, price.Value, text));
        }
        
        public static async Task<Maybe<AvitoSpare>> TryExtract(AvitoSpareConstructionDependencies dependencies, AvitoSpareRequirements<AvitoSpareWebPage> requirements)
        {
            dependencies.Deconstruct(out AvitoCatalogueSpare spare, out IBrowser browser, out AvitoBypassFactory bypass);
            requirements.Deconstruct(out Func<AvitoSpareWebPage, Task<Maybe<PriceInformation>>> prices, out Func<AvitoSpareWebPage, Task<TextList>>[] texts);
            
            Maybe<AvitoSpareWebPage> pageAttempt = await spare.BypassedSparePage(bypass, browser);
            if (!pageAttempt.HasValue) return Maybe<AvitoSpare>.None();
            Maybe<PriceInformation> price = await prices(pageAttempt.Value);
            TextList textList = await TextList.FromRequirementsAsync(pageAttempt.Value, texts);
            return TryConstruct(spare, price, textList);
        }
    }
}