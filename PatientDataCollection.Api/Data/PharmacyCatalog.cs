using Microsoft.EntityFrameworkCore;
using PatientDataCollection.Api.Models;

namespace PatientDataCollection.Api.Data;

public static class PharmacyCatalog
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Medicines.AnyAsync(cancellationToken))
        {
            var now = DateTime.UtcNow;
            db.Medicines.AddRange(Catalog(now));
            await db.SaveChangesAsync(cancellationToken);
        }

        if (await db.Prescriptions.AnyAsync(cancellationToken))
        {
            return;
        }

        var patients = await db.Patients.OrderBy(x => x.Id).Take(4).ToListAsync(cancellationToken);
        var medicines = await db.Medicines.OrderBy(x => x.Id).Take(6).ToListAsync(cancellationToken);
        if (patients.Count == 0 || medicines.Count < 2)
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;
        db.Prescriptions.Add(Script(patients[0], medicines[0], medicines[1], "Dr. Meera Shah", "Pending", nowUtc.AddHours(-3), null));
        if (patients.Count > 1)
        {
            db.Prescriptions.Add(Script(patients[1], medicines[2], medicines.Count > 3 ? medicines[3] : medicines[0], "Dr. Arun Patel", "Pending", nowUtc.AddHours(-1), null));
        }

        if (patients.Count > 2)
        {
            var dispensed = Script(patients[2], medicines[0], medicines.Count > 4 ? medicines[4] : medicines[1], "Dr. Meera Shah", "Dispensed", nowUtc.AddHours(-6), nowUtc.AddHours(-5));
            foreach (var item in dispensed.Items)
            {
                var shelf = medicines.First(x => x.Id == item.MedicineId);
                shelf.StockQuantity = Math.Max(0, shelf.StockQuantity - item.Quantity);
            }

            db.Prescriptions.Add(dispensed);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Prescription Script(Patient patient, Medicine first, Medicine second, string prescriber, string status, DateTime created, DateTime? dispensed)
    {
        return new Prescription
        {
            PrescriptionNumber = $"RX-{created:yyyy}-{patient.Id:0000}{first.Id:00}",
            PatientId = patient.Id,
            PrescriberName = prescriber,
            Status = status,
            Notes = "Seeded from the existing patient chart.",
            CreatedAtUtc = created,
            DispensedAtUtc = dispensed,
            Items =
            [
                new PrescriptionItem { MedicineId = first.Id, Dosage = "1 tablet twice daily after food", Quantity = 10, DaysSupply = 5, UnitPrice = first.UnitPrice },
                new PrescriptionItem { MedicineId = second.Id, Dosage = "1 dose at night", Quantity = 5, DaysSupply = 5, UnitPrice = second.UnitPrice }
            ]
        };
    }

    private static IEnumerable<Medicine> Catalog(DateTime now)
    {
        (string Name, string Generic, string Strength, string Form, string Category, string Maker, int Stock, int Reorder, decimal Price)[] rows =
        [
            ("Crocin", "Paracetamol", "500 mg", "Tablet", "Analgesic", "GSK", 240, 40, 2.50m),
            ("Brufen", "Ibuprofen", "400 mg", "Tablet", "Analgesic", "Abbott", 18, 30, 4.00m),
            ("Mox", "Amoxicillin", "500 mg", "Capsule", "Antibiotic", "Sun Pharma", 120, 40, 8.50m),
            ("Azee", "Azithromycin", "250 mg", "Tablet", "Antibiotic", "Cipla", 64, 20, 18.00m),
            ("Okacet", "Cetirizine", "10 mg", "Tablet", "Antihistamine", "Cipla", 300, 50, 1.75m),
            ("Omez", "Omeprazole", "20 mg", "Capsule", "Gastric", "Dr. Reddy's", 90, 25, 6.00m),
            ("Glycomet", "Metformin", "500 mg", "Tablet", "Diabetes", "USV", 210, 40, 3.25m),
            ("Amlong", "Amlodipine", "5 mg", "Tablet", "Cardiac", "Micro Labs", 12, 25, 5.50m),
            ("Atorva", "Atorvastatin", "10 mg", "Tablet", "Cardiac", "Zydus", 75, 20, 7.00m),
            ("Asthalin", "Salbutamol", "100 mcg", "Inhaler", "Respiratory", "Cipla", 22, 10, 145.00m),
            ("Electral", "Oral rehydration salts", "21.8 g", "Sachet", "Electrolyte", "FDC", 180, 40, 22.00m),
            ("Calcirol", "Cholecalciferol", "60000 IU", "Capsule", "Supplement", "Cadila", 40, 15, 35.00m),
            ("Pan", "Pantoprazole", "40 mg", "Tablet", "Gastric", "Alkem", 8, 20, 9.00m),
            ("Losar", "Losartan", "50 mg", "Tablet", "Cardiac", "Unichem", 55, 20, 11.00m),
            ("Ecosprin", "Aspirin", "75 mg", "Tablet", "Cardiac", "USV", 140, 30, 1.50m),
            ("Benadryl", "Diphenhydramine", "100 ml", "Syrup", "Cough", "Johnson", 16, 12, 125.00m),
            ("Lantus", "Insulin glargine", "100 IU/ml", "Injection", "Diabetes", "Sanofi", 9, 8, 720.00m),
            ("Volini", "Diclofenac", "30 g", "Cream", "Analgesic", "Sun Pharma", 48, 15, 95.00m),
            ("Montair", "Montelukast", "10 mg", "Tablet", "Respiratory", "Cipla", 70, 20, 14.00m),
            ("Ciplox", "Ciprofloxacin", "500 mg", "Tablet", "Antibiotic", "Cipla", 36, 20, 12.50m)
        ];

        foreach (var row in rows)
        {
            yield return new Medicine
            {
                Name = row.Name,
                GenericName = row.Generic,
                Strength = row.Strength,
                Form = row.Form,
                Category = row.Category,
                Manufacturer = row.Maker,
                StockQuantity = row.Stock,
                ReorderLevel = row.Reorder,
                UnitPrice = row.Price,
                RequiresPrescription = row.Category != "Supplement" && row.Category != "Electrolyte",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
        }
    }
}
