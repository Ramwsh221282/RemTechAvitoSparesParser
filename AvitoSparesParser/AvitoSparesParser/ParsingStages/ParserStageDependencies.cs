using AvitoFirewallBypass;

using ParsingSDK.Parsing;

using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace AvitoSparesParser.ParsingStages;

public sealed record ParserStageDependencies(
    NpgSqlConnectionFactory NpgSql, 
    Serilog.ILogger Logger,
    AvitoBypassFactory Bypasses,
    BrowserFactory Browsers
    );
public delegate Task ParserStageProcess(ParserStageDependencies deps, CancellationToken ct);
