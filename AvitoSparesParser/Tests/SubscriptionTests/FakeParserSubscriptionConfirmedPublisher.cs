using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace Tests.SubscriptionTests;

public sealed class FakeParserSubscriptionConfirmedPublisher(Serilog.ILogger logger, RabbitMqConnectionSource rabbitMq)
{
    private readonly Serilog.ILogger _logger = logger.ForContext<FakeParserSubscriptionConfirmedPublisher>();
    public async Task Publish(dynamic message, CancellationToken ct = default)
    {
        string exchange = message.parser_domain + '.' + message.parser_type;
        string queue = message.parser_domain + '.' + message.parser_type + ".confirmation";

        IConnection connection = await rabbitMq.GetConnection(ct);

        CreateChannelOptions options = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
        );

        await using IChannel channel = await connection.CreateChannelAsync(options: options, cancellationToken: ct);

        await channel.QueueDeclareAsync(
            queue: queue,
            cancellationToken: ct
        );

        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: "topic",
            cancellationToken: ct
        );

        await channel.QueueBindAsync(
            queue: queue,
            exchange: exchange,
            routingKey: queue,
            cancellationToken: ct
        );

        ReadOnlyMemory<byte> messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        BasicProperties properties = new() { Persistent = true };

        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: queue,
            mandatory: true,
            body: messageBytes,
            cancellationToken: ct
        );

        _logger.Information("Published response for reply queue");
    }
}
