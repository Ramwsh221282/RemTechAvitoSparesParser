using AvitoSparesParser.Database;
using AvitoSparesParser.ParserStartConfiguration;
using AvitoSparesParser.ParserSubscription;
using AvitoSparesParser.ParsingStages;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// builder.Services.RegisterParserDependencies(conf =>
// {
//         conf.AddSingleton<IOptions<ScrapingBrowserOptions>>(_ => Options.Create(new ScrapingBrowserOptions
//         {
//             Headless = false,
//         }));
// });

builder.Services.RegisterAvitoFirewallBypass();
builder.Services.RegisterSharedInfrastructure();
builder.Services.RegisterDatabaseUpgrader();
builder.Services.RegisterParserSubscriptionProcess();
builder.Services.RegisterParserStartQueue();
builder.Services.RegisterParserWorkStages();
builder.Services.RegisterTextTransformerBuilder();
builder.Services.AddQuartzServices();

WebApplication app = builder.Build();
app.Services.ApplyDatabaseMigrations();
app.Run();

namespace AvitoSparesParser
{
    public partial class Program
    {

    }
}