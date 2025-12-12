using System.Data;

using Dapper;

using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace AvitoSparesParser.ProcessingParsers;

public sealed record ProcessingParser(
    Guid Id,
    string Domain,
    string Type,
    DateTime Entered,
    DateTime? Finished
);

public sealed record ProcessingParserLink(
    Guid Id,
    Guid ParserId,
    string Url,
    bool CatalogueFetched,
    int RetryCount
);

public sealed record ProcessingParserLinkQuery(
    bool OnlyFetched = false,
    bool OnlyNotFetched = false,
    int? RetryCountThreshold = null,
    bool WithLock = false
);

public static class ProcessingParserStoringExtensions
{
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

            if (query.OnlyFetched) filters.Add("catalogue_fetched is true");
            if (query.OnlyNotFetched) filters.Add("catalogue_fetched is false");

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
            parser_id as parser_id,
            url as url,
            catalogue_fetched as catalogue_fetched,
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
                Guid parser_id = reader.GetGuid(reader.GetOrdinal("parser_id"));
                string url = reader.GetString(reader.GetOrdinal("url"));
                bool catalogue_fetched = reader.GetBoolean(reader.GetOrdinal("catalogue_fetched"));
                int retry_count = reader.GetInt32(reader.GetOrdinal("retry_count"));
                links.Add(new ProcessingParserLink(id, parser_id, url, catalogue_fetched, retry_count));
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
                catalogue_fetched = @catalogue_fetched,
                retry_count = @retry_count
            WHERE id = @id
            """;
            IEnumerable<object> parameters = links.Select(link => link.ExtractParameters());
            CommandDefinition command = new(sql, parameters, transaction: session.Transaction);
            await session.Execute(command);
        }

        public async Task AddMany(NpgSqlSession session)
        {
            const string sql = """
            INSERT INTO avito_spares_parser.processing_parser_links
            (id, parser_id, url, catalogue_fetched, retry_count)
            VALUES
            (@id, @parser_id, @url, @catalogue_fetched, @retry_count)
            """;
            IEnumerable<object> parameters = links.Select(link => link.ExtractParameters());
            CommandDefinition command = new(sql, parameters, transaction: session.Transaction);
            await session.Execute(command);
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

        public async Task Update(NpgSqlSession session, CancellationToken ct = default)
        {
            const string sql = "UPDATE SET finished = @finished WHERE id = @id";
            CommandDefinition command = session.FormCommand(sql, parser.ExtractParameters(), ct: ct);
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
            parser_id = link.ParserId,
            url = link.Url,
            catalogue_fetched = link.CatalogueFetched,
            retry_count = link.RetryCount,
        };
    }
}
