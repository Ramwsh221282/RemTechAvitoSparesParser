using AvitoSparesParser.CatalogueParsing;
using AvitoSparesParser.Constants;

using Microsoft.Extensions.DependencyInjection;

using RemTech.SharedKernel.Infrastructure.NpgSql;

using Tests.StartParserTests;

namespace Tests.ParsingStagesTests;

public sealed class PaginationStageTest(ParsingStagesTestsFixture fixture) : IClassFixture<ParsingStagesTestsFixture>
{
    private readonly IServiceProvider _sp = fixture.Services;

    [Fact]
    private async Task Invoke()
    {
        Guid id = Guid.NewGuid();
        Guid linkId = Guid.NewGuid();
        string parser_domain = ServiceConstants.ServiceDomain;
        string parser_type = ServiceConstants.ServiceType;
        string url = "https://www.avito.ru/all/zapchasti_i_aksessuary/zapchasti/dlya_gruzovikov_i_spetstehniki-ASgBAgICAkQKJKwJjGQ?cd=1&q=ponsse";
        IEnumerable<object> links = [new { id = linkId, parser_id = id, url }];

        object message = new
        {
            id,
            parser_domain,
            parser_type,
            links
        };

        await PublishStartParserMessage(message);
        await Task.Delay(TimeSpan.FromSeconds(60));
        bool hasCataloguePages = await EnsureHasCataloguePages();
        Assert.True(hasCataloguePages);
    }

    private async Task PublishStartParserMessage(object message)
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        StartParserFakePublisher publisher = scope.ServiceProvider.GetRequiredService<StartParserFakePublisher>();
        await publisher.Publish(message);
    }

    private async Task<bool> EnsureHasCataloguePages()
    {
        AvitoCataloguePageQuery query = new(UnprocessedOnly: true);
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        await using NpgSqlSession session = scope.ServiceProvider.GetRequiredService<NpgSqlSession>();
        AvitoCataloguePage[] pages = await IEnumerable<AvitoCataloguePage>.GetMany(session, query);
        return pages.Length > 0;
    }
}
