using AvitoSparesParser.AvitoSpareContext;
using AvitoSparesParser.CatalogueParsing;
using AvitoSparesParser.Commands.HoverCatalogueItemImages;
using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace AvitoSparesParser.Commands.ExtractCataloguePageItems;

public sealed class ExtractCataloguePagesItemCommand(
    Func<Task<IPage>> pageSource, 
    AvitoBypassFactory bypassFactory) : IExtractCataloguePagesItemCommand
{
    public async Task<AvitoSpare[]> Extract(AvitoCataloguePage page)
    {
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
        
        string url = page.Url;
        IPage pageInstance = await pageSource();
        await pageInstance.PerformQuickNavigation(url, timeout: 1500);
        if (!await bypassFactory.Create(pageInstance).Bypass())
            throw new InvalidOperationException("Bypass failed.");
        await pageInstance.ScrollBottom();
        await new HoverCatalogueItemImagesCommand(() => Task.FromResult(pageInstance)).Hover();
        JsonData[] data = await pageInstance.EvaluateFunctionAsync<JsonData[]>(javaScript);
        return [..data.Where(d => d.AllPropertiesSet()).Select(d => d.ToAvitoSpareCatalogueRepresentation())];
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

        public AvitoSpare ToAvitoSpareCatalogueRepresentation()
        {
            var representation = new AvitoSpareCatalogueRepresentation(
                Url!, long.Parse(Price!), IsNds, Address!, Photos!, Oem!);
            return AvitoSpare.CatalogueRepresented(Id!, representation);
        }
        
        public bool AllPropertiesSet()
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