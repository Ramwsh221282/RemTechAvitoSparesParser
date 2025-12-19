using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ParsingSDK;
using ParsingSDK.ParserInvokingContext;
using RemTech.SharedKernel.Infrastructure;
using RemTech.Tests.Shared;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Tests.StartParserTests;

namespace Tests;

public sealed class IntegrationalTestsFixture : WebApplicationFactory<AvitoSparesParser.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder().BuildPgVectorContainer();
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder().BuildRabbitMqContainer();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.ReconfigureConfigurationProvider();
            services.ReconfigurePostgreSqlOptions(_dbContainer);
            services.ReconfigureRabbitMqOptions(_rabbitMq);
            services.ReconfigureQuartzHostedService();
            services.RegisterParserStartOptionsByAppsettings();
            services.AddTransient<FakeStartParserPublisher>();
            ReconfigureBrowserOptions(services);
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _rabbitMq.StartAsync();
        Services.ApplyDatabaseMigrations();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
        await _rabbitMq.StopAsync();
        await _rabbitMq.DisposeAsync();
    }

    private static void ReconfigureBrowserOptions(IServiceCollection services)
    {
        services.RemoveAll<IOptions<ScrapingBrowserOptions>>();
        services.RegisterParserDependencies(optionsConfiguration: conf =>
        {
            conf.Headless = false;
            conf.DevelopmentMode = true;
        });
    }
}
