using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace Tests.StartParserTests;

public sealed record FakeParser(Guid Id, string Domain, string Type, FakeParserLink[] Links);

public sealed record FakeParserLink(Guid Id, string Url);

public sealed class FakeStartParserPublisher(RabbitMqConnectionSource connectionSource, Serilog.ILogger logger)
{
    private Serilog.ILogger Logger { get; } = logger.ForContext<FakeStartParserPublisher>();

    public async Task PublishStartParser(FakeParser parser)
    {
        object payload = new
        {
            parser_id = parser.Id,
            parser_domain = parser.Domain,
            parser_type = parser.Type,
            links = parser.Links.Select(link => new { id = link.Id, url = link.Url }).ToArray()
        };
        
        string json = JsonSerializer.Serialize(payload);
        byte[] body = Encoding.UTF8.GetBytes(json);

        CancellationToken ct = CancellationToken.None;
        IConnection connection = await connectionSource.GetConnection(ct);

        CreateChannelOptions options = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
            );
        
        await using IChannel channel = await connection.CreateChannelAsync(options, ct);

        await channel.ExchangeDeclareAsync(
            exchange: $"{parser.Domain}.{parser.Type}",
            type: "topic",
            durable: true,
            autoDelete: false
            );        
        
        await channel.QueueDeclareAsync(
            queue: $"{parser.Domain}.{parser.Type}.start",
            durable: true,
            exclusive: false,
            autoDelete: false
            );
        
        await channel.QueueBindAsync(
            queue: $"{parser.Domain}.{parser.Type}.start",
            exchange: $"{parser.Domain}.{parser.Type}",
            routingKey: $"{parser.Domain}.{parser.Type}.start"
            );

        BasicProperties properties = new() { Persistent = true };
        
        await channel.BasicPublishAsync(
            exchange: $"{parser.Domain}.{parser.Type}",
            routingKey: $"{parser.Domain}.{parser.Type}.start",
            basicProperties: properties,
            mandatory: true,
            body: body
            );
        
        Logger.Information("Published start parser for {Domain}.{Type}", parser.Domain, parser.Type);
    }
}