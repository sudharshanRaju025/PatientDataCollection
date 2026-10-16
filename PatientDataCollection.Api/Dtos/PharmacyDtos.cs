using System.ComponentModel.DataAnnotations;

namespace PatientDataCollection.Api.Dtos;

public class MedicineWriteDto : IValidatableObject
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = "";

    [Required, MaxLength(120)]
    public string GenericName { get; set; } = "";

    [Required, MaxLength(40)]
    public string Strength { get; set; } = "";

    [Required, MaxLength(40)]
    public string Form { get; set; } = "";

    [Required, MaxLength(60)]
    public string Category { get; set; } = "";

    [Required, MaxLength(120)]
    public string Manufacturer { get; set; } = "";

    [Range(0, 1000000)]
    public int StockQuantity { get; set; }

    [Range(0, 100000)]
    public int ReorderLevel { get; set; }

    [Range(0, 1000000)]
    public decimal UnitPrice { get; set; }

    public bool RequiresPrescription { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var forms = new[] { "Tablet", "Capsule", "Syrup", "Injection", "Cream", "Inhaler", "Drops", "Sachet" };
        if (!forms.Contains(Form))
        {
            yield return new ValidationResult("Select a medicine form.", [nameof(Form)]);
        }
    }
}

public class MedicineDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string GenericName { get; set; } = "";
    public string Strength { get; set; } = "";
    public string Form { get; set; } = "";
    public string Category { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public int StockQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public decimal UnitPrice { get; set; }
    public bool RequiresPrescription { get; set; }
    public bool LowStock { get; set; }
}

public class MedicineListDto
{
    public IReadOnlyList<MedicineDto> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class PrescriptionItemWriteDto
{
    [Range(1, int.MaxValue)]
    public int MedicineId { get; set; }

    [Required, MaxLength(160)]
    public string Dosage { get; set; } = "";

    [Range(1, 1000)]
    public int Quantity { get; set; }

    [Range(1, 365)]
    public int DaysSupply { get; set; }
}

public class PrescriptionWriteDto : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int PatientId { get; set; }

    [Required, MaxLength(120)]
    public string PrescriberName { get; set; } = "";

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MinLength(1)]
    public List<PrescriptionItemWriteDto> Items { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Items.Count == 0)
        {
            yield return new ValidationResult("Add at least one medicine.", [nameof(Items)]);
        }

        if (Items.Select(x => x.MedicineId).Distinct().Count() != Items.Count)
        {
            yield return new ValidationResult("Each medicine can appear only once on a prescription.", [nameof(Items)]);
        }
    }
}

public class PrescriptionItemDto
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = "";
    public string Strength { get; set; } = "";
    public string Form { get; set; } = "";
    public string Dosage { get; set; } = "";
    public int Quantity { get; set; }
    public int DaysSupply { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class PrescriptionSummaryDto
{
    public int Id { get; set; }
    public string PrescriptionNumber { get; set; } = "";
    public int PatientId { get; set; }
    public string PatientName { get; set; } = "";
    public string MedicalRecordNumber { get; set; } = "";
    public string PrescriberName { get; set; } = "";
    public string Status { get; set; } = "";
    public int ItemCount { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? DispensedAtUtc { get; set; }
}

public class PrescriptionDetailDto : PrescriptionSummaryDto
{
    public string? Notes { get; set; }
    public string? AllergyWarning { get; set; }
    public IReadOnlyList<PrescriptionItemDto> Items { get; set; } = [];
}

public class PrescriptionListDto
{
    public IReadOnlyList<PrescriptionSummaryDto> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class PharmacySummaryDto
{
    public int PatientCount { get; set; }
    public int MedicineCount { get; set; }
    public int PendingPrescriptions { get; set; }
    public int DispensedToday { get; set; }
    public int LowStockCount { get; set; }
    public decimal DispensedValueToday { get; set; }
    public IReadOnlyList<PrescriptionSummaryDto> Pending { get; set; } = [];
    public IReadOnlyList<MedicineDto> LowStock { get; set; } = [];
}
