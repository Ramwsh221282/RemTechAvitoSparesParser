using AvitoSparesParser.ConcreteItemParsing.AvitoWebPages;
using ParsingSDK.Parsing;
using ParsingSDK.TextProcessing;
using PuppeteerSharp;

namespace AvitoSparesParser.ConcreteItemParsing.AvitoSpares.Extensions;

public static class AvitoSpareRequirementsWebPage
{
    extension(AsyncSparePropertyFactory<AvitoSpareWebPage>)
    {
        public static AsyncSparePropertyFactory<AvitoSpareWebPage> TypeRequirement =>
            async (page) =>
            {
                Maybe<IElementHandle> breadcrumbsContainer = await page.TryGetElement(
                    "div[id='bx_item-breadcrumbs']"
                );
                if (!breadcrumbsContainer.HasValue)
                    return AvitoSpareProperty.Nothing();
                IElementHandle[] breadcrumbs = await breadcrumbsContainer.Value.GetElements(
                    "span[itemprop='itemListElement']"
                );
                if (breadcrumbs.Length == 0)
                    return AvitoSpareProperty.Nothing();
                Maybe<string> type = await breadcrumbs[^1].GetElementInnerText();
                return AvitoSpareProperty.Something("type", type.Value);
            };

        public static AsyncSparePropertyFactory<AvitoSpareWebPage> TitleRequirement =>
            async (page) =>
            {
                Maybe<IElementHandle> titleContainer = await page.TryGetElement(
                    "div.js-item-view-title-info"
                );
                if (!titleContainer.HasValue)
                    return AvitoSpareProperty.Nothing();
                Maybe<IElementHandle> h1 = await titleContainer.Value.GetElementRetriable(
                    "h1",
                    retryAmount: 5
                );
                if (!h1.HasValue)
                    return AvitoSpareProperty.Nothing();
                Maybe<string> title = await h1.Value.GetElementInnerText();
                return AvitoSpareProperty.Something("title", title.Value);
            };

        public static AsyncSparePropertyFactory<AvitoSpareWebPage> OemRequirement =>
            async (page) =>
            {
                Maybe<IElementHandle> paramsContainer = await page.TryGetElement(
                    "div[id='bx_item-params']"
                );
                if (!paramsContainer.HasValue)
                    return AvitoSpareProperty.Nothing();
                Maybe<IElementHandle> listContainer =
                    await paramsContainer.Value.GetElementRetriable("ul", retryAmount: 5);
                IElementHandle[] @params = await listContainer.Value.GetElements("li");
                foreach (IElementHandle param in @params)
                {
                    Maybe<string> text = await param.GetElementInnerText();
                    if (!text.HasValue)
                        continue;
                    if (!text.Value.Contains("Номер запчасти"))
                        continue;
                    string oem = text.Value.Split(':', StringSplitOptions.TrimEntries)[^1].Trim();
                    return AvitoSpareProperty.Something("oem", oem);
                }

                return AvitoSpareProperty.Nothing();
            };

        public static AsyncSparePropertyFactory<AvitoSpareWebPage> PriceRequirement =>
            async (page) =>
            {
                Maybe<IElementHandle> priceContainer = await page.TryGetElement(
                    "span[id='bx_item-price-value']"
                );
                if (!priceContainer.HasValue)
                    return AvitoSpareProperty.Nothing();
                Maybe<IElementHandle> priceValueContainer =
                    await priceContainer.Value.GetElementRetriable(
                        "span[itemprop='price']",
                        retryAmount: 5
                    );
                if (!priceValueContainer.HasValue)
                    return AvitoSpareProperty.Nothing();
                Maybe<string> price = await priceValueContainer.Value.GetAttribute("content");
                if (!price.HasValue)
                    return AvitoSpareProperty.Nothing();
                if (!long.TryParse(price.Value, out long result))
                    return AvitoSpareProperty.Nothing();
                return AvitoSpareProperty.Something("price", result);
            };

        public static AsyncSparePropertyFactory<AvitoSpareWebPage> IsNdsRequirement =>
            async (page) =>
            {
                Maybe<IElementHandle> priceContainer = await page.TryGetElement(
                    "span[id='bx_item-price-value']"
                );
                if (!priceContainer.HasValue)
                    return AvitoSpareProperty.Nothing();
                Maybe<IElementHandle> ndsContainer = await priceContainer.Value.GetElementRetriable(
                    "span.style__price-value-additional___XzI0NG"
                );
                if (!ndsContainer.HasValue)
                    return AvitoSpareProperty.Something("is_nds", false);
                return AvitoSpareProperty.Something("is_nds", true);
            };

        public static AsyncSparePropertyFactory<AvitoSpareWebPage> WithTextTransforming(
            ITextTransformer transformer,
            AsyncSparePropertyFactory<AvitoSpareWebPage> origin
        ) =>
            async page =>
            {
                Maybe<AvitoSpareProperty> property = await origin(page);
                if (!property.HasValue)
                    return AvitoSpareProperty.Nothing();
                object propertyValue = property.Value.Value;
                if (propertyValue.GetType() != typeof(string))
                    return property;
                string transformed = transformer.TransformText((string)propertyValue);
                return AvitoSpareProperty.Something(property.Value.Name, transformed);
            };

        public static AsyncSparePropertyFactory<AvitoSpareWebPage> AddressRequirement =>
            async page =>
            {
                Maybe<IElementHandle> addressContainer = await page.TryGetElement(
                    "div.style__item-map___XzQ5MT"
                );
                if (!addressContainer.HasValue)
                    AvitoSpareProperty.Nothing();
                Maybe<IElementHandle> locationContainer =
                    await addressContainer.Value.GetElementRetriable(
                        "div.style__item-map-location___XzQ5MT"
                    );
                if (!locationContainer.HasValue)
                    return AvitoSpareProperty.Nothing();
                Maybe<IElementHandle> locationValueContainer =
                    await locationContainer.Value.GetElementRetriable(
                        "div.style__item-address___XzQ5MT"
                    );
                if (!locationValueContainer.HasValue)
                    return AvitoSpareProperty.Nothing();
                Maybe<string> locationValue =
                    await locationValueContainer.Value.GetElementInnerText();
                if (!locationValue.HasValue)
                    return AvitoSpareProperty.Nothing();
                return AvitoSpareProperty.Something("address", locationValue.Value);
            };
    }
}
