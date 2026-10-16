using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatientDataCollection.Api.Configuration;
using PatientDataCollection.Api.Data;

namespace PatientDataCollection.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController(AppDbContext db, DatabaseOptions database, ILogger<HealthController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var connected = false;
        string? error = null;
        try
        {
            connected = await db.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Health check could not reach MySQL.");
            error = "The API could not reach MySQL. Check host, port, schema, username, and password.";
        }

        return Ok(new
        {
            status = connected ? "ready" : "database_unavailable",
            database = new
            {
                connected,
                host = database.Host,
                port = database.Port,
                database = database.DatabaseName,
                schema = database.Schema,
                username = database.Username,
                serverVersion = database.ServerVersion,
                error
            }
        });
    }
}
