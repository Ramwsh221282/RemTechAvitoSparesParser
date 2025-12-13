using AvitoFirewallBypass;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

using ParsingSDK;
using ParsingSDK.TextProcessing;

using RemTech.SharedKernel.Infrastructure;

namespace Tests.ParsingTests;

public sealed class SparesParsingFixture : WebApplicationFactory<AvitoSparesParser.Program>, IAsyncLifetime
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        
        builder.ConfigureServices(s =>
        {
            s.RegisterParserDependencies();
            s.RegisterAvitoFirewallBypass();
            s.RegisterSharedInfrastructure();
            s.RegisterTextTransformerBuilder();
        });
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