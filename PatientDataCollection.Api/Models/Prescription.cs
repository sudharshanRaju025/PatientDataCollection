namespace PatientDataCollection.Api.Models;

public class Prescription
{
    public int Id { get; set; }
    public string PrescriptionNumber { get; set; } = "";
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public string PrescriberName { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string? Notes { get; set; }
    public string? AllergyWarning { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? DispensedAtUtc { get; set; }
    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}
