using System.Data;
using Dapper;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace AvitoSparesParser.ConcreteItemParsing.AvitoSpares.Extensions;

public static class AvitoSpareStoring
{
    extension(AvitoSpare)
    {
        public static async Task<AvitoSpare[]> GetMany(NpgSqlSession session, AvitoSpareQuery query, CancellationToken ct = default)
        {
            (DynamicParameters parameters, string filterSql) = query.WhereClause();
            string limitClause = query.LimitClause();
            string lockClause = query.LockClause();
            string sql = $"""
                          SELECT
                          id as id,
                          url as url,
                          payload as payload,
                          processed as processed
                          FROM avito_spares_parser.spares
                          {filterSql}
                          {lockClause}
                          {limitClause}
                          """;
            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            using IDataReader reader = await session.ExecuteReader(command, ct);
            List<AvitoSpare> spares = [];
            while (reader.Read())
            {
                string id = reader.GetString(reader.GetOrdinal("id"));
                string url = reader.GetString(reader.GetOrdinal("url"));
                string payload = reader.GetString(reader.GetOrdinal("payload"));
                bool processed = reader.GetBoolean(reader.GetOrdinal("processed"));
                spares.Add(AvitoSpare.WithJsonPayload(id, url, processed, payload));
            }
            return spares.ToArray(); 
        }
    }
    
    extension(IEnumerable<AvitoSpare> spares)
    {
        public async Task PersistMany(NpgSqlSession session)
        {
            const string sql = """
                               INSERT INTO avito_spares_parser.spares
                               (id, url, payload, processed)
                               VALUES
                               (@id, @url, @payload::jsonb, @processed)
                               ON CONFLICT (id) DO NOTHING
                               """;
            IEnumerable<object> parameters = spares.Select(ExtractParameters);
            await session.ExecuteBulk(sql, parameters);
        }
    }
    
    extension(AvitoSpareQuery query)
    {
        private (DynamicParameters parameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();

            if (query.ProcessedOnly) filters.Add("processed is TRUE");
            if (query.UnprocessedOnly) filters.Add("processed is FALES");
            
            return filters.Count == 0 ? (parameters, string.Empty) : (parameters, "WHERE " + string.Join(" AND ", filters));
        }
        
        private string LimitClause() => query.Limit.HasValue ? $"LIMIT {query.Limit.Value}" : string.Empty;
        private string LockClause() => query.WithLock ? "FOR UPDATE" : string.Empty;
    }
    
    extension(AvitoSpare spare)
    {
        private object ExtractParameters() => new
        {
            id = spare.Id,
            url = spare.Url,
            payload = spare.Payload,
            processed = spare.Marker.Processed
        };
    }
}