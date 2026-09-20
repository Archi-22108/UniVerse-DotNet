namespace UniVerse.Web.Models;

/// <summary>
/// Domain model for Campus Vending & Store Products.
/// Demonstrates C# OOP inheritance from BaseEntity.
/// </summary>
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageFileName { get; set; } = string.Empty;
    public bool InStock { get; set; } = true;
    public string VendingMachineId { get; set; } = "VM-HOSTEL-D";
}
