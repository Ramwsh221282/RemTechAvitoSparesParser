using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace AvitoSparesParser.Commands.HoverCatalogueItemImages;

public sealed class HoverCatalogueItemImagesCommand(Func<Task<IPage>> pageSource) : IHoverCatalogueItemImagesCommand
{
    public async Task Hover()
    {
        IPage page = await pageSource();
        IElementHandle[] catalogueItems = await GetElements(page);
        if (catalogueItems.Length == 0) return;

        foreach (IElementHandle catalogueItem in catalogueItems)
            await HoverImageElement(catalogueItem);
    }

    private async Task<IElementHandle[]> GetElements(IPage page)
    {
        return await page.GetElementsRetriable("div[data-marker='item']");
    }

    private async Task HoverImageElement(IElementHandle element)
    {
        Maybe<IElementHandle> itemImage = await element.GetElementRetriable("div[data-marker='item-image']");
        if (!itemImage.HasValue) return;
        await itemImage.Value.HoverAsync();
    }
}