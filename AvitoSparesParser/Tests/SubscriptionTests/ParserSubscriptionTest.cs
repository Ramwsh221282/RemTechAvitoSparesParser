using AvitoSparesParser.ParserSubscription;

using Microsoft.Extensions.DependencyInjection;
using ParserSubscriber;

using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace Tests.SubscriptionTests;

public sealed class ParserSubscriptionTest(ParserSubscriptionTestFixture fixture) : IClassFixture<ParserSubscriptionTestFixture>
{
    private readonly IServiceProvider _sp = fixture.Services;

    [Fact]
    private async Task Test_Parser_Subscribed()
    {
        IParserSubscriber process = _sp.GetRequiredService<IParserSubscriber>();
        await process.Subscribe();
        bool sessionCreated = await EnsureSubscriptionPersisted();
        Assert.True(sessionCreated);
    }

    private async Task<bool> EnsureSubscriptionPersisted()
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        await using NpgSqlSession session = scope.ServiceProvider.GetRequiredService<NpgSqlSession>();
        return await ParserSubscribtion.Persisted(session);
    }
}
