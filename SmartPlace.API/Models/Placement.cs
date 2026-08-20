namespace SmartPlace.API.Models;

public class Placement
{
    public int PlacementId { get; set; }

    public decimal OfferedPackage { get; set; }

    public DateTime? JoiningDate { get; set; }

    public string Status { get; set; } = "Placed";

    public string? OfferLetterUrl { get; set; }

    // Foreign Key
    public int StudentId { get; set; }

    // Navigation Property
    public Student? Student { get; set; }

    // Foreign Key
    public int CompanyId { get; set; }

    // Navigation Property
    public Company? Company { get; set; }
}