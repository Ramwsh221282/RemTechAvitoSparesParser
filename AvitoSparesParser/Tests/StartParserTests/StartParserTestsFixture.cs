using AvitoSparesParser.ParserProcessStarting.BackgroundTasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using RemTech.SharedKernel.Infrastructure;
using RemTech.Tests.Shared;

using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Tests.StartParserTests;

public sealed class StartParserTestsFixture : WebApplicationFactory<AvitoSparesParser.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder().BuildPgVectorContainer();
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder().BuildRabbitMqContainer();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _rabbitMq.StartAsync();
        Services.ApplyDatabaseMigrations();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
        await _rabbitMq.StopAsync();
        await _rabbitMq.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(s =>
        {
            s.ReconfigurePostgreSqlOptions(_dbContainer);
            s.ReconfigureRabbitMqOptions(_rabbitMq);
            
            var startParserProcessListenerDescriptor = s.Single(serv => serv.ImplementationType == typeof(StartParserProcessListener));
            s.Remove(startParserProcessListenerDescriptor);

            s.AddHostedService<StartParserProcessListener>();
            s.AddTransient<StartParserFakePublisher>();
        });
    }

}
