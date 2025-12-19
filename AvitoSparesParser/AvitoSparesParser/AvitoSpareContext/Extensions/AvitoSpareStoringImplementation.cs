using System.Data;
using System.Text.Json;
using Dapper;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace AvitoSparesParser.AvitoSpareContext.Extensions;

public static class AvitoSpareStoringImplementation
{
    extension(AvitoSpare)
    {
        public static async Task<AvitoSpare[]> Query(NpgSqlSession session, AvitoSpareQuery query, CancellationToken ct = default)
        {
            (DynamicParameters parameters, string filterSql) = WhereClause(query);
            string lockClause = LockClause(query);
            string limitClause = LimitClause(query);
            string sql = $"""
                          SELECT
                          id as id,
                          url as url,
                          price as price,
                          is_nds as is_nds,
                          address as address,
                          photos as photos,
                          oem as oem,
                          processed as processed,
                          retry_count as retry_count,
                          type as type,
                          title as title
                          FROM avito_spares_parser.spares
                          {filterSql}
                          {lockClause}
                          {limitClause}
                          """;
            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            using IDataReader reader = await session.ExecuteReader(command, ct);
            List<AvitoSpare> spares = [];
            while (reader.Read()) spares.Add(CreateBy(reader));
            return spares.ToArray();
        }

        private static AvitoSpare CreateBy(IDataReader reader)
        {
            string id = reader.GetString(reader.GetOrdinal("id"));
            string url = reader.GetString(reader.GetOrdinal("url"));
            bool isNds = reader.GetBoolean(reader.GetOrdinal("is_nds"));
            long price = reader.GetInt64(reader.GetOrdinal("price"));
            string address = reader.GetString(reader.GetOrdinal("address"));
            string[] photos = JsonSerializer.Deserialize<string[]>(reader.GetString(reader.GetOrdinal("photos")))!;
            string oem = reader.GetString(reader.GetOrdinal("oem"));
            bool processed = reader.GetBoolean(reader.GetOrdinal("processed"));
            int retryCount = reader.GetInt32(reader.GetOrdinal("retry_count"));
            
            string? type = reader.IsDBNull(reader.GetOrdinal("type")) 
                ? null : reader.GetString(reader.GetOrdinal("type"));
            string? title = reader.IsDBNull(reader.GetOrdinal("title")) 
                ? null : reader.GetString(reader.GetOrdinal("title"));

            AvitoSpareCatalogueRepresentation catalogueRepresentation = new(url, price, isNds, address, photos, oem);
            if (type == null || title == null)
                return AvitoSpare.CatalogueRepresented(id, catalogueRepresentation)
                    .Transform(processed, processedExtractor: p => p)
                    .Transform(retryCount, retryCountExtractor: r => r);
            
            AvitoSpareConcreteRepresentation concreteRepresentation = new(type, title);
            return AvitoSpare.Create(id, retryCount, processed, catalogueRepresentation, concreteRepresentation);
        }
    }
    
    extension(IEnumerable<AvitoSpare> spares)
    {
        
        public async Task RemoveMany(NpgSqlSession session)
        {
            const string sql = "DELETE FROM avito_spares_parser.spares WHERE id = ANY(@ids)";
            IEnumerable<object> parameters = spares.Select(s => s.Id);
            await session.ExecuteBulk(sql, parameters);
        }
        
        public async Task PersistAsCatalogueRepresentationMany(NpgSqlSession session)
        {
            const string sql =
                """
                INSERT INTO avito_spares_parser.spares
                (id, url, price, is_nds, address, photos, oem, processed, retry_count)
                VALUES
                (@id, @url, @price, @is_nds, @address, @photos::jsonb, @oem, @processed, @retry_count)
                ON CONFLICT (id) DO NOTHING
                """;
            IEnumerable<object> parameters = spares.Select(spare => spare.ExtractCatalogueRepresentationParameters());
            await session.ExecuteBulk(sql, parameters);
        }

        public async Task PersistAsConcreteRepresentationMany(NpgSqlSession session)
        {
            const string sql =
                """
                INSERT INTO avito_spares_parser.spares
                (id, url, price, is_nds, address, photos, oem, type, title, processed, retry_count)
                VALUES
                (@id, @url, @price, @is_nds, @address, @photos::jsonb, @oem, @type, @title, @processed, @retry_count)
                ON CONFLICT (id) DO UPDATE SET type = @type, title = @title, processed = @processed, retry_count = @retry_count
                """;
            IEnumerable<object> parameters = spares.Select(spare => spare.ExtractConcreteRepresentationParameters());
            await session.ExecuteBulk(sql, parameters);
        }
    }
    
    extension(AvitoSpare spare)
    {
        private object ExtractCatalogueRepresentationParameters() => new
        {
            id = spare.Id,
            url = spare.CatalogueRepresentation.Url,
            price = spare.CatalogueRepresentation.Price,
            retry_count = spare.RetryCount,
            processed = spare.Processed,
            is_nds = spare.CatalogueRepresentation.IsNds,
            address = spare.CatalogueRepresentation.Address,
            photos = JsonSerializer.Serialize(spare.CatalogueRepresentation.Photos),
            oem = spare.CatalogueRepresentation.Oem,
        };

        private object ExtractConcreteRepresentationParameters() => new
        {
            id = spare.Id,
            url = spare.CatalogueRepresentation.Url,
            price = spare.CatalogueRepresentation.Price,
            retry_count = spare.RetryCount,
            processed = spare.Processed,
            is_nds = spare.CatalogueRepresentation.IsNds,
            address = spare.CatalogueRepresentation.Address,
            photos = JsonSerializer.Serialize(spare.CatalogueRepresentation.Photos),
            oem = spare.CatalogueRepresentation.Oem,
            type = spare.ConcreteRepresentation.Type,
            title = spare.ConcreteRepresentation.Title
        };
    }

    extension(AvitoSpareQuery query)
    {
        private (DynamicParameters parameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();

            if (query.ProcessedOnly) filters.Add("processed is TRUE");
            if (query.UnprocessedOnly) filters.Add("processed is FALSE");
            if (query.CatalogueOnly)
            {
                filters.Add("type is null");
                filters.Add("title is null");
            }
            if (query.ConcreteOnly)
            {
                filters.Add("type is not null");
                filters.Add("title is not null");
            }
            if (query.RetryCountThreshold.HasValue)
            {
                filters.Add($"retry_count <= @retryCountThreshold");
                parameters.Add("retryCountThreshold", query.RetryCountThreshold.Value, DbType.Int32);
            }

            return filters.Count == 0 ? (parameters, string.Empty) : (parameters, "WHERE " + string.Join(" AND ", filters));
        }
        
        private string LockClause() => query.WithLock ? "FOR UPDATE" : string.Empty;
        private string LimitClause() => query.Limit.HasValue ? $"LIMIT {query.Limit.Value}" : string.Empty;
    }
}