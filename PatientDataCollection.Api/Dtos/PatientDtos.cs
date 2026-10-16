using System.ComponentModel.DataAnnotations;

namespace PatientDataCollection.Api.Dtos;

public class PatientWriteDto : IValidatableObject
{
    [Required, MaxLength(80)]
    public string FirstName { get; set; } = "";

    [Required, MaxLength(80)]
    public string LastName { get; set; } = "";

    [Required]
    public DateOnly DateOfBirth { get; set; }

    [Required, MaxLength(40)]
    public string Gender { get; set; } = "";

    [Required, MaxLength(12)]
    public string BloodGroup { get; set; } = "Unknown";

    [Required, MaxLength(24)]
    public string Phone { get; set; } = "";

    [MaxLength(160)]
    public string? Email { get; set; }

    [Required, MaxLength(240)]
    public string AddressLine { get; set; } = "";

    [Required, MaxLength(80)]
    public string City { get; set; } = "";

    [Required, MaxLength(80)]
    public string State { get; set; } = "";

    [Required, MaxLength(20)]
    public string PostalCode { get; set; } = "";

    [Required, MaxLength(120)]
    public string EmergencyContactName { get; set; } = "";

    [Required, MaxLength(24)]
    public string EmergencyContactPhone { get; set; } = "";

    [MaxLength(4000)]
    public string? Allergies { get; set; }

    [MaxLength(4000)]
    public string? CurrentMedications { get; set; }

    [MaxLength(4000)]
    public string? MedicalHistory { get; set; }

    [MaxLength(4000)]
    public string? PresentingNotes { get; set; }

    [MaxLength(120)]
    public string? InsuranceProvider { get; set; }

    [MaxLength(80)]
    public string? InsuranceNumber { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Active";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var genders = new[] { "Female", "Male", "Other", "Prefer not to say" };
        var groups = new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-", "Unknown" };
        var statuses = new[] { "Active", "Inactive" };

        if (!genders.Contains(Gender))
        {
            yield return new ValidationResult("Select a gender.", [nameof(Gender)]);
        }

        if (!groups.Contains(BloodGroup))
        {
            yield return new ValidationResult("Select a blood group.", [nameof(BloodGroup)]);
        }

        if (!statuses.Contains(Status))
        {
            yield return new ValidationResult("Select a status.", [nameof(Status)]);
        }

        if (DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            yield return new ValidationResult("Date of birth cannot be in the future.", [nameof(DateOfBirth)]);
        }

        if (DateOfBirth < new DateOnly(1900, 1, 1))
        {
            yield return new ValidationResult("Date of birth looks too far in the past.", [nameof(DateOfBirth)]);
        }

        if (!IsPhone(Phone))
        {
            yield return new ValidationResult("Enter a valid phone number.", [nameof(Phone)]);
        }

        if (!IsPhone(EmergencyContactPhone))
        {
            yield return new ValidationResult("Enter a valid emergency phone number.", [nameof(EmergencyContactPhone)]);
        }

        if (!string.IsNullOrWhiteSpace(Email) && !new EmailAddressAttribute().IsValid(Email))
        {
            yield return new ValidationResult("Enter a valid email address.", [nameof(Email)]);
        }
    }

    private static bool IsPhone(string value)
    {
        var digits = value.Count(char.IsDigit);
        return digits >= 7 && digits <= 15 && value.All(c => char.IsDigit(c) || "+-(). ".Contains(c));
    }
}

public class PatientSummaryDto
{
    public int Id { get; set; }
    public string MedicalRecordNumber { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = "";
    public string BloodGroup { get; set; } = "";
    public string Phone { get; set; } = "";
    public string City { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
}

public class PatientDetailDto : PatientSummaryDto
{
    public string? Email { get; set; }
    public string AddressLine { get; set; } = "";
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
    public DateTime CreatedAtUtc { get; set; }
}

public class PatientListDto
{
    public IReadOnlyList<PatientSummaryDto> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class PatientStatsDto
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
    public int UpdatedToday { get; set; }
}
