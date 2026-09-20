using System.Collections.Generic;

namespace UniVerse.Web.Models;

/// <summary>
/// View model for Runner Dashboard
/// Matches the user's screenshot 1:1 and supports full interactive runner operations.
/// </summary>
public class RunnerViewModel
{
    public bool IsOnline { get; set; } = true;
    public int AvailableCount { get; set; } = 0;
    public int ActiveCount { get; set; } = 0;
    public decimal TotalEarnings { get; set; } = 0.0m;
    public string Rating { get; set; } = "New";
    public string BadgeLevel { get; set; } = "LEVEL 1 STARTER RUNNER";
    public int CompletedDeliveriesCount { get; set; } = 0;
    public string ActiveTab { get; set; } = "available"; // available, active, history

    public List<RunnerOrderItemDto> AvailableOrders { get; set; } = new();
    public List<RunnerOrderItemDto> ActiveMissions { get; set; } = new();
    public List<RunnerOrderItemDto> CompletedHistory { get; set; } = new();
}

public class RunnerOrderItemDto
{
    public int Id { get; set; }
    public string FormattedId { get; set; } = "#D0113435";
    public string StudentRequester { get; set; } = "Archi.kumari126697";
    public string ItemTitle { get; set; } = "CrunchEx Chili Tadka";
    public string PickupSpot { get; set; } = "Hostel Vending Machine";
    public string Destination { get; set; } = "Hostel A - Room 400";
    public decimal RewardFee { get; set; } = 5.0m;
    public decimal ItemCost { get; set; } = 20.0m;
    public string Status { get; set; } = "Pending"; // Pending, Accepted, Picked Up, In Transit, Delivered
    public string OtpCode { get; set; } = "9855";
    public string TimeAgo { get; set; } = "Just now";
}
