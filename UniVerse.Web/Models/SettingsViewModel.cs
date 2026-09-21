using System.ComponentModel.DataAnnotations;

namespace UniVerse.Web.Models;

/// <summary>
/// ViewModel for the Settings page - ADO.NET backed campus preferences.
/// Demonstrates C# OOP Encapsulation with section-based properties.
/// </summary>
public class SettingsViewModel
{
    // Profile Info (read-only display)
    public string FullName { get; set; } = "Archi Kumari";
    public string Email { get; set; } = "archi.kumari126697@marwadiuniversity.ac.in";
    public string StudentId { get; set; } = "1041";
    public string Role { get; set; } = "Student";
    public string? ProfilePictureUrl { get; set; }
    public string Initial => !string.IsNullOrWhiteSpace(FullName) ? FullName.Trim()[0].ToString().ToUpper() : "A";
    public string DisplayHandle => "archi.kumar (53489)";

    // Hostel & Delivery Defaults
    public string HostelBlock { get; set; } = "Hostel A";
    public string RoomNumber { get; set; } = "";
    public string RunnerContact { get; set; } = "";
    public string QuickDropoffNote { get; set; } = "";

    // Security / Password Change
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
    public string ConfirmNewPassword { get; set; } = "";
    public string AccountStatus { get; set; } = "Active";

    // Notification Preferences
    public bool NotifyRequestUpdates { get; set; } = true;
    public bool NotifyDeliveryUpdates { get; set; } = true;
    public bool NotifyChatMessages { get; set; } = true;
    public bool NotifyMarketplace { get; set; } = true;
    public bool NotifyAudioChimes { get; set; } = true;

    // Privacy
    public string ProfileVisibility { get; set; } = "Public (Visible to campus)";
    public string ActivityVisibility { get; set; } = "Public";

    // Messages
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public string ActiveTab { get; set; } = "all";
}
