using System.Collections.Generic;

namespace UniVerse.Web.Models;

/// <summary>
/// ViewModel for Create / New Delivery Request flow.
/// Demonstrates C# OOP Encapsulation and Data Transfer Objects.
/// </summary>
public class CreateRequestViewModel
{
    public string StudentName { get; set; } = "Archi.kumari126697";
    public string HostelBlock { get; set; } = "Hostel D";
    public string RoomNumber { get; set; } = "D-402";
    public string HostelRoom => $"{HostelBlock} · Room {RoomNumber}";

    public string SelectedCategory { get; set; } = "Vending Machine";
    public string ActiveStockFilter { get; set; } = "all";

    public List<VendingProductDto> Products { get; set; } = new();

    // Form submission properties
    public string SelectedItemsJson { get; set; } = "[]";
    public decimal SubtotalAmount { get; set; } = 0;
    public decimal RewardFee { get; set; } = 15.0m;
    public decimal TotalAmount => SubtotalAmount + RewardFee;
    public string Urgency { get; set; } = "Standard";
    public string? Notes { get; set; }
}

public class VendingProductDto
{
    public int Id { get; set; }
    public string SlotCode { get; set; } = "A-01";
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = "Snack";
    public string SubCategory { get; set; } = "Chips"; // Chips, Drinks, Chocolates
    public decimal Price { get; set; } = 20.0m;
    public string ImageFileName { get; set; } = string.Empty;
    public bool InStock { get; set; } = true;
}

public class CartItemDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal Total => Price * Quantity;
}
