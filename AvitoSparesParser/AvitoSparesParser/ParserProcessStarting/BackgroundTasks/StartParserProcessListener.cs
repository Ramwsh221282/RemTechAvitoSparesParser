using AvitoSparesParser.Constants;
using AvitoSparesParser.ParserProcessStarting.Extensions;
using AvitoSparesParser.ParsingStages;
using AvitoSparesParser.ParsingStages.Extensions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace AvitoSparesParser.ParserProcessStarting.BackgroundTasks;

public sealed class StartParserProcessListener(
    RabbitMqConnectionSource rabbitMq,
    Serilog.ILogger logger,
    NpgSqlConnectionFactory npgSql
) : BackgroundService
{
    private Serilog.ILogger Logger => logger.ForContext<StartParserProcessListener>();
    private IChannel _channel = null!;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IConnection connection = await rabbitMq.GetConnection(stoppingToken);

        CreateChannelOptions options = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
        );

        _channel = await connection.CreateChannelAsync(options, cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: ServiceConstants.CurrentServiceStartQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        await _channel.ExchangeDeclareAsync(
            exchange: ServiceConstants.StartParserExchange,
            type: "topic",
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        await _channel.QueueBindAsync(
            queue: ServiceConstants.CurrentServiceStartQueue,
            exchange: ServiceConstants.StartParserExchange,
            routingKey: ServiceConstants.CurrentServiceStartQueue,
            cancellationToken: stoppingToken
        );

        AsyncEventingBasicConsumer consumer = new(_channel);
        consumer.ReceivedAsync += Handler;
        await _channel.BasicConsumeAsync(
            queue: ServiceConstants.CurrentServiceStartQueue,
            autoAck: true,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    private AsyncEventHandler<BasicDeliverEventArgs> Handler => async (sender, ea) =>
    {
        Logger.Information("Received start parser message from queue.");
        try
        {
            await using NpgSqlSession session = new(npgSql);
            await session.UseTransaction();

            ProcessingParser parser = ProcessingParser.FromDeliverEventArgs(ea);
            ProcessingParserLink[] links = IEnumerable<ProcessingParserLink>.ArrayFromDeliverEventArgs(ea);
            ParsingStage stage = ParsingStage.PaginationFromParser(parser);

            await stage.Save(session);
            await parser.Add(session);
            await links.AddMany(session);
            await session.UnsafeCommit(CancellationToken.None);

            Logger.Information("Parser {Domain} {Type} has been registered with links count: {Count}.",
                parser.Domain, parser.Type, links.Length);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error at processing message from queue.");
        }
    };
}
