using Microsoft.EntityFrameworkCore;
using PatientDataCollection.Api.Models;

namespace PatientDataCollection.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();

    public const string SchemaName = "PatientData";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users", SchemaName);
            entity.HasIndex(x => x.Username).IsUnique();
            entity.Property(x => x.Username).HasMaxLength(80).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(40).IsRequired();
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("patients", SchemaName);
            entity.HasIndex(x => x.MedicalRecordNumber).IsUnique();
            entity.HasIndex(x => x.LastName);
            entity.HasIndex(x => x.Phone);
            entity.Property(x => x.MedicalRecordNumber).HasMaxLength(32).IsRequired();
            entity.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Gender).HasMaxLength(40).IsRequired();
            entity.Property(x => x.BloodGroup).HasMaxLength(12).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(24).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(160);
            entity.Property(x => x.AddressLine).HasMaxLength(240).IsRequired();
            entity.Property(x => x.City).HasMaxLength(80).IsRequired();
            entity.Property(x => x.State).HasMaxLength(80).IsRequired();
            entity.Property(x => x.PostalCode).HasMaxLength(20).IsRequired();
            entity.Property(x => x.EmergencyContactName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.EmergencyContactPhone).HasMaxLength(24).IsRequired();
            entity.Property(x => x.Allergies).HasColumnType("text");
            entity.Property(x => x.CurrentMedications).HasColumnType("text");
            entity.Property(x => x.MedicalHistory).HasColumnType("text");
            entity.Property(x => x.PresentingNotes).HasColumnType("text");
            entity.Property(x => x.InsuranceProvider).HasMaxLength(120);
            entity.Property(x => x.InsuranceNumber).HasMaxLength(80);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Medicine>(entity =>
        {
            entity.ToTable("medicines", SchemaName);
            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.Category);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.GenericName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Strength).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Form).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Manufacturer).HasMaxLength(120).IsRequired();
            entity.Property(x => x.UnitPrice).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.ToTable("prescriptions", SchemaName);
            entity.HasIndex(x => x.PrescriptionNumber).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.PrescriptionNumber).HasMaxLength(32).IsRequired();
            entity.Property(x => x.PrescriberName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.AllergyWarning).HasMaxLength(2000);
            entity.HasOne(x => x.Patient)
                .WithMany()
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PrescriptionItem>(entity =>
        {
            entity.ToTable("prescription_items", SchemaName);
            entity.Property(x => x.Dosage).HasMaxLength(160).IsRequired();
            entity.Property(x => x.UnitPrice).HasPrecision(10, 2);
            entity.HasOne(x => x.Prescription)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Medicine)
                .WithMany(x => x.PrescriptionItems)
                .HasForeignKey(x => x.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
