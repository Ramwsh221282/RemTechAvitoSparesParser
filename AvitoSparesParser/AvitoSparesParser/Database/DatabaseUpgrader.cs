using Microsoft.Extensions.Options;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace AvitoSparesParser.Database;

public sealed class DatabaseUpgrader : AbstractDatabaseUpgrader
{
    public DatabaseUpgrader(IOptions<NpgSqlOptions> options) : base(options)
    {
        OfAssembly(typeof(DatabaseUpgrader).Assembly);
    }

}

public static class DatabaseUpgraderInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterDatabaseUpgrader()
        {
            services.AddTransient<IDbUpgrader, DatabaseUpgrader>();
        }
    }
}