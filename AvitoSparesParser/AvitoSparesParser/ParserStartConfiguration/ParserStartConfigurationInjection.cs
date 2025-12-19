using AvitoSparesParser.ParserStartConfiguration.Extensions;
using AvitoSparesParser.ParsingStages;
using AvitoSparesParser.ParsingStages.Extensions;
using ParsingSDK.ParserInvokingContext;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.RabbitMq;
using ILogger = Serilog.ILogger;

namespace AvitoSparesParser.ParserStartConfiguration;

public static class ParserStartConfigurationInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterParserStartQueue()
        {
            services.RegisterParserStartQueueHandle();
            services.RegisterParserInvokingQueue(sp =>
                {
                    return async ct =>
                    {
                        RabbitMqConnectionSource source = sp.GetRequiredService<RabbitMqConnectionSource>();
                        return await source.GetConnection(ct);
                    };
                }
            );
        }

        private void RegisterParserStartQueueHandle()
        {
            services.AddSingleton<ParserStartQueueHandle>(sp =>
            {
                NpgSqlConnectionFactory npgSql = sp.GetRequiredService<NpgSqlConnectionFactory>();
                ILogger logger = sp.GetRequiredService<ILogger>();

                return async ea =>
                {
                    await using NpgSqlSession session = new(npgSql);
                    await session.UseTransaction();

                    if (await ProcessingParser.Exists(session))
                    {
                        logger.Information("There is already processing parser in process.");
                        return;
                    }

                    ProcessingParser parser = ProcessingParser.FromDeliverEventArgs(ea);
                    ProcessingParserLink[] links = IEnumerable<ProcessingParserLink>.ArrayFromDeliverEventArgs(ea);
                    ParsingStage stage = ParsingStage.PaginationFromParser(parser);

                    await stage.Save(session);
                    await parser.Add(session);
                    await links.AddMany(session);
                    await session.UnsafeCommit(CancellationToken.None);

                    logger.Information("Parser {Domain} {Type} has been registered with links count: {Count}.",
                        parser.Domain, parser.Type, links.Length);
                };
            });
        }
    }
}