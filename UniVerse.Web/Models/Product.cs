namespace UniVerse.Web.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageFileName { get; set; } = string.Empty;
    public bool InStock { get; set; } = true;
    public string VendingMachineId { get; set; } = "VM-HOSTEL-D";
}
