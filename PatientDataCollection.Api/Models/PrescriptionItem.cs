namespace PatientDataCollection.Api.Models;

public class PrescriptionItem
{
    public int Id { get; set; }
    public int PrescriptionId { get; set; }
    public Prescription Prescription { get; set; } = null!;
    public int MedicineId { get; set; }
    public Medicine Medicine { get; set; } = null!;
    public string Dosage { get; set; } = "";
    public int Quantity { get; set; }
    public int DaysSupply { get; set; }
    public decimal UnitPrice { get; set; }
}
