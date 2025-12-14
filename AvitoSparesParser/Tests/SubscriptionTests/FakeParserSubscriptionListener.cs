using System.Text;
using System.Text.Json;
using AvitoSparesParser.Constants;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace Tests.SubscriptionTests;

public sealed class FakeParserSubscriptionListener(
    RabbitMqConnectionSource rabbitMq,
    FakeParserSubscriptionConfirmedPublisher publisher,
    Serilog.ILogger logger) : BackgroundService
{
    private readonly Serilog.ILogger _logger = logger.ForContext<FakeParserSubscriptionListener>();
    private IChannel _channel = null!;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IConnection connection = await rabbitMq.GetConnection(stoppingToken);

        CreateChannelOptions options = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
        );

        _channel = await connection.CreateChannelAsync(options: options, cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: ServiceConstants.CreateParsersQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        await _channel.ExchangeDeclareAsync(
            exchange: ServiceConstants.CreateParsersExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        await _channel.QueueBindAsync(
            queue: ServiceConstants.CreateParsersQueue,
            exchange: ServiceConstants.CreateParsersExchange,
            routingKey: ServiceConstants.CreateParsersRoutingKey,
            cancellationToken: stoppingToken
        );

        AsyncEventingBasicConsumer consumer = new(_channel);
        consumer.ReceivedAsync += Handler;
        await _channel.BasicConsumeAsync(
            queue: ServiceConstants.CreateParsersQueue,
            autoAck: true,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    private AsyncEventHandler<BasicDeliverEventArgs> Handler => async (sender, ea) =>
    {
        _logger.Information("Received message for subscribtion");
        string json = Encoding.UTF8.GetString(ea.Body.ToArray());
        using JsonDocument document = JsonDocument.Parse(json);
        Guid id = document.RootElement.GetProperty("id").GetGuid();
        string parser_domain = document.RootElement.GetProperty("parser_domain").GetString()!;
        string parser_type = document.RootElement.GetProperty("parser_type").GetString()!;
        dynamic message = new
        {
            id,
            parser_domain,
            parser_type
        };
        await publisher.Publish(message);
    };
}
