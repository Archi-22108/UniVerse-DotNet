using System;
using System.Collections.Generic;

namespace UniVerse.Web.Models;

/// <summary>
/// View model for "My Requests" campus delivery management page
/// Matching the user's screenshot 1:1.
/// </summary>
public class MyRequestsViewModel
{
    public int ActiveCount { get; set; } = 2;
    public int DeliveredCount { get; set; } = 0;
    public int CancelledCount { get; set; } = 0;
    public int TotalCount => ActiveCount + DeliveredCount + CancelledCount;
    public string AvgDeliveryTime { get; set; } = "~15 mins";
    public string ActiveTab { get; set; } = "active";
    public DeliveryRequestItemDto? HighlightedActiveRequest { get; set; }
    public List<DeliveryRequestItemDto> Requests { get; set; } = new();
}

public class DeliveryRequestItemDto
{
    public int Id { get; set; }
    public string FormattedId { get; set; } = "#D0113435";
    public string ItemTitle { get; set; } = "CrunchEx Chili Tadka";
    public string PickupSpot { get; set; } = "Hostel Vending Machine";
    public string Destination { get; set; } = "Hostel A - Room 400";
    public int ItemCount { get; set; } = 1;
    public decimal TotalAmount { get; set; } = 25.0m;
    public decimal RewardFee { get; set; } = 5.0m;
    public string Status { get; set; } = "Requested"; // Requested, Accepted, Picked Up, In Transit, Delivered, Cancelled
    public string TimeAgo { get; set; } = "6 minutes ago";
    public string OtpCode { get; set; } = "9855";
    public bool IsLiveRadarActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
