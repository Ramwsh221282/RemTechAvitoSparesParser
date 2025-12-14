using System.Data;
using Dapper;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace AvitoSparesParser.ParsingStages.Extensions;

public static class ParsingStageStoringExtensions
{
    extension(ParsingStage)
    {
        public static async Task<Maybe<ParsingStage>> GetStage(
            NpgSqlSession session,
            ParsingStageQuery query,
            CancellationToken ct = default)
        {
            (DynamicParameters parameters, string filterSql) = query.WhereClause();
            string lockClause = query.LockClause();
            string sql = $"""
            SELECT id, name
            FROM avito_spares_parser.stages
            {filterSql}
            {lockClause}
            """;
            CommandDefinition command = new(sql, parameters, transaction: session.Transaction, cancellationToken: ct);
            using IDataReader reader = await session.ExecuteReader(command, ct);
            return reader.Read() switch
            {
                false => Maybe<ParsingStage>.None(),
                true => Maybe<ParsingStage>.Some(new ParsingStage(reader.GetGuid(0), reader.GetString(1))),
            };
        }
    }

    extension(ParsingStageQuery query)
    {
        private (DynamicParameters parameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();
            if (query.Id.HasValue)
            {
                filters.Add($"id = @id");
                parameters.Add("id", query.Id.Value);
            }

            if (query.Name is not null)
            {
                filters.Add($"name = @name");
                parameters.Add("name", query.Name);
            }

            string filterSql = filters.Count > 0 ? $"WHERE {string.Join(" AND ", filters)}" : string.Empty;
            return (parameters, filterSql);
        }

        private string LockClause() => query.WithLock ? "FOR UPDATE" : string.Empty;
    }


    extension(ParsingStage stage)
    {
        public async Task Save(NpgSqlSession session, CancellationToken ct = default)
        {
            const string sql = """
            INSERT INTO avito_spares_parser.stages(id, name)
            VALUES (@id, @name)
            ON CONFLICT(id) 
            DO UPDATE SET name = @name
            """;
            CommandDefinition command = session.FormCommand(sql, stage.ExtractParameters(), ct: ct);
            await session.Execute(command);
        }

        public async Task Update(NpgSqlSession session, CancellationToken ct = default)
        {
            const string sql = $"""
            UPDATE avito_spares_parser.stages
            SET name = @name
            WHERE id = @id
            """;
            CommandDefinition command = session.FormCommand(sql, stage.ExtractParameters(), ct: ct);
            await session.Execute(command);
        }

        public async Task Clear(NpgSqlSession session, CancellationToken ct = default)
        {
            const string sql = "DELETE FROM avito_spares_parser.stages";
            CommandDefinition command = new(sql, cancellationToken: ct, transaction: session.Transaction);
            await session.Execute(command);
        }

        private object ExtractParameters() => new
        {
            id = stage.Id,
            name = stage.Name
        };
    }
}