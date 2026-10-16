using Microsoft.EntityFrameworkCore;
using PatientDataCollection.Api.Data;
using PatientDataCollection.Api.Dtos;
using PatientDataCollection.Api.Models;

namespace PatientDataCollection.Api.Services;

public class PatientService(AppDbContext db)
{
    public async Task<PatientStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return new PatientStatsDto
        {
            Total = await db.Patients.CountAsync(cancellationToken),
            Active = await db.Patients.CountAsync(x => x.Status == "Active", cancellationToken),
            Inactive = await db.Patients.CountAsync(x => x.Status == "Inactive", cancellationToken),
            UpdatedToday = await db.Patients.CountAsync(x => x.UpdatedAtUtc >= today, cancellationToken)
        };
    }

    public async Task<PatientListDto> SearchAsync(string? search, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = db.Patients.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.FirstName.Contains(term) ||
                x.LastName.Contains(term) ||
                (x.FirstName + " " + x.LastName).Contains(term) ||
                x.MedicalRecordNumber.Contains(term) ||
                x.Phone.Contains(term) ||
                x.City.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PatientSummaryDto
            {
                Id = x.Id,
                MedicalRecordNumber = x.MedicalRecordNumber,
                FirstName = x.FirstName,
                LastName = x.LastName,
                DateOfBirth = x.DateOfBirth,
                Gender = x.Gender,
                BloodGroup = x.BloodGroup,
                Phone = x.Phone,
                City = x.City,
                Status = x.Status,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new PatientListDto
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PatientDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return patient is null ? null : ToDetail(patient);
    }

    public async Task<PatientDetailDto> CreateAsync(PatientWriteDto input, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var patient = new Patient
        {
            MedicalRecordNumber = await NextRecordNumberAsync(cancellationToken),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        Apply(patient, input);
        db.Patients.Add(patient);
        await db.SaveChangesAsync(cancellationToken);
        return ToDetail(patient);
    }

    public async Task<PatientDetailDto?> UpdateAsync(int id, PatientWriteDto input, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (patient is null)
        {
            return null;
        }

        Apply(patient, input);
        patient.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToDetail(patient);
    }

    private async Task<string> NextRecordNumberAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var token = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var mrn = $"MRN-{DateTime.UtcNow:yyyy}-{token}";
            var exists = await db.Patients.AnyAsync(x => x.MedicalRecordNumber == mrn, cancellationToken);
            if (!exists)
            {
                return mrn;
            }
        }

        return $"MRN-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    private static void Apply(Patient patient, PatientWriteDto input)
    {
        patient.FirstName = input.FirstName.Trim();
        patient.LastName = input.LastName.Trim();
        patient.DateOfBirth = input.DateOfBirth;
        patient.Gender = input.Gender.Trim();
        patient.BloodGroup = input.BloodGroup.Trim();
        patient.Phone = input.Phone.Trim();
        patient.Email = Clean(input.Email);
        patient.AddressLine = input.AddressLine.Trim();
        patient.City = input.City.Trim();
        patient.State = input.State.Trim();
        patient.PostalCode = input.PostalCode.Trim();
        patient.EmergencyContactName = input.EmergencyContactName.Trim();
        patient.EmergencyContactPhone = input.EmergencyContactPhone.Trim();
        patient.Allergies = Clean(input.Allergies);
        patient.CurrentMedications = Clean(input.CurrentMedications);
        patient.MedicalHistory = Clean(input.MedicalHistory);
        patient.PresentingNotes = Clean(input.PresentingNotes);
        patient.InsuranceProvider = Clean(input.InsuranceProvider);
        patient.InsuranceNumber = Clean(input.InsuranceNumber);
        patient.Status = input.Status.Trim();
    }

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static PatientDetailDto ToDetail(Patient patient) => new()
    {
        Id = patient.Id,
        MedicalRecordNumber = patient.MedicalRecordNumber,
        FirstName = patient.FirstName,
        LastName = patient.LastName,
        DateOfBirth = patient.DateOfBirth,
        Gender = patient.Gender,
        BloodGroup = patient.BloodGroup,
        Phone = patient.Phone,
        Email = patient.Email,
        AddressLine = patient.AddressLine,
        City = patient.City,
        State = patient.State,
        PostalCode = patient.PostalCode,
        EmergencyContactName = patient.EmergencyContactName,
        EmergencyContactPhone = patient.EmergencyContactPhone,
        Allergies = patient.Allergies,
        CurrentMedications = patient.CurrentMedications,
        MedicalHistory = patient.MedicalHistory,
        PresentingNotes = patient.PresentingNotes,
        InsuranceProvider = patient.InsuranceProvider,
        InsuranceNumber = patient.InsuranceNumber,
        Status = patient.Status,
        CreatedAtUtc = patient.CreatedAtUtc,
        UpdatedAtUtc = patient.UpdatedAtUtc
    };
}
