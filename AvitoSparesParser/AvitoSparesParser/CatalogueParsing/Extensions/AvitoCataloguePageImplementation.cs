using AvitoFirewallBypass;
using AvitoSparesParser.Common;
using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace AvitoSparesParser.CatalogueParsing.Extensions;

public static class AvitoCataloguePageImplementation
{
    extension(AvitoCataloguePage page)
    {
        public async Task<AvitoCatalogueSpare[]> SparesArray(
            BrowserFactory browsers,
            AvitoBypassFactory bypasses)
        {
            IBrowser browser = await browsers.ProvideBrowser(headless: false);
            try
            {
                IPage browserPage = await browser.GetPage();
                await browserPage.QuickNavigate(page.Url);
                // await browserPage.NavigatePage(page.Url);
                if (!await bypasses.Create(browserPage).Bypass()) return [];
                await browserPage.ScrollBottom();

                IElementHandle[] webElements = await browserPage.GetElements();
                List<AvitoCatalogueSpare> results = new(webElements.Length);

                await foreach (AvitoCatalogueSpare result in webElements.ExtractCatalogueSpares(browserPage))
                    results.Add(result);

                return [.. results];
            }
            finally
            {
                await browser.DestroyAsync();
            }
        }
    }

    extension(IElementHandle[] webElements)
    {
        private async IAsyncEnumerable<AvitoCatalogueSpare> ExtractCatalogueSpares(IPage page)
        {
            int retryAmount = 5;
            int length = webElements.Length;
            for (int i = 0; i < length; i++)
            {
                IElementHandle webElement = webElements[i];
                Maybe<string> itemId = await webElement.GetAttribute("data-item-id");
                if (!itemId.HasValue) continue;

                Maybe<IElementHandle> titleContainer = await webElement.GetElementRetriable("div.iva-item-listTopBlock-n6Rva", retryAmount: retryAmount);
                if (!titleContainer.HasValue) continue;

                Maybe<IElementHandle> itemUrlContainer = await titleContainer.Value.GetElementRetriable("a[itemprop='url']", retryAmount: retryAmount);
                if (!itemUrlContainer.HasValue) continue;

                Maybe<string> itemUrlAttribueValue = await itemUrlContainer.Value.GetAttribute("href");
                if (!itemUrlAttribueValue.HasValue) continue;

                Maybe<IElementHandle> itemImage = await webElement.GetElementRetriable("div[data-marker='item-image']", retryAmount: retryAmount);
                if (!itemImage.HasValue) continue;
                await itemImage.Value.HoverAsync();

                Maybe<IElementHandle> updatedItemImage = await page.GetElementRetriable($"div[data-marker='item'][data-item-id='{itemId.Value}']", retryAmount: retryAmount);
                if (!updatedItemImage.HasValue) continue;

                Maybe<IElementHandle> photoSliderList = await updatedItemImage.Value.GetElementRetriable("ul.photo-slider-list-R0jle", retryAmount: retryAmount);
                if (!photoSliderList.HasValue) continue;

                IElementHandle[] photoElements = await photoSliderList.Value.GetElements("li");
                IReadOnlyList<string> photos = await GetItemPhotos(photoElements);

                string itemIdValue = itemId.Value;
                string itemUrlValue = $"https://avito.ru{itemUrlAttribueValue.Value}";
                AvitoCatalogueItemMetadata metadata = new(itemIdValue, itemUrlValue);
                PlainJsonStringArray photosJson = PlainJsonStringArray.FromEntries(photos);
                yield return AvitoCatalogueSpare.New(metadata, photosJson);
            }
        }
    }

    extension(IElementHandle[] photoElements)
    {
        private async Task<IReadOnlyList<string>> GetItemPhotos()
        {
            int retryAmount = 5;
            List<string> photos = [];
            foreach (IElementHandle photo in photoElements)
            {
                Maybe<IElementHandle> imageElement = await photo.GetElementRetriable("img", retryAmount: retryAmount);
                if (!imageElement.HasValue) continue;
                Maybe<string> srcSetAttribute = await imageElement.Value.GetAttribute("srcset");
                if (!srcSetAttribute.HasValue) continue;
                string[] sets = srcSetAttribute.Value.Split(',');
                string highQualityImageUrl = sets[^1].Split(' ')[0];
                photos.Add(highQualityImageUrl);
            }
            return photos;
        }
    }

    extension(IPage page)
    {
        private async Task<IElementHandle[]> GetElements()
        {
            Maybe<IElementHandle> rootItemsContainer = await page.GetElementRetriable("div.index-root-H81wX", retryAmount: 5);
            if (!rootItemsContainer.HasValue) return [];
            IElementHandle[] elements = await rootItemsContainer.Value.GetElements("div[data-marker='item']");
            return elements.Length == 0 ? [] : elements;
        }
    }
}