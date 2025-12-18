using Microsoft.Extensions.Options;
using ParserSubscriber.SubscribtionContext;
using ParserSubscriber.SubscribtionContext.Options;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RabbitMQProvider = RemTech.SharedKernel.Infrastructure.RabbitMq.RabbitMqConnectionSource;

namespace AvitoSparesParser.ParserSubscription;

public static class ParserSubscriptionProcessInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterParserSubscriptionProcess()
        {
            services.AddOptions<RabbitMqRequestReplyResponseListeningQueueOptions>()
                .BindConfiguration(nameof(RabbitMqRequestReplyResponseListeningQueueOptions));
            
            services.RegisterParserSubscriber<RabbitMqRequestReplySubscriber>(
                rabbitMqProviderConfiguration: ConnectionSourceConfiguration,
                npgSqlProviderConfiguration: NpgSqlProviderConfiguration,
                schemaName: "avito_spares_parser"
            );
        }
    }

    private static Action<IServiceCollection> ConnectionSourceConfiguration => (serv) =>
        {
            serv.AddSingleton<RabbitMqConnectionProvider>(sp =>
                {
                    RabbitMQProvider source = sp.GetRequiredService<RabbitMQProvider>();
                    return (ct) => source.GetConnection(ct);
                });
        };
    
    private static Action<IServiceCollection> NpgSqlProviderConfiguration => (serv) =>
        serv.AddSingleton<NpgSqlProvider>(sp =>
        {
            IOptions<NpgSqlOptions> options = sp.GetRequiredService<IOptions<NpgSqlOptions>>();
            return new NpgSqlProvider { ConnectionString = options.Value.ToConnectionString() };
        });
}
