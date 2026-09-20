using System;

namespace UniVerse.Web.Models;

/// <summary>
/// Domain model for Campus Delivery Requests.
/// Demonstrates C# OOP inheritance from BaseEntity.
/// </summary>
public class DeliveryRequest : BaseEntity
{
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string HostelRoom { get; set; } = string.Empty;
    public string PickupLocation { get; set; } = "Campus Vending Machine (Hostel D)";
    public string DropoffLocation { get; set; } = "Hostel Room";
    public string ItemsDescription { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal RewardFee { get; set; } = 15.0m;
    public string Status { get; set; } = "Pending"; // Pending, Accepted, PickedUp, InTransit, Delivered, Cancelled
    public string? RunnerName { get; set; }
    public string? DeliveryOtp { get; set; }
}
