using PuppeteerSharp;

namespace AvitoSparesParser.Common;

public static class BrowserPageExtensions
{
    extension(IPage page)
    {
        public async Task QuickNavigate(string url)
        {
            NavigationOptions options = new() { Timeout = 500 };
            try
            {

                await page.GoToAsync(url, options);
            }
            catch
            {

            }

        }
    }
}
