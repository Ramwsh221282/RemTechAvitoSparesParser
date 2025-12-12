using System.Text;
using System.Text.Json;

using AvitoSparesParser.Constants;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace AvitoSparesParser.ParserSubscription;

public sealed class ParserSubscriptionProcess(
    Serilog.ILogger logger,
    NpgSqlConnectionFactory npgSql,
    RabbitMqConnectionSource rabbitMq,
    IHostApplicationLifetime lifetime)
{
    private readonly Serilog.ILogger _logger = logger.ForContext<ParserSubscriptionProcess>();
    private readonly CancellationToken _ct = lifetime.ApplicationStopped;
    private readonly TaskCompletionSource _tcs = new();
    private IChannel _responseQueueChannel = null!;
    private AsyncEventingBasicConsumer _responseQueueConsumer = null!;

    public async Task Subscribe()
    {
        await DeclareResponseListenerQueue();
        await PublishSubscriptionRequest();
        await WaitForConfirmation();
        await DestroyResponseQueueConsumer();
    }

    private async Task WaitForConfirmation()
    {
        await _tcs.Task;
        _logger.Information("Subscription process finished.");
    }

    private async Task PublishSubscriptionRequest()
    {
        _logger.Information("Publishing subscription request.");

        Guid id = Guid.NewGuid();
        string parser_domain = ServiceConstants.ServiceDomain;
        string parser_type = ServiceConstants.ServiceType;

        object body = new
        {
            id,
            parser_domain,
            parser_type
        };

        byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body));
        IConnection connection = await rabbitMq.GetConnection(_ct);

        CreateChannelOptions options = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
        );

        IChannel channel = await connection.CreateChannelAsync(options: options, cancellationToken: _ct);

        await channel.QueueDeclareAsync(
            queue: ServiceConstants.CreateParsersQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: _ct
        );

        await channel.ExchangeDeclareAsync(
            exchange: ServiceConstants.CreateParsersExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: _ct
        );

        await channel.QueueBindAsync(
            queue: ServiceConstants.CreateParsersQueue,
            exchange: ServiceConstants.CreateParsersExchange,
            routingKey: ServiceConstants.CreateParsersQueue,
            cancellationToken: _ct
        );

        BasicProperties publishProperties = new() { Persistent = true };

        await channel.BasicPublishAsync(
            exchange: ServiceConstants.CreateParsersExchange,
            routingKey: ServiceConstants.CreateParsersQueue,
            mandatory: true,
            basicProperties: publishProperties,
            body: payload,
            cancellationToken: _ct
        );

        _logger.Information("Published subscription request.");
    }

    private async Task DeclareResponseListenerQueue()
    {
        _logger.Information("Creating subscription response queue");

        IConnection connection = await rabbitMq.GetConnection(_ct);

        CreateChannelOptions options = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
        );

        _responseQueueChannel = await connection.CreateChannelAsync(options: options, cancellationToken: _ct);

        await _responseQueueChannel.QueueDeclareAsync(
            queue: ServiceConstants.CurrentServiceConfirmationQueue,
            cancellationToken: _ct
        );

        await _responseQueueChannel.ExchangeDeclareAsync(
            exchange: ServiceConstants.CurrentServiceExchange,
            type: ExchangeType.Topic,
            cancellationToken: _ct
        );

        await _responseQueueChannel.QueueBindAsync(
            queue: ServiceConstants.CurrentServiceConfirmationQueue,
            exchange: ServiceConstants.CurrentServiceExchange,
            routingKey: ServiceConstants.CurrentServiceConfirmationQueue,
            cancellationToken: _ct
        );

        _responseQueueConsumer = new(_responseQueueChannel);
        _responseQueueConsumer.ReceivedAsync += Handler;

        await _responseQueueChannel.BasicConsumeAsync(
            queue: ServiceConstants.CurrentServiceConfirmationQueue,
            autoAck: true,
            consumer: _responseQueueConsumer,
            cancellationToken: _ct
        );

        _logger.Information("Subscription response queue has been created");
    }

    private AsyncEventHandler<BasicDeliverEventArgs> Handler => async (sender, ea) =>
    {
        _logger.Information("Subscription response queue received message.");
        try
        {
            await using NpgSqlSession session = new(npgSql);
            await session.UseTransaction(_ct);
            if (await SubscriptionRecord.Persisted(session, _ct))
            {
                _logger.Error("Parser has already subscribed. Aborting subscription saving.");
                _tcs.SetResult();
                return;
            }

            SubscriptionRecord record = SubscriptionRecord.FromDeliverEventArgs(ea);
            await record.Persist(session, _ct);
            await session.UnsafeCommit(_ct);

            _logger.Information("Subscription has been saved.");
            _tcs.SetResult();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error at confirming subscription message in reply queue.");
            throw;
        }
    };

    private async Task DestroyResponseQueueConsumer()
    {
        await _responseQueueChannel.QueuePurgeAsync(
            queue: ServiceConstants.CurrentServiceConfirmationQueue,
            cancellationToken: _ct
        );

        await _responseQueueChannel.QueueDeleteAsync(
            queue: ServiceConstants.CurrentServiceConfirmationQueue,
            cancellationToken: _ct
        );

        await _responseQueueChannel.ExchangeDeleteAsync(
            exchange: ServiceConstants.CurrentServiceExchange,
            cancellationToken: _ct
        );

        _responseQueueConsumer.ReceivedAsync -= Handler;
        await _responseQueueChannel.DisposeAsync();
    }
}