using Microsoft.EntityFrameworkCore;
using PatientDataCollection.Api.Data;
using PatientDataCollection.Api.Dtos;
using PatientDataCollection.Api.Models;

namespace PatientDataCollection.Api.Services;

public class PharmacyService(AppDbContext db)
{
    public async Task<PharmacySummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var pending = await db.Prescriptions.AsNoTracking()
            .Where(x => x.Status == "Pending")
            .OrderBy(x => x.CreatedAtUtc)
            .Take(6)
            .Select(SummaryProjection())
            .ToListAsync(cancellationToken);

        var lowStock = await db.Medicines.AsNoTracking()
            .Where(x => x.StockQuantity <= x.ReorderLevel)
            .OrderBy(x => x.StockQuantity)
            .Take(6)
            .Select(MedicineProjection())
            .ToListAsync(cancellationToken);

        var dispensedToday = db.Prescriptions.AsNoTracking().Where(x => x.Status == "Dispensed" && x.DispensedAtUtc >= today);

        return new PharmacySummaryDto
        {
            PatientCount = await db.Patients.CountAsync(cancellationToken),
            MedicineCount = await db.Medicines.CountAsync(cancellationToken),
            PendingPrescriptions = await db.Prescriptions.CountAsync(x => x.Status == "Pending", cancellationToken),
            DispensedToday = await dispensedToday.CountAsync(cancellationToken),
            LowStockCount = await db.Medicines.CountAsync(x => x.StockQuantity <= x.ReorderLevel, cancellationToken),
            DispensedValueToday = await db.PrescriptionItems.AsNoTracking()
                .Where(x => x.Prescription.Status == "Dispensed" && x.Prescription.DispensedAtUtc >= today)
                .SumAsync(x => (decimal?)(x.UnitPrice * x.Quantity), cancellationToken) ?? 0m,
            Pending = pending,
            LowStock = lowStock
        };
    }

    public async Task<MedicineListDto> SearchMedicinesAsync(string? search, bool lowStockOnly, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Medicines.AsNoTracking();
        if (lowStockOnly)
        {
            query = query.Where(x => x.StockQuantity <= x.ReorderLevel);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.Name.Contains(term) ||
                x.GenericName.Contains(term) ||
                x.Category.Contains(term) ||
                x.Manufacturer.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MedicineProjection())
            .ToListAsync(cancellationToken);

        return new MedicineListDto { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<MedicineDto?> GetMedicineAsync(int id, CancellationToken cancellationToken = default)
    {
        return await db.Medicines.AsNoTracking().Where(x => x.Id == id).Select(MedicineProjection()).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MedicineDto> CreateMedicineAsync(MedicineWriteDto input, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var medicine = new Medicine { CreatedAtUtc = now, UpdatedAtUtc = now };
        Apply(medicine, input);
        db.Medicines.Add(medicine);
        await db.SaveChangesAsync(cancellationToken);
        return ToMedicine(medicine);
    }

    public async Task<MedicineDto?> UpdateMedicineAsync(int id, MedicineWriteDto input, CancellationToken cancellationToken = default)
    {
        var medicine = await db.Medicines.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (medicine is null)
        {
            return null;
        }

        Apply(medicine, input);
        medicine.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToMedicine(medicine);
    }

    public async Task<PrescriptionListDto> SearchPrescriptionsAsync(string? search, string? status, int? patientId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = db.Prescriptions.AsNoTracking();
        if (patientId is > 0)
        {
            query = query.Where(x => x.PatientId == patientId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.PrescriptionNumber.Contains(term) ||
                x.PrescriberName.Contains(term) ||
                x.Patient.FirstName.Contains(term) ||
                x.Patient.LastName.Contains(term) ||
                (x.Patient.FirstName + " " + x.Patient.LastName).Contains(term) ||
                x.Patient.MedicalRecordNumber.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(SummaryProjection())
            .ToListAsync(cancellationToken);

        return new PrescriptionListDto { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<PrescriptionDetailDto?> GetPrescriptionAsync(int id, CancellationToken cancellationToken = default)
    {
        var prescription = await db.Prescriptions.AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Items)
            .ThenInclude(x => x.Medicine)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return prescription is null ? null : ToDetail(prescription);
    }

    public async Task<PrescriptionDetailDto> CreatePrescriptionAsync(PrescriptionWriteDto input, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(x => x.Id == input.PatientId, cancellationToken)
            ?? throw new InvalidOperationException("That patient chart was not found.");

        var medicineIds = input.Items.Select(x => x.MedicineId).ToList();
        var medicines = await db.Medicines.Where(x => medicineIds.Contains(x.Id)).ToListAsync(cancellationToken);
        if (medicines.Count != medicineIds.Count)
        {
            throw new InvalidOperationException("One of the selected medicines is no longer on the shelf.");
        }

        var now = DateTime.UtcNow;
        var prescription = new Prescription
        {
            PrescriptionNumber = await NextNumberAsync(cancellationToken),
            PatientId = patient.Id,
            PrescriberName = input.PrescriberName.Trim(),
            Notes = Clean(input.Notes),
            Status = "Pending",
            CreatedAtUtc = now,
            AllergyWarning = BuildAllergyWarning(patient.Allergies, medicines)
        };

        foreach (var line in input.Items)
        {
            var medicine = medicines.First(x => x.Id == line.MedicineId);
            prescription.Items.Add(new PrescriptionItem
            {
                MedicineId = medicine.Id,
                Dosage = line.Dosage.Trim(),
                Quantity = line.Quantity,
                DaysSupply = line.DaysSupply,
                UnitPrice = medicine.UnitPrice
            });
        }

        db.Prescriptions.Add(prescription);
        await db.SaveChangesAsync(cancellationToken);
        prescription.Patient = patient;
        foreach (var item in prescription.Items)
        {
            item.Medicine = medicines.First(x => x.Id == item.MedicineId);
        }

        return ToDetail(prescription);
    }

    public async Task<PrescriptionDetailDto> DispenseAsync(int id, CancellationToken cancellationToken = default)
    {
        var prescription = await db.Prescriptions
            .Include(x => x.Patient)
            .Include(x => x.Items)
            .ThenInclude(x => x.Medicine)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("That prescription was not found.");

        if (prescription.Status != "Pending")
        {
            throw new InvalidOperationException("Only a pending prescription can be dispensed.");
        }

        var shortStock = prescription.Items
            .Where(x => x.Medicine.StockQuantity < x.Quantity)
            .Select(x => $"{x.Medicine.Name} has {x.Medicine.StockQuantity} left, and this script needs {x.Quantity}.")
            .ToList();
        if (shortStock.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", shortStock));
        }

        foreach (var item in prescription.Items)
        {
            item.Medicine.StockQuantity -= item.Quantity;
            item.Medicine.UpdatedAtUtc = DateTime.UtcNow;
        }

        prescription.Status = "Dispensed";
        prescription.DispensedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToDetail(prescription);
    }

    public async Task<PrescriptionDetailDto> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var prescription = await db.Prescriptions
            .Include(x => x.Patient)
            .Include(x => x.Items)
            .ThenInclude(x => x.Medicine)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("That prescription was not found.");

        if (prescription.Status != "Pending")
        {
            throw new InvalidOperationException("Only a pending prescription can be cancelled.");
        }

        prescription.Status = "Cancelled";
        await db.SaveChangesAsync(cancellationToken);
        return ToDetail(prescription);
    }

    private async Task<string> NextNumberAsync(CancellationToken cancellationToken)
    {
        var token = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var number = $"RX-{DateTime.UtcNow:yyyy}-{token}";
        var exists = await db.Prescriptions.AnyAsync(x => x.PrescriptionNumber == number, cancellationToken);
        return exists ? $"RX-{DateTime.UtcNow:yyyyMMddHHmmss}" : number;
    }

    private static string? BuildAllergyWarning(string? allergies, IReadOnlyList<Medicine> medicines)
    {
        if (string.IsNullOrWhiteSpace(allergies))
        {
            return null;
        }

        var hits = medicines
            .Where(medicine =>
                ContainsTerm(allergies, medicine.Name) ||
                ContainsTerm(allergies, medicine.GenericName))
            .Select(medicine => medicine.Name)
            .Distinct()
            .ToList();

        return hits.Count == 0
            ? null
            : $"Chart allergies may overlap with {string.Join(", ", hits)}. Confirm before dispensing.";
    }

    private static bool ContainsTerm(string haystack, string term)
    {
        return term.Length >= 4 && haystack.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static void Apply(Medicine medicine, MedicineWriteDto input)
    {
        medicine.Name = input.Name.Trim();
        medicine.GenericName = input.GenericName.Trim();
        medicine.Strength = input.Strength.Trim();
        medicine.Form = input.Form.Trim();
        medicine.Category = input.Category.Trim();
        medicine.Manufacturer = input.Manufacturer.Trim();
        medicine.StockQuantity = input.StockQuantity;
        medicine.ReorderLevel = input.ReorderLevel;
        medicine.UnitPrice = input.UnitPrice;
        medicine.RequiresPrescription = input.RequiresPrescription;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static System.Linq.Expressions.Expression<Func<Medicine, MedicineDto>> MedicineProjection() => medicine => new MedicineDto
    {
        Id = medicine.Id,
        Name = medicine.Name,
        GenericName = medicine.GenericName,
        Strength = medicine.Strength,
        Form = medicine.Form,
        Category = medicine.Category,
        Manufacturer = medicine.Manufacturer,
        StockQuantity = medicine.StockQuantity,
        ReorderLevel = medicine.ReorderLevel,
        UnitPrice = medicine.UnitPrice,
        RequiresPrescription = medicine.RequiresPrescription,
        LowStock = medicine.StockQuantity <= medicine.ReorderLevel
    };

    private static System.Linq.Expressions.Expression<Func<Prescription, PrescriptionSummaryDto>> SummaryProjection() => prescription => new PrescriptionSummaryDto
    {
        Id = prescription.Id,
        PrescriptionNumber = prescription.PrescriptionNumber,
        PatientId = prescription.PatientId,
        PatientName = prescription.Patient.FirstName + " " + prescription.Patient.LastName,
        MedicalRecordNumber = prescription.Patient.MedicalRecordNumber,
        PrescriberName = prescription.PrescriberName,
        Status = prescription.Status,
        ItemCount = prescription.Items.Count,
        Total = prescription.Items.Sum(item => item.UnitPrice * item.Quantity),
        CreatedAtUtc = prescription.CreatedAtUtc,
        DispensedAtUtc = prescription.DispensedAtUtc
    };

    private static MedicineDto ToMedicine(Medicine medicine) => new()
    {
        Id = medicine.Id,
        Name = medicine.Name,
        GenericName = medicine.GenericName,
        Strength = medicine.Strength,
        Form = medicine.Form,
        Category = medicine.Category,
        Manufacturer = medicine.Manufacturer,
        StockQuantity = medicine.StockQuantity,
        ReorderLevel = medicine.ReorderLevel,
        UnitPrice = medicine.UnitPrice,
        RequiresPrescription = medicine.RequiresPrescription,
        LowStock = medicine.StockQuantity <= medicine.ReorderLevel
    };

    private static PrescriptionDetailDto ToDetail(Prescription prescription) => new()
    {
        Id = prescription.Id,
        PrescriptionNumber = prescription.PrescriptionNumber,
        PatientId = prescription.PatientId,
        PatientName = $"{prescription.Patient.FirstName} {prescription.Patient.LastName}",
        MedicalRecordNumber = prescription.Patient.MedicalRecordNumber,
        PrescriberName = prescription.PrescriberName,
        Status = prescription.Status,
        Notes = prescription.Notes,
        AllergyWarning = prescription.AllergyWarning,
        ItemCount = prescription.Items.Count,
        Total = prescription.Items.Sum(item => item.UnitPrice * item.Quantity),
        CreatedAtUtc = prescription.CreatedAtUtc,
        DispensedAtUtc = prescription.DispensedAtUtc,
        Items = prescription.Items.Select(item => new PrescriptionItemDto
        {
            Id = item.Id,
            MedicineId = item.MedicineId,
            MedicineName = item.Medicine.Name,
            Strength = item.Medicine.Strength,
            Form = item.Medicine.Form,
            Dosage = item.Dosage,
            Quantity = item.Quantity,
            DaysSupply = item.DaysSupply,
            UnitPrice = item.UnitPrice,
            LineTotal = item.UnitPrice * item.Quantity
        }).ToList()
    };
}
