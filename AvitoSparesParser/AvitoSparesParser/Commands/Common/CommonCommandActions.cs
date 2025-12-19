using PuppeteerSharp;

namespace AvitoSparesParser.Commands.Common;

public static class CommonCommandActions
{
    extension(IPage page)
    {
        public async Task BypassFirewallIfPresent(AvitoBypassFactory bypassFactory)
        {
            if (!await bypassFactory.Create(page).Bypass()) 
                throw new InvalidOperationException("Bypass failed.");
        }
    }

    extension<T>(Task<T> current)
    {
        public async Task<T> Then(Func<T, Task<T>> action)
        {
            T result = await current;
            return await action(result);
        }

        public async Task<U> Reduce<U>(Func<T, Task<U>> action)
        {
            T result = await current;
            return await action(result);
        }
        
        public async Task<T> Then(Func<T, Task> action)
        {
            T result = await current;
            await action(result);
            return result;
        }
    }
}