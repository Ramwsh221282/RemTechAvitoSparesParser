using AvitoFirewallBypass;

using AvitoSparesParser.Database;
using AvitoSparesParser.ParserProcessStarting.BackgroundTasks;
using AvitoSparesParser.ParserSubscription;
using AvitoSparesParser.ParsingStages;

using ParsingSDK;

using RemTech.SharedKernel.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterParserDependencies();
builder.Services.RegisterAvitoFirewallBypass();
builder.Services.RegisterSharedInfrastructure();
builder.Services.RegisterDatabaseUpgrader();
builder.Services.RegisterParserSubscriptionProcess();
builder.Services.RegisterStartParserListener();
builder.Services.RegisterParserWorkStages();
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