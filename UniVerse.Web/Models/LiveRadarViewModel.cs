using System;
using System.Collections.Generic;

namespace UniVerse.Web.Models;

/// <summary>
/// View Model for Live Campus Radar & Active Request Tracking
/// Matching the user's high-tech cyber radar screen 1:1.
/// </summary>
public class LiveRadarViewModel
{
    public int RequestId { get; set; } = 1;
    public string CustomRequestId { get; set; } = "#14C402C7";
    public string StudentName { get; set; } = "Archi.kumari126697";
    public string PickupSpot { get; set; } = "Hostel Vending Machine";
    public string DestinationRoom { get; set; } = "Hostel A - Room 400";
    public string ItemsDescription { get; set; } = "1x CrunchEx Chili Tadka";
    public decimal ItemsCost { get; set; } = 20.0m;
    public decimal RunnerReward { get; set; } = 5.0m;
    public decimal TotalAmount => ItemsCost + RunnerReward;
    public string Status { get; set; } = "Looking for Student Runners";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int NearbyRunnersCount { get; set; } = 14;
    public List<RadarItemDto> Items { get; set; } = new();
}

public class RadarItemDto
{
    public int Quantity { get; set; } = 1;
    public string Name { get; set; } = "CrunchEx Chili Tadka";
    public decimal Price { get; set; } = 20.0m;
}
