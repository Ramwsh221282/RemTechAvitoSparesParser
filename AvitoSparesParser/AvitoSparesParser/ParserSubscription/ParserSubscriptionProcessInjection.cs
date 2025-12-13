using ParserSubscriber.Subscribers.RabbitMq;

using RemTech.SharedKernel.Infrastructure.NpgSql;

using RabbitMQProvider = RemTech.SharedKernel.Infrastructure.RabbitMq.RabbitMqConnectionSource;

namespace AvitoSparesParser.ParserSubscription;

public static class ParserSubscriptionProcessInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterParserSubscriptionProcess()
        {
            services.RegisterParserSubscriber<RabbitMqRequestReplySubscriber>(
                rabbitMqProviderConfiguration: ConnectionSourceConfiguration,
                messageHandlerConfiguration: MessageHandlerConfiguration,
                optionsConfiguration: OptionsConfiguration
            );
        }
    }

    private static Action<IServiceCollection> ConnectionSourceConfiguration => (serv) =>
        {
            serv.AddSingleton<RabbitMqConnectionSource>(sp =>
                {
                    RabbitMQProvider source = sp.GetRequiredService<RabbitMQProvider>();
                    return async (ct) => await source.GetConnection(ct);
                });
        };

    private static Action<IServiceCollection> MessageHandlerConfiguration => (serv) =>
    {
        serv.AddTransient<RabbitMqRequestReplySubscriberMessageHandler>(sp =>
        {
            NpgSqlConnectionFactory npgSql = sp.GetRequiredService<NpgSqlConnectionFactory>();
            Serilog.ILogger logger = sp.GetRequiredService<Serilog.ILogger>().ForContext<RabbitMqRequestReplySubscriberMessageHandler>();
            return async (ea, channel) =>
            {
                try
                {
                    CancellationToken ct = CancellationToken.None;
                    await using NpgSqlSession session = new(npgSql);
                    await session.UseTransaction(CancellationToken.None);
                    if (await ParserSubscribtion.Persisted(session, ct))
                    {
                        logger.Error("Parser has already subscribed. Aborting subscription saving.");
                        return;
                    }

                    ParserSubscribtion record = ParserSubscribtion.FromDeliverEventArgs(ea);
                    await record.Persist(session, ct);
                    await session.UnsafeCommit(ct);

                    logger.Information("Subscription has been saved.");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error at confirming subscription message in reply queue.");
                    throw;
                }
            };
        });
    };

    private static Action<IServiceCollection> OptionsConfiguration => (serv) =>
        serv.AddOptions<RabbitMqRequestReplyResponseListeningQueueOptions>()
            .BindConfiguration(nameof(RabbitMqRequestReplyResponseListeningQueueOptions));
}
