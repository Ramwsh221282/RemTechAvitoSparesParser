using System.Data;
using AvitoSparesParser.Common;
using Dapper;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace AvitoSparesParser.CatalogueParsing.Extensions;

public static class AvitoCataloguePageStoring
{
    extension(IEnumerable<AvitoCataloguePage>)
    {
        public static async Task<AvitoCataloguePage[]> GetMany(
            NpgSqlSession session,
            AvitoCataloguePageQuery query,
            CancellationToken ct = default
        )
        {
            (DynamicParameters parameters, string filterSql) = WhereClause(query);
            string lockClause = LockClause(query);
            string sql = $"""
            SELECT
            id as id,
            url as url,
            processed as processed,
            retry_count as retry_count
            FROM avito_spares_parser.catalogue_pages
            {filterSql}
            {lockClause}
            """;
            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            using IDataReader reader = await session.ExecuteReader(command, ct);
            List<AvitoCataloguePage> pages = [];
            while (reader.Read())
            {
                Guid id = reader.GetGuid(reader.GetOrdinal("id"));
                string url = reader.GetString(reader.GetOrdinal("url"));
                bool processed = reader.GetBoolean(reader.GetOrdinal("processed"));
                int retryCount = reader.GetInt32(reader.GetOrdinal("retry_count"));
                pages.Add(new AvitoCataloguePage(id, url, new RetryCounter(retryCount), new ProcessedMarker(processed)));
            }

            return [.. pages];
        }
    }

    extension(IEnumerable<AvitoCataloguePage> pages)
    {
        public async Task AddMany(NpgSqlSession session)
        {
            const string sql = """
            INSERT INTO avito_spares_parser.catalogue_pages 
            (id, url, processed, retry_count)
            VALUES 
            (@id, @url, @processed, @retry_count);
            """;
            IEnumerable<object> parameters = pages.Select(p => p.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }

        public async Task UpdateMany(NpgSqlSession session)
        {
            const string sql =
            """
            UPDATE avito_spares_parser.catalogue_pages
            SET processed = @processed, retry_count = @retry_count
            WHERE id = @id
            """;
            IEnumerable<object> parameters = pages.Select(p => p.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }
    }

    extension(AvitoCataloguePage page)
    {
        private object ExtractParameters() => new
        {
            id = page.Id,
            url = page.Url,
            processed = page.Marker.Processed,
            retry_count = page.Counter.Value
        };
    }

    extension(AvitoCataloguePageQuery query)
    {
        private string LockClause() => query.WithLock ? "FOR UPDATE" : string.Empty;

        private (DynamicParameters parameters, string filters) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();

            if (query.ProcessedOnly) filters.Add("processed is true");
            if (query.UnprocessedOnly) filters.Add("processed is false");
            if (query.RetryThreshold.HasValue)
            {
                filters.Add("retry_count <= @retryThreshold");
                parameters.Add("@retryThreshold", query.RetryThreshold.Value, DbType.Int32);
            }

            return filters.Count == 0 ? (parameters, string.Empty) : (parameters, "WHERE " + string.Join(" AND ", filters));
        }
    }
}