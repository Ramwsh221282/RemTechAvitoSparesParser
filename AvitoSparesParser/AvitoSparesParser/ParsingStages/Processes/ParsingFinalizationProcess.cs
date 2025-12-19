using AvitoSparesParser.AvitoSpareContext;
using AvitoSparesParser.AvitoSpareContext.Extensions;
using AvitoSparesParser.ParsingStages.Extensions;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace AvitoSparesParser.ParsingStages.Processes;

public static class ParsingFinalizationProcess
{
    extension(ParserStageProcess)
    {
        public static ParserStageProcess Finalization => async (deps, ct) =>
        {
            deps.Deconstruct(out NpgSqlConnectionFactory npgSql, out Serilog.ILogger logger, out _, out _, out _);
            await using NpgSqlSession session = new(npgSql);
            
            Maybe<ParsingStage> finalization = await GetFinalizationStage(session, ct);
            if (!finalization.HasValue) return;
            
            AvitoSpare[] spares = await GetCompletedAvitoSpares(session, ct);
            if (CanSwitchNextStage(spares))
            {
                await SwitchNextStage(finalization.Value, session, logger, ct);
                await FinishTransaction(session, logger, ct);
                return;
            }
            
            await SendResultsToMainBackend(spares, session, logger);
            await FinishTransaction(session, logger, ct);
        };
    }

    private static async Task FinishTransaction(NpgSqlSession session, Serilog.ILogger logger, CancellationToken ct)
    {
        try
        {
            await session.UnsafeCommit(ct);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to commit transaction");
        }
    }
    
    private static async Task SendResultsToMainBackend(AvitoSpare[] spares, NpgSqlSession session, Serilog.ILogger logger)
    {
        await spares.RemoveMany(session);
        logger.Information("Sent {Count} results to main backend.", spares.Length);
    }
    
    private static async Task<Maybe<ParsingStage>> GetFinalizationStage(NpgSqlSession session, CancellationToken ct)
    {
        ParsingStageQuery query = new(Name: ParsingStageConstants.FINALIZATION_STAGE, WithLock: true);
        Maybe<ParsingStage> stage = await ParsingStage.GetStage(session, query, ct);
        return stage;        
    }

    private static async Task SwitchNextStage(ParsingStage stage, NpgSqlSession session, Serilog.ILogger logger, CancellationToken ct)
    {
        ParsingStage completed = stage.ToEmptyStage();
        await completed.Update(session, ct);
        logger.Information("Switched to {Stage} stage.", completed.Name);
    }
    
    private static bool CanSwitchNextStage(AvitoSpare[] spares)
    {
        return spares.Length == 0;
    }
    
    private static async Task<AvitoSpare[]> GetCompletedAvitoSpares(NpgSqlSession session, CancellationToken ct)
    {
        AvitoSpareQuery query = new(ConcreteOnly: true, Limit: 50, WithLock: true);
        return await AvitoSpare.Query(session, query, ct);
    }
}