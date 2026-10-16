using Microsoft.EntityFrameworkCore;
using PatientDataCollection.Api.Models;

namespace PatientDataCollection.Api.Data;

public static class DbInitializer
{
    public const string DefaultUsername = "admin";
    public const string DefaultPassword = "Admin@123";

    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Users.AnyAsync(cancellationToken))
        {
            db.Users.Add(new AppUser
            {
                Username = DefaultUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword),
                DisplayName = "Clinical Admin",
                Role = "Clinician",
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        await PharmacyCatalog.SeedAsync(db, cancellationToken);
    }
}
