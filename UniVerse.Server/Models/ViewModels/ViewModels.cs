using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UniVerse.Server.Models.ViewModels
{
    // ─── Account ViewModels ──────────────────────────────────────────────────

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Student email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Student Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; } = true;

        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Student email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [Display(Name = "University Email (@marwadiuniversity.ac.in)")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Enrollment Number")]
        public string? EnrollmentNumber { get; set; }

        [Required(ErrorMessage = "Please select your primary campus role.")]
        [Display(Name = "Role")]
        public string Role { get; set; } = "student"; // "student" or "runner"

        [Required(ErrorMessage = "Hostel building is required.")]
        [Display(Name = "Hostel Building")]
        public string HostelName { get; set; } = "Hostel D";

        [Required(ErrorMessage = "Room number is required.")]
        [Display(Name = "Room Number")]
        public string RoomNumber { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Contact Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ─── Delivery ViewModels ─────────────────────────────────────────────────

    public class DeliveryCreateViewModel
    {
        [Required(ErrorMessage = "Pickup location is required.")]
        [Display(Name = "Pickup Spot (Canteen, Stationery, Gate)")]
        public string PickupLocation { get; set; } = "Marwadi Central Canteen";

        [Required(ErrorMessage = "Drop-off hostel is required.")]
        [Display(Name = "Drop-off Location")]
        public string DropoffLocation { get; set; } = "Hostel D, Room 304";

        [Display(Name = "Special Instructions for Runner")]
        public string? Instructions { get; set; }

        [Range(10, 500, ErrorMessage = "Delivery fee tip must be between ₹10 and ₹500.")]
        [Display(Name = "Runner Tip / Delivery Fee (₹)")]
        public double DeliveryFee { get; set; } = 30.0;

        [Required(ErrorMessage = "Item name is required.")]
        [Display(Name = "Items Ordered (e.g. Samosa, Cold Coffee, Drawing Sheet)")]
        public string ItemNames { get; set; } = string.Empty;

        [Range(1, 20, ErrorMessage = "Quantity must be between 1 and 20.")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; } = 1;

        [Range(0, 10000, ErrorMessage = "Estimated price must be valid.")]
        [Display(Name = "Total Estimated Amount (₹)")]
        public double EstimatedAmount { get; set; } = 100.0;
    }

    public class RunnerHubViewModel
    {
        public User CurrentRunner { get; set; } = new();
        public List<DeliveryRequestDetailDto> PendingDeliveries { get; set; } = new();
        public List<DeliveryRequestDetailDto> MyActiveDeliveries { get; set; } = new();
    }

    // ─── Marketplace ViewModels ──────────────────────────────────────────────

    public class MarketplaceCreateViewModel
    {
        [Required(ErrorMessage = "Listing title is required.")]
        [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
        [Display(Name = "Item Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a category.")]
        public string Category { get; set; } = "Books";

        [Required(ErrorMessage = "Please specify item condition.")]
        public string Condition { get; set; } = "Like New";

        [Required(ErrorMessage = "Price is required.")]
        [Range(1, 100000, ErrorMessage = "Price must be greater than ₹0.")]
        [Display(Name = "Selling Price (₹)")]
        public double Price { get; set; }

        [Display(Name = "Original Purchase Price / MRP (₹)")]
        public double? OriginalPrice { get; set; }

        [Display(Name = "Price is Negotiable")]
        public bool Negotiable { get; set; } = true;

        [Required(ErrorMessage = "Campus meetup location is required.")]
        [Display(Name = "Pickup / Exchange Location on Campus")]
        public string PickupLocation { get; set; } = "Central Library or Hostel D";

        [Display(Name = "Detailed Description")]
        public string? Description { get; set; }

        [Display(Name = "Image URL (Optional)")]
        public string? ImageUrl { get; set; }
    }

    public class MakeOfferViewModel
    {
        [Required]
        public string ListingId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Offer amount is required.")]
        [Range(1, 100000, ErrorMessage = "Offer must be greater than ₹0.")]
        [Display(Name = "Your Offer Price (₹)")]
        public double OfferPrice { get; set; }
    }

    // ─── Dashboard ViewModels ────────────────────────────────────────────────
    public class DashboardViewModel
    {
        public User CurrentUser { get; set; } = new();
        public List<DeliveryRequestDetailDto> AllRequests { get; set; } = new();
        public List<DeliveryRequestDetailDto> ActiveRequests { get; set; } = new();
        public List<DeliveryRequestDetailDto> CompletedRequests { get; set; } = new();
        public List<DeliveryRequestDetailDto> CancelledRequests { get; set; } = new();
        public List<MarketplaceListingDetailDto> RecentListings { get; set; } = new();
        public int TotalRequestsCount => AllRequests.Count;
        public int ActiveRequestsCount => ActiveRequests.Count;
        public int CompletedRequestsCount => CompletedRequests.Count;
        public int CancelledRequestsCount => CancelledRequests.Count;
    }

    // ─── Requests Page ViewModel ──────────────────────────────────────────────
    public class RequestsPageViewModel
    {
        public User CurrentUser { get; set; } = new();
        public List<DeliveryRequestDetailDto> Requests { get; set; } = new();
        public string SelectedTab { get; set; } = "all";
        public string SearchQuery { get; set; } = string.Empty;
        public int ActiveCount => Requests.Count(r => r.Status != "delivered" && r.Status != "cancelled");
        public int CompletedCount => Requests.Count(r => r.Status == "delivered");
        public int CancelledCount => Requests.Count(r => r.Status == "cancelled");
    }

    // ─── Runner Page ViewModel ────────────────────────────────────────────────
    public class RunnerPageViewModel
    {
        public User CurrentRunner { get; set; } = new();
        public List<DeliveryRequestDetailDto> PendingDeliveries { get; set; } = new();
        public List<DeliveryRequestDetailDto> MyActiveDeliveries { get; set; } = new();
        public List<DeliveryRequestDetailDto> MyCompletedDeliveries { get; set; } = new();
        public bool IsOnDuty { get; set; } = true;
        public double TodayEarnings => MyCompletedDeliveries.Sum(d => d.DeliveryFee);
    }

    // ─── Wallet ViewModels ────────────────────────────────────────────────────
    public class WalletTransactionItem
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString();
        public string Type { get; set; } = "deposit"; // "deposit", "earning", "payment", "refund"
        public double Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "completed"; // "completed", "pending", "failed"
        public System.DateTime CreatedAt { get; set; } = System.DateTime.Now;
    }

    public class WalletPageViewModel
    {
        public User CurrentUser { get; set; } = new();
        public double Balance { get; set; }
        public List<WalletTransactionItem> Transactions { get; set; } = new();
    }

    // ─── Marketplace Page ViewModel ───────────────────────────────────────────
    public class MarketplacePageViewModel
    {
        public User CurrentUser { get; set; } = new();
        public List<MarketplaceListingDetailDto> Listings { get; set; } = new();
        public string SelectedCategory { get; set; } = "all";
        public string SearchQuery { get; set; } = string.Empty;
    }

    // ─── Chat ViewModels ──────────────────────────────────────────────────────
    public class ChatMessageItem
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString();
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public System.DateTime Timestamp { get; set; } = System.DateTime.Now;
        public bool IsFromCurrentUser { get; set; }
    }

    public class ChatContactItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";
        public string AvatarLetter { get; set; } = "S";
        public string LastMessage { get; set; } = string.Empty;
        public string LastMessageTime { get; set; } = string.Empty;
        public int UnreadCount { get; set; }
        public bool IsOnline { get; set; } = true;
        public List<ChatMessageItem> Messages { get; set; } = new();
    }

    public class ChatPageViewModel
    {
        public User CurrentUser { get; set; } = new();
        public List<ChatContactItem> Contacts { get; set; } = new();
        public string ActiveContactId { get; set; } = string.Empty;
    }

    // ─── Analytics Page ViewModel ─────────────────────────────────────────────
    public class DailyVolumeItem
    {
        public string DayName { get; set; } = string.Empty;
        public int Percentage { get; set; }
        public int OrderCount { get; set; }
        public double Amount { get; set; }
    }

    public class AnalyticsPageViewModel
    {
        public User CurrentUser { get; set; } = new();
        public int TimeRangeDays { get; set; } = 7;
        public double TotalSpent { get; set; }
        public double TotalEarned { get; set; }
        public int TotalOrders { get; set; }
        public string AvgDeliveryTime { get; set; } = "~14 mins";
        public double CarbonSavedKg { get; set; } = 1.2;
        public List<DailyVolumeItem> DailyVolumes { get; set; } = new();
        public int CompletedCount { get; set; }
        public int PendingCount { get; set; }
        public int CancelledCount { get; set; }
    }

    // ─── Profile & Settings ViewModels ─────────────────────────────────────────
    public class ProfilePageViewModel
    {
        public User CurrentUser { get; set; } = new();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class SettingsPageViewModel
    {
        public User CurrentUser { get; set; } = new();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? DeliverySuccess { get; set; }
        public string? PasswordSuccess { get; set; }
        public string? PasswordError { get; set; }
        public string? AlertsSuccess { get; set; }
        public string? SecuritySuccess { get; set; }
        public string? DangerError { get; set; }
        public bool PushNotifications { get; set; } = true;
        public bool OrderAlerts { get; set; } = true;
        public bool SoundEffects { get; set; } = true;
        public bool AutoAcceptOrders { get; set; } = false;
        public bool NotifyRequests { get; set; } = true;
        public bool NotifyDeliveries { get; set; } = true;
        public bool NotifyChats { get; set; } = true;
        public bool NotifyMarketplace { get; set; } = true;
        public string ProfileVisibility { get; set; } = "public";
        public string ActivityVisibility { get; set; } = "public";
        public string DefaultInstructions { get; set; } = "";
    }
}
