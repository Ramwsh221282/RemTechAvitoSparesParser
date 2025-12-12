using AvitoSparesParser.Common;

using ParsingSDK.Parsing;

using PuppeteerSharp;

namespace AvitoSparesParser.ConcreteItemParsing.AvitoWebPages.Extensions;

public static class AvitoSpareWebPagePropertiesExtraction
{
    extension(AvitoSpareWebPage page)
    {
        public async Task<TextList> ExtractCharacteristicsToTextList()
        {
            TextList list = TextList.Empty();
            Maybe<IElementHandle> characteristicsContainer = await page.TryGetElement("ul.params__paramsList___XzY3MG");
            if (!characteristicsContainer.HasValue) return list;
            IElementHandle[] characteristicNodes = await characteristicsContainer.Value.GetElements("li");
            if (characteristicNodes.Length == 0) return list;
            Maybe<string>[] characteristics = await characteristicNodes.MapArrayAsync(CharacteristicFromCharacteristicNode); 
            list.Add(characteristics.Where(c => c.HasValue).Select(c => c.Value));
            return list;
        }

        public async Task<TextList> ExtractDescriptionPartsToTextList()
        {
            TextList list = TextList.Empty();
            const int retryCount = 5;
            Maybe<IElementHandle> descriptionContainer = await page.TryGetElement("div[id='bx_item-description']");
            if (!descriptionContainer.HasValue) return list;
            Maybe<IElementHandle> elementWithDescription = await descriptionContainer.Value.GetElementRetriable("div[data-marker='item-view/item-description']", retryCount);
            if (!elementWithDescription.HasValue) return list;
            IElementHandle[] descriptionParts = await elementWithDescription.Value.GetElements("p");
            if (descriptionParts.Length == 0) return list;
            Maybe<string>[] textParts = await descriptionParts.MapArrayAsync(WebDescriptionPartToString);
            list.Add(textParts.Where(p => p.HasValue).Select(p => p.Value));
            return list;
        }
        
        public async Task<TextList> ExtractTitleToTextList()
        {
            TextList list = TextList.Empty();
            const int retryAmount = 5;
            Maybe<IElementHandle> titleContainer = await page.TryGetElement("div.js-item-view-title-info");
            if (!titleContainer.HasValue) return list;
            Maybe<IElementHandle> titleElement = await titleContainer.Value.GetElementRetriable("h1", retryAmount);
            if (!titleElement.HasValue) return list;
            Maybe<string> text = await titleElement.Value.GetElementInnerText();
            if (text.HasValue) list.Add(text.Value);
            return list;
        }
        
        public async Task<Maybe<PriceInformation>> ExtractPriceInformation()
        {
            int retryAmount = 5;
            Maybe<IElementHandle> priceContainer = await page.TryGetElement("div.styles__item-price___ZDZjZj");
            if (!priceContainer.HasValue) return Maybe<PriceInformation>.None();
            Maybe<IElementHandle> elementWithPriceValueContainer = await page.TryGetElement("span[id='bx_item-price-value']");
            if (!elementWithPriceValueContainer.HasValue) return Maybe<PriceInformation>.None();
            Maybe<IElementHandle> elementWithPriceAttribute = await elementWithPriceValueContainer.Value.GetElementRetriable("span[itemprop='price']", retryAmount);
            if (!elementWithPriceAttribute.HasValue) return Maybe<PriceInformation>.None();
            
            Maybe<string> priceValue = await elementWithPriceAttribute.Value.GetAttribute("content");
            if (!priceValue.HasValue) return Maybe<PriceInformation>.None();
            
            bool isPriceParsableToLongNumber = long.TryParse(priceValue.Value, out long priceResult);
            if (!isPriceParsableToLongNumber) return Maybe<PriceInformation>.None();
            Maybe<string> fullText = await priceContainer.Value.GetElementInnerText();

            return fullText.HasValue switch
            {
                false => Maybe<PriceInformation>.None(),
                true => fullText.Value.Contains("НДС", StringComparison.OrdinalIgnoreCase) switch
                { 
                    false => Maybe<PriceInformation>.Some(new PriceInformation(priceResult, true)),
                    true => Maybe<PriceInformation>.Some(new PriceInformation(priceResult, true)),
                }
            };
        }
    }

    private static async Task<Maybe<string>> WebDescriptionPartToString(IElementHandle part)
    {
        Maybe<string> text = await part.GetElementInnerText();
        return text;
    }
    
    private static async Task<Maybe<string>> CharacteristicFromCharacteristicNode(IElementHandle node)
    {
        Maybe<string> nameValuePair = await node.GetElementInnerText();
        if (!nameValuePair.HasValue) return Maybe<string>.None();
        string[] splitted = nameValuePair.Value.Trim().Split(':');
        string characteristic = $" {splitted[0]} {splitted[^1]} ";
        return Maybe<string>.Some(characteristic);
    }
}