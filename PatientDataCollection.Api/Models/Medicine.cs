namespace PatientDataCollection.Api.Models;

public class Medicine
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
    public bool RequiresPrescription { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
}
