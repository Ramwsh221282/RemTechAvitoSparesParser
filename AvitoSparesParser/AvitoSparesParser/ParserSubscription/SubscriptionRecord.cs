using System.Text;
using System.Text.Json;

using Dapper;

using RabbitMQ.Client.Events;

using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace AvitoSparesParser.ParserSubscription;

public sealed record SubscriptionRecord(Guid Id, DateTime Created);

public static class SubscriptionRecordCreation
{
    extension(SubscriptionRecord record)
    {
        public static SubscriptionRecord FromDeliverEventArgs(BasicDeliverEventArgs ea)
        {
            byte[] body = ea.Body.ToArray();
            string json = Encoding.UTF8.GetString(body);
            using JsonDocument document = JsonDocument.Parse(json);
            Guid id = document.RootElement.GetProperty("id").GetGuid();
            return new SubscriptionRecord(id, DateTime.UtcNow);
        }
    }
}

// CREATE TABLE IF NOT EXISTS avito_spares_parser.subscriptions
// (
//     id uuid primary key,
//     created timestamptz not null
// );
public static class SubscriptionRecordStoringImplementation
{
    extension(SubscriptionRecord)
    {
        public static async Task<bool> Persisted(NpgSqlSession session, CancellationToken ct = default)
        {
            const string sql = "SELECT EXISTS (SELECT 1 FROM avito_spares_parser.subscriptions)";
            CommandDefinition command = new(sql, cancellationToken: ct, transaction: session.Transaction);
            return await session.QuerySingleRow<bool>(command);
        }
    }

    extension(SubscriptionRecord record)
    {
        public async Task Persist(NpgSqlSession session, CancellationToken ct = default)
        {
            const string sql = "INSERT INTO avito_spares_parser.subscriptions (id, created) VALUES (@id, @created)";
            CommandDefinition command = session.FormCommand(sql, record.ExtractParameters(), ct: ct);
            await session.Execute(command);
        }

        private object ExtractParameters() => new
        {
            id = record.Id,
            created = record.Created
        };
    }
}
