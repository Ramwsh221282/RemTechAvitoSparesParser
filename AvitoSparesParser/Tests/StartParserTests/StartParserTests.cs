using Microsoft.Extensions.DependencyInjection;

namespace Tests.StartParserTests;

public sealed class StartParserTests(IntegrationalTestsFixture fixture) : IClassFixture<IntegrationalTestsFixture>
{
    private IServiceProvider Services { get; } = fixture.Services;

    [Fact]
    private async Task Invoke_Parser_Start()
    {
        FakeParser parser = new(Guid.NewGuid(), "Avito", "Запчасти", [ new FakeParserLink(Guid.NewGuid(), "https://www.avito.ru/all/zapchasti_i_aksessuary/zapchasti/dlya_gruzovikov_i_spetstehniki/texnika_dlia_lesozagotovki-ASgBAgICA0QKJKwJjGT46w7G1oED?cd=1&f=ASgBAgICBEQKJKwJjGSexw346j_46w7G1oED")]);
        await PublishStartParser(parser);
        await Task.Delay(TimeSpan.FromMinutes(30));
    }
    
    private async Task PublishStartParser(FakeParser parser)
    {
        FakeStartParserPublisher publisher = Services.GetRequiredService<FakeStartParserPublisher>();
        await publisher.PublishStartParser(parser);
    }
}