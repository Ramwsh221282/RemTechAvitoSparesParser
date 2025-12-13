using System.Text;
using System.Text.Json;

using AvitoSparesParser.Constants;

using RabbitMQ.Client;

using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace Tests.StartParserTests;

public sealed class StartParserFakePublisher(RabbitMqConnectionSource rabbitMq)
{
    public async Task Publish(dynamic message, CancellationToken ct = default)
    {
        string queue = message.parser_domain + '.' + message.parser_type + '.' + "start";        

        IConnection connection = await rabbitMq.GetConnection(ct);

        CreateChannelOptions options = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
        );

        await using IChannel channel = await connection.CreateChannelAsync(options: options, cancellationToken: ct);

        await channel.ExchangeDeclareAsync(
            exchange: ServiceConstants.StartParserExchange,
            type: "topic",
            durable: true,
            autoDelete: false,
            cancellationToken: ct
        );

        await channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct
        );

        await channel.QueueBindAsync(
            exchange: ServiceConstants.StartParserExchange,
            queue: queue,
            routingKey: queue,
            cancellationToken: ct
        );

        BasicProperties publishProperties = new() { Persistent = true };
        ReadOnlyMemory<byte> body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await channel.BasicPublishAsync(
            exchange: ServiceConstants.StartParserExchange,
            routingKey: queue,
            mandatory: true,
            basicProperties: publishProperties,
            body: body,
            cancellationToken: ct
        );
    }
}