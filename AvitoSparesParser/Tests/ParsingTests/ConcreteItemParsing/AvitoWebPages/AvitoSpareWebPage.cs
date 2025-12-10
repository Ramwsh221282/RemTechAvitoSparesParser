using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace Tests.ParsingTests.ConcreteItemParsing.AvitoWebPages;

public sealed class AvitoSpareWebPage(IPage page) : IDisposable, IAsyncDisposable
{
    public void Dispose() => page.Dispose();
    public async ValueTask DisposeAsync() => await page.DisposeAsync();
    public async Task<Maybe<IElementHandle>> TryGetElement(string path, int retryCount = 5) => await page.GetElementRetriable(path, retryCount);
    public async Task<IElementHandle[]> GetElements(string path, int retryCount = 5) => await page.GetElementsRetriable(path, retryCount);
}