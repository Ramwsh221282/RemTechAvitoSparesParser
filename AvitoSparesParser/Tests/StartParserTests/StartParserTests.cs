using AvitoSparesParser.Constants;
using AvitoSparesParser.ParserProcessStarting;
using AvitoSparesParser.ParserProcessStarting.Extensions;
using AvitoSparesParser.ParsingStages;
using AvitoSparesParser.ParsingStages.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace Tests.StartParserTests;

public sealed class StartParserTests(StartParserTestsFixture fixture) : IClassFixture<StartParserTestsFixture>
{
    private readonly IServiceProvider _sp = fixture.Services;

    [Fact]
    private async Task Test_Start_Parser_Success()
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
        await Task.Delay(TimeSpan.FromSeconds(10));
        bool hasLinks = await EnsureLinksCreated();
        bool hasPagination = await EnsureStageIsPagination();
        Assert.True(hasLinks);
        Assert.True(hasPagination);
    }

    private async Task PublishStartParserMessage(object message)
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        StartParserFakePublisher publisher = scope.ServiceProvider.GetRequiredService<StartParserFakePublisher>();
        await publisher.Publish(message);
    }

    private async Task<bool> EnsureStageIsPagination()
    {
        ParsingStageQuery query = new(Name: ParsingStageConstants.PAGINATION);
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        await using NpgSqlSession session = scope.ServiceProvider.GetRequiredService<NpgSqlSession>();
        Maybe<ParsingStage> stage = await ParsingStage.GetStage(session, query);
        return stage.HasValue;
    }

    private async Task<bool> EnsureLinksCreated()
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        await using NpgSqlSession session = scope.ServiceProvider.GetRequiredService<NpgSqlSession>();
        ProcessingParserLinkQuery query = new(OnlyNotFetched: true);
        ProcessingParserLink[] links = await IEnumerable<ProcessingParserLink>.QueryMany(session, query);
        return links.Length > 0;
    }
}
