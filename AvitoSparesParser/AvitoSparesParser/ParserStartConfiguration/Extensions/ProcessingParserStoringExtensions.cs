using System.Data;
using Dapper;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace AvitoSparesParser.ParserStartConfiguration.Extensions;

public static class ProcessingParserStoringExtensions
{
    extension(ProcessingParser)
    {
        public static async Task<bool> Exists(NpgSqlSession session)
        {
            const string sql = "SELECT EXISTS (SELECT 1 FROM avito_spares_parser.processing_parsers)";
            CommandDefinition command = new(sql, transaction: session.Transaction);
            bool exists = await session.QuerySingleRow<bool>(command);
            return exists;
        }
    }
    
    extension(ProcessingParserLinkQuery query)
    {
        private (DynamicParameters parameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();

            if (query.RetryCountThreshold.HasValue)
            {
                filters.Add("retry_count <= @retryThreshold");
                parameters.Add("@retryThreshold", query.RetryCountThreshold.Value, DbType.Int32);
            }

            if (query.OnlyFetched) filters.Add("processed is true");
            if (query.OnlyNotFetched) filters.Add("processed is false");

            return filters.Count == 0 ? (parameters, string.Empty) : (parameters, "WHERE " + string.Join(" AND ", filters));
        }

        private string LockClause() => query.WithLock ? "FOR UPDATE" : string.Empty;
    }

    extension(IEnumerable<ProcessingParserLink>)
    {
        public static async Task<ProcessingParserLink[]> QueryMany(
            NpgSqlSession session,
            ProcessingParserLinkQuery query,
            CancellationToken ct = default
        )
        {
            (DynamicParameters parameters, string filterSql) = WhereClause(query);
            string lockClause = query.LockClause();
            string sql = $"""
            SELECT
            id as id,
            url as url,
            processed as processed,
            retry_count as retry_count
            FROM avito_spares_parser.processing_parser_links
            {filterSql}
            {lockClause}
            """;
            CommandDefinition command = session.FormCommand(sql, parameters, ct: ct);
            List<ProcessingParserLink> links = [];
            using IDataReader reader = await session.ExecuteReader(command, ct);
            while (reader.Read())
            {
                Guid id = reader.GetGuid(reader.GetOrdinal("id"));
                string url = reader.GetString(reader.GetOrdinal("url"));
                bool processed = reader.GetBoolean(reader.GetOrdinal("processed"));
                int retry_count = reader.GetInt32(reader.GetOrdinal("retry_count"));
                links.Add(new ProcessingParserLink(
                    id,
                    url,
                    new Common.RetryCounter(retry_count),
                    new Common.ProcessedMarker(processed)
                ));
            }

            return [.. links];
        }
    }

    extension(IEnumerable<ProcessingParserLink> links)
    {
        public async Task UpdateMany(NpgSqlSession session)
        {
            const string sql = """
            UPDATE avito_spares_parser.processing_parser_links
            SET
                processed = @processed,
                retry_count = @retry_count
            WHERE id = @id
            """;
            IEnumerable<object> parameters = links.Select(link => link.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }

        public async Task AddMany(NpgSqlSession session)
        {
            const string sql = """
            INSERT INTO avito_spares_parser.processing_parser_links
            (id, url, processed, retry_count)
            VALUES
            (@id, @url, @processed, @retry_count)
            """;
            IEnumerable<object> parameters = links.Select(link => link.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }
    }

    extension(ProcessingParser parser)
    {
        public async Task Add(NpgSqlSession session, CancellationToken ct = default)
        {
            const string sql = """
            INSERT INTO avito_spares_parser.processing_parsers
            (id, domain, type, finished, entered)
            VALUES
            (@id, @domain, @type, @finished, @entered)
            """;

            CommandDefinition command = new(sql, parser.ExtractParameters(), transaction: session.Transaction, cancellationToken: ct);
            await session.Execute(command);
        }

        private object ExtractParameters() => new
        {
            id = parser.Id,
            domain = parser.Domain,
            type = parser.Type,
            finished = parser.Finished,
            entered = parser.Entered,
        };
    }

    extension(ProcessingParserLink link)
    {
        private object ExtractParameters() => new
        {
            id = link.Id,
            url = link.Url,
            processed = link.Marker.Processed,
            retry_count = link.Counter.Value,
        };
    }
}
