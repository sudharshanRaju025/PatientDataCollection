using MySqlConnector;

namespace PatientDataCollection.Api.Configuration;

public static class DatabaseConnection
{
    public static DatabaseOptions Resolve(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings:DefaultConnection in appsettings.json. Example: Server=localhost;Port=3306;Database=your_schema;User=root;Password=your_password;");
        }

        var builder = new MySqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.Server) || string.IsNullOrWhiteSpace(builder.Database) || string.IsNullOrWhiteSpace(builder.UserID))
        {
            throw new InvalidOperationException(
                "DefaultConnection must include Server (host), Port, Database, User, and Password.");
        }

        var schema = configuration["Database:Schema"];
        if (string.IsNullOrWhiteSpace(schema))
        {
            schema = "PatientData";
        }

        if (!IsSafeIdentifier(builder.Database) || !IsSafeIdentifier(schema))
        {
            throw new InvalidOperationException("Database and schema names can use only letters, numbers, and underscores.");
        }

        builder.TreatTinyAsBoolean = true;
        if (string.IsNullOrWhiteSpace(builder.CharacterSet))
        {
            builder.CharacterSet = "utf8mb4";
        }

        var serverVersion = configuration["Database:ServerVersion"];
        if (string.IsNullOrWhiteSpace(serverVersion))
        {
            serverVersion = "8.0.36-mysql";
        }

        return new DatabaseOptions
        {
            ConnectionString = builder.ConnectionString,
            ServerVersion = serverVersion.Trim(),
            Host = builder.Server,
            Port = builder.Port,
            DatabaseName = builder.Database,
            Schema = schema.Trim(),
            Username = builder.UserID
        };
    }

    public static bool IsSafeIdentifier(string value)
    {
        return value.Length > 0 && value.All(character => char.IsLetterOrDigit(character) || character == '_');
    }
}
