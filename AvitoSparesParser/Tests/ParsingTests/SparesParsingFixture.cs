using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace Tests.ParsingTests;

public sealed class SparesParsingFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
    }

    public async Task InitializeAsync()
    {
        await Task.Yield();
    }

    public new async Task DisposeAsync()
    {
        await Task.Yield();
    }
}