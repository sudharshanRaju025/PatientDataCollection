using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PatientDataCollection.Api.Configuration;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace PatientDataCollection.Api.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var database = DatabaseConnection.Resolve(configuration);
        var versionText = database.ServerVersion.Split('-', '+')[0];

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(database.ConnectionString, new MySqlServerVersion(Version.Parse(versionText)), mysql =>
            {
                mysql.SchemaBehavior(
                    MySqlSchemaBehavior.Translate,
                    (schemaName, objectName) => $"{schemaName}_{objectName}");
                mysql.MigrationsHistoryTable($"{database.Schema}___ef_migrations");
            })
            .Options;

        return new AppDbContext(options);
    }
}
