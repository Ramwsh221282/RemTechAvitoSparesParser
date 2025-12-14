using System.Data;
using AvitoSparesParser.Common;
using Dapper;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace AvitoSparesParser.CatalogueParsing.Extensions;

public static class AvitoCatalogueSpareStoring
{
    extension(AvitoCatalogueSpare)
    {
        public static async Task<AvitoCatalogueSpare[]> GetMany(NpgSqlSession session, AvitoCatalogueSpareQuery query, CancellationToken ct = default)
        {
            (DynamicParameters parameters, string filterSql) = query.WhereClause();
            string lockClause = query.LockClause();
            string limitClause = query.LimitClause();
            string sql =
            $"""
            SELECT
            id as id,
            url as url,
            photos as photos,
            processed as processed,
            retry_count as retry_count
            FROM avito_spares_parser.catalogue_items
            {filterSql}
            {lockClause}
            {limitClause}
            """;

            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            using IDataReader reader = await session.ExecuteReader(command, ct);
            List<AvitoCatalogueSpare> spares = [];

            while (reader.Read())
            {
                AvitoCatalogueItemMetadata metadata = new(
                    id: reader.GetString(reader.GetOrdinal("id")),
                    url: reader.GetString(reader.GetOrdinal("url"))
                );


                PlainJsonStringArray photos = PlainJsonStringArray.FromJson(
                    json: reader.GetString(reader.GetOrdinal("photos"))
                );

                ProcessedMarker marker = new(
                    processed: reader.GetBoolean(reader.GetOrdinal("processed"))
                );

                RetryCounter counter = new(
                    counter: reader.GetInt32(reader.GetOrdinal("retry_count"))
                );

                spares.Add(new AvitoCatalogueSpare(metadata, photos, counter, marker));
            }

            return [.. spares];
        }
    }

    extension(AvitoCatalogueSpareQuery query)
    {
        private string LimitClause()
        {
            return query.Limit.HasValue ? $"LIMIT {query.Limit.Value}" : string.Empty;
        }

        private (DynamicParameters parameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();

            if (query.UnprocessedOnly) filters.Add("processed is false");
            if (query.RetryLimitThreshold.HasValue)
            {
                filters.Add("retry_count <= @retryThreshold");
                parameters.Add("@retryThreshold", query.RetryLimitThreshold.Value, DbType.Int32);
            }


            return filters.Count == 0 ? (parameters, string.Empty) : (parameters, "WHERE " + string.Join(" AND ", filters));
        }

        private string LockClause()
        {
            return query.WithLock ? "FOR UPDATE" : string.Empty;
        }
    }


    extension(AvitoCatalogueSpare spare)
    {
        private object ExtractParameters() => new
        {
            id = spare.Metadata.Id,
            url = spare.Metadata.Url,
            photos = spare.Photos.Json,
            processed = spare.Marker.Processed,
            retry_count = spare.Counter.Value
        };
    }

    extension(IEnumerable<AvitoCatalogueSpare> spares)
    {
        public async Task PersistMany(NpgSqlSession session)
        {
            const string sql =
            """
            INSERT INTO avito_spares_parser.catalogue_items
            (id, url, photos, processed, retry_count)
            VALUES
            (@id, @url, @photos::jsonb, @processed, @retry_count)
            ON CONFLICT (id) DO NOTHING
            """;
            IEnumerable<object> parameters = spares.Select(p => p.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }

        public async Task UpdateMany(NpgSqlSession session)
        {
            const string sql =
            """
            UPDATE avito_spares_parser.catalogue_items
            SET processed = @processed, retry_count = @retry_count
            WHERE id = @id
            """;
            IEnumerable<object> parameters = spares.Select(p => p.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }
    }
}
