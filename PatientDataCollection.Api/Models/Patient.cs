namespace PatientDataCollection.Api.Models;

public class Patient
{
    public int Id { get; set; }
    public string MedicalRecordNumber { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = "";
    public string BloodGroup { get; set; } = "Unknown";
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
    public string AddressLine { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string EmergencyContactName { get; set; } = "";
    public string EmergencyContactPhone { get; set; } = "";
    public string? Allergies { get; set; }
    public string? CurrentMedications { get; set; }
    public string? MedicalHistory { get; set; }
    public string? PresentingNotes { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? InsuranceNumber { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
