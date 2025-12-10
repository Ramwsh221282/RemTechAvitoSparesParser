using AvitoFirewallBypass;
using ParsingSDK;
using RemTech.SharedKernel.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterParserDependencies();
builder.Services.RegisterAvitoFirewallBypass();
builder.Services.RegisterSharedInfrastructure();

WebApplication app = builder.Build();

app.Run();

namespace AvitoSparesParser
{
    public partial class Program
    {
        
    }
}