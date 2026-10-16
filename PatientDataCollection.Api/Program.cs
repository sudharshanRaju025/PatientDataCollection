using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using PatientDataCollection.Api.Configuration;
using PatientDataCollection.Api.Data;
using PatientDataCollection.Api.Services;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    var logFile = Path.Combine(context.HostingEnvironment.ContentRootPath, "logs", "meridian-.log");
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            logFile,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            shared: true);
});

var database = DatabaseConnection.Resolve(builder.Configuration);
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) || Encoding.UTF8.GetByteCount(jwt.Key) < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters. Set it in appsettings.json.");
}

builder.Services.AddSingleton(database);
builder.Services.AddSingleton(jwt);

var versionText = database.ServerVersion.Split('-', '+')[0];
var serverVersion = new MySqlServerVersion(Version.Parse(versionText));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(database.ConnectionString, serverVersion, mysql => ConfigureMySql(mysql, database.Schema)));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<PharmacyService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => ToCamel(entry.Key),
                    entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
            {
                message = "Please check the highlighted fields.",
                errors
            });
        };
    });

builder.Services.AddOpenApi();

var app = builder.Build();

app.Logger.LogInformation(
    "Serilog is writing local logs to {LogFile}",
    Path.Combine(app.Environment.ContentRootPath, "logs", "meridian-.log"));

await PrepareDatabaseAsync(app, database);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSerilogRequestLogging();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

static void ConfigureMySql(Microsoft.EntityFrameworkCore.Infrastructure.MySqlDbContextOptionsBuilder mysql, string schema)
{
    mysql.SchemaBehavior(
        MySqlSchemaBehavior.Translate,
        (schemaName, objectName) => $"{schemaName}_{objectName}");
    mysql.MigrationsHistoryTable($"{schema}___ef_migrations");
}

static async Task EnsureDatabaseAsync(DatabaseOptions database)
{
    var bootstrap = new MySqlConnectionStringBuilder(database.ConnectionString)
    {
        Database = "mysql"
    };

    await using var connection = new MySqlConnection(bootstrap.ConnectionString);
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{database.DatabaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
    await command.ExecuteNonQueryAsync();
}

static string ToCamel(string key)
{
    if (string.IsNullOrEmpty(key))
    {
        return key;
    }

    var name = key.Split('.').Last();
    return char.ToLowerInvariant(name[0]) + name[1..];
}

static async Task PrepareDatabaseAsync(WebApplication app, DatabaseOptions database)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Database");
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        await EnsureDatabaseAsync(database);
        await db.Database.MigrateAsync();
        await DbInitializer.SeedAsync(db);
        logger.LogInformation(
            "MySQL is ready at {Host}:{Port}, database {Database}, schema {Schema}, user {Username}.",
            database.Host,
            database.Port,
            database.DatabaseName,
            database.Schema,
            database.Username);
    }
    catch (Exception ex)
    {
        logger.LogError(
            ex,
            "Could not open MySQL at {Host}:{Port}, database {Database}, schema {Schema}, user {Username}. Update ConnectionStrings:DefaultConnection in appsettings.json.",
            database.Host,
            database.Port,
            database.DatabaseName,
            database.Schema,
            database.Username);
        throw;
    }
}
