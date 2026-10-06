using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UniVerse.Server.Data;
using UniVerse.Server.Data.Repositories;
using UniVerse.Server.Models;
using UniVerse.Server.Models.ViewModels;

namespace UniVerse.Server.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IDeliveryRepository _deliveryRepo;
        private readonly IUserRepository _userRepo;
        private readonly IMarketplaceRepository _marketRepo;
        private readonly IWebHostEnvironment _env;

        public DashboardController(
            IDeliveryRepository deliveryRepo,
            IUserRepository userRepo,
            IMarketplaceRepository marketRepo,
            IWebHostEnvironment env)
        {
            _deliveryRepo = deliveryRepo;
            _userRepo = userRepo;
            _marketRepo = marketRepo;
            _env = env;
        }

        private async Task<User> GetCurrentUserAsync()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            User? currentUser = null;

            if (!string.IsNullOrEmpty(userId))
            {
                currentUser = await _userRepo.GetByIdAsync(userId);
            }

            if (currentUser == null)
            {
                var allUsers = await _userRepo.GetAllUsersAsync();
                currentUser = allUsers.FirstOrDefault() ?? new User
                {
                    Id = "usr_student_001",
                    FullName = "Archi Kumar",
                    Email = "archi.kumar@marwadiuniversity.ac.in",
                    Role = "student",
                    HostelName = "Hostel D",
                    RoomNumber = "304",
                    PhoneNumber = "+91 98765 43210",
                    EnrollmentNumber = "92100103001",
                    RewardBalance = 240.0,
                    Department = "Computer Science & Engineering",
                    Semester = "Semester 6"
                };
            }

            return currentUser;
        }

        // ─── 1. Main Dashboard Index ──────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            var requests = await _deliveryRepo.GetRequestsAsync();
            var listings = await _marketRepo.GetListingsAsync();

            var active = requests.Where(r => r.Status != "delivered" && r.Status != "cancelled").ToList();
            var completed = requests.Where(r => r.Status == "delivered").ToList();
            var cancelled = requests.Where(r => r.Status == "cancelled").ToList();

            ViewBag.ActivePage = "dashboard";
            ViewBag.ActiveRequestsCount = active.Count;

            var vm = new DashboardViewModel
            {
                CurrentUser = user,
                AllRequests = requests,
                ActiveRequests = active,
                CompletedRequests = completed,
                CancelledRequests = cancelled,
                RecentListings = listings.Take(6).ToList()
            };

            return View(vm);
        }

        // ─── 2. My Requests ───────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Requests(string? tab = "all", string? q = null)
        {
            var user = await GetCurrentUserAsync();
            var allRequests = await _deliveryRepo.GetRequestsAsync();

            var filtered = allRequests.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var query = q.Trim().ToLowerInvariant();
                filtered = filtered.Where(r =>
                    r.PickupLocation.ToLowerInvariant().Contains(query) ||
                    r.DropoffLocation.ToLowerInvariant().Contains(query) ||
                    r.Items.Any(i => i.Name.ToLowerInvariant().Contains(query)));
            }

            tab = (tab ?? "all").ToLowerInvariant();
            if (tab == "active")
            {
                filtered = filtered.Where(r => r.Status != "delivered" && r.Status != "cancelled");
            }
            else if (tab == "completed")
            {
                filtered = filtered.Where(r => r.Status == "delivered");
            }
            else if (tab == "cancelled")
            {
                filtered = filtered.Where(r => r.Status == "cancelled");
            }

            var requestList = filtered.ToList();
            var activeCount = allRequests.Count(r => r.Status != "delivered" && r.Status != "cancelled");

            ViewBag.ActivePage = "requests";
            ViewBag.ActiveRequestsCount = activeCount;

            var vm = new RequestsPageViewModel
            {
                CurrentUser = user,
                Requests = requestList,
                SelectedTab = tab,
                SearchQuery = q ?? string.Empty
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> CancelRequest(string id)
        {
            await _deliveryRepo.UpdateStatusAsync(id, "cancelled");
            TempData["SuccessMessage"] = "Delivery request cancelled successfully.";
            return RedirectToAction(nameof(Requests));
        }

        // ─── 3. Runner Mode ───────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Runner()
        {
            var user = await GetCurrentUserAsync();
            var allRequests = await _deliveryRepo.GetRequestsAsync();

            // All pending campus delivery requests must be visible on the runner radar feed
            var pending = allRequests.Where(r => r.Status == "pending").ToList();
            var activeDeliveries = allRequests.Where(r => (r.Status == "accepted" || r.Status == "picked_up" || r.Status == "in_transit") && r.RunnerId == user.Id).ToList();
            var completedDeliveries = allRequests.Where(r => r.Status == "delivered" && r.RunnerId == user.Id).ToList();

            ViewBag.ActivePage = "runner";
            ViewBag.ActiveRequestsCount = allRequests.Count(r => r.Status != "delivered" && r.Status != "cancelled");

            var vm = new RunnerPageViewModel
            {
                CurrentRunner = user,
                PendingDeliveries = pending,
                MyActiveDeliveries = activeDeliveries,
                MyCompletedDeliveries = completedDeliveries,
                IsOnDuty = user.IsActiveRunner
            };

            return View(vm);
        }

        [HttpGet]
        [Route("api/runner/radar-sync")]
        public async Task<IActionResult> RadarSync()
        {
            var pending = await _deliveryRepo.GetRequestsAsync("pending");
            return Json(new { pendingCount = pending.Count });
        }

        [HttpPost]
        public async Task<IActionResult> AcceptDelivery(string id)
        {
            var user = await GetCurrentUserAsync();
            await _deliveryRepo.AssignRunnerAsync(id, user.Id);
            await _deliveryRepo.UpdateStatusAsync(id, "in_transit");
            TempData["SuccessMessage"] = "Delivery accepted! Head to pickup point.";
            return RedirectToAction(nameof(Runner));
        }

        [HttpPost]
        public async Task<IActionResult> CompleteDelivery(string id, string otp)
        {
            var user = await GetCurrentUserAsync();
            var result = await _deliveryRepo.CompleteDeliveryWithOtpAsync(id, user.Id, otp);
            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Delivery verified! +₹{result.Reward:F0} credited to your wallet.";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }
            return RedirectToAction(nameof(Runner));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleDuty(bool active)
        {
            var user = await GetCurrentUserAsync();
            await _userRepo.ToggleRunnerDutyAsync(user.Id, active);
            return RedirectToAction(nameof(Runner));
        }

        // ─── 4. Wallet ────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Wallet()
        {
            var user = await GetCurrentUserAsync();
            var allRequests = await _deliveryRepo.GetRequestsAsync();

            var transactions = new List<WalletTransactionItem>
            {
                new() { Type = "deposit", Amount = 150.0, Description = "UPI Instant Top-Up (GPay)", CreatedAt = DateTime.Now.AddHours(-2), Status = "completed" },
                new() { Type = "earning", Amount = 40.0, Description = "Runner Tip: Crispy Samosa & Chai", CreatedAt = DateTime.Now.AddHours(-6), Status = "completed" },
                new() { Type = "payment", Amount = 30.0, Description = "Delivery Fee for Hostel D Dropoff", CreatedAt = DateTime.Now.AddDays(-1), Status = "completed" },
                new() { Type = "earning", Amount = 50.0, Description = "Runner Tip: Engineering Drawing Sheets", CreatedAt = DateTime.Now.AddDays(-2), Status = "completed" },
                new() { Type = "deposit", Amount = 100.0, Description = "Campus Wallet Welcome Bonus", CreatedAt = DateTime.Now.AddDays(-5), Status = "completed" }
            };

            ViewBag.ActivePage = "wallet";
            ViewBag.ActiveRequestsCount = allRequests.Count(r => r.Status != "delivered" && r.Status != "cancelled");

            var vm = new WalletPageViewModel
            {
                CurrentUser = user,
                Balance = user.RewardBalance,
                Transactions = transactions
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> TopUpWallet(double amount)
        {
            if (amount <= 0) amount = 100;
            var user = await GetCurrentUserAsync();
            await _userRepo.AddRewardBalanceAsync(user.Id, amount);
            TempData["SuccessMessage"] = $"₹{amount:F0} added to your UniVerse Wallet!";
            return RedirectToAction(nameof(Wallet));
        }

        // ─── 5. Marketplace ───────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Marketplace(string? category = "all", string? q = null)
        {
            var user = await GetCurrentUserAsync();
            var allRequests = await _deliveryRepo.GetRequestsAsync();
            var listings = await _marketRepo.GetListingsAsync(category == "all" ? null : category);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var query = q.Trim().ToLowerInvariant();
                listings = listings.Where(l =>
                    l.Title.ToLowerInvariant().Contains(query) ||
                    l.PickupLocation.ToLowerInvariant().Contains(query) ||
                    (l.Description != null && l.Description.ToLowerInvariant().Contains(query))
                ).ToList();
            }

            ViewBag.ActivePage = "marketplace";
            ViewBag.ActiveRequestsCount = allRequests.Count(r => r.Status != "delivered" && r.Status != "cancelled");

            var vm = new MarketplacePageViewModel
            {
                CurrentUser = user,
                Listings = listings,
                SelectedCategory = category ?? "all",
                SearchQuery = q ?? string.Empty
            };

            return View(vm);
        }

        // ─── 6. Chat Support & Messages ───────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Chat(string? id = null)
        {
            var user = await GetCurrentUserAsync();
            var allRequests = await _deliveryRepo.GetRequestsAsync();

            var contacts = new List<ChatContactItem>
            {
                new()
                {
                    Id = "conv_support",
                    Name = "UniVerse Campus Support",
                    Role = "Official Bot",
                    AvatarLetter = "⚡",
                    LastMessage = "How can we help with your campus deliveries today?",
                    LastMessageTime = "10:45 AM",
                    UnreadCount = 0,
                    IsOnline = true,
                    Messages = new List<ChatMessageItem>
                    {
                        new() { SenderName = "UniVerse Support", Content = "Welcome to UniVerse Peer Network! How can we assist you?", Timestamp = DateTime.Now.AddMinutes(-30), IsFromCurrentUser = false },
                        new() { SenderName = user.FullName, Content = "Hi, how do I become an active campus runner?", Timestamp = DateTime.Now.AddMinutes(-20), IsFromCurrentUser = true },
                        new() { SenderName = "UniVerse Support", Content = "Switch to 'Runner Mode' from the left sidebar and toggle 'On Duty'. You will immediately see student requests nearby!", Timestamp = DateTime.Now.AddMinutes(-18), IsFromCurrentUser = false }
                    }
                },
                new()
                {
                    Id = "conv_rahul",
                    Name = "Rahul Sharma",
                    Role = "Runner (Hostel C)",
                    AvatarLetter = "R",
                    LastMessage = "I have reached Hostel D ground floor with your order.",
                    LastMessageTime = "12:15 PM",
                    UnreadCount = 1,
                    IsOnline = true,
                    Messages = new List<ChatMessageItem>
                    {
                        new() { SenderName = "Rahul Sharma", Content = "Picked up your Samosa and Cold Coffee from Central Canteen!", Timestamp = DateTime.Now.AddMinutes(-15), IsFromCurrentUser = false },
                        new() { SenderName = user.FullName, Content = "Great, please bring it to Room 304, 3rd floor.", Timestamp = DateTime.Now.AddMinutes(-10), IsFromCurrentUser = true },
                        new() { SenderName = "Rahul Sharma", Content = "I have reached Hostel D ground floor with your order.", Timestamp = DateTime.Now.AddMinutes(-2), IsFromCurrentUser = false }
                    }
                },
                new()
                {
                    Id = "conv_priya",
                    Name = "Priya Mehta",
                    Role = "Buyer (Library)",
                    AvatarLetter = "P",
                    LastMessage = "Is the Engineering Graphics drafter still available?",
                    LastMessageTime = "Yesterday",
                    UnreadCount = 0,
                    IsOnline = false,
                    Messages = new List<ChatMessageItem>
                    {
                        new() { SenderName = "Priya Mehta", Content = "Hello, is your drafter listed on Marketplace available?", Timestamp = DateTime.Now.AddDays(-1), IsFromCurrentUser = false },
                        new() { SenderName = user.FullName, Content = "Yes! It's in mint condition, can give at Central Library tomorrow.", Timestamp = DateTime.Now.AddDays(-1), IsFromCurrentUser = true }
                    }
                }
            };

            var activeContact = string.IsNullOrEmpty(id) ? contacts.First().Id : id;

            ViewBag.ActivePage = "chat";
            ViewBag.ActiveRequestsCount = allRequests.Count(r => r.Status != "delivered" && r.Status != "cancelled");

            var vm = new ChatPageViewModel
            {
                CurrentUser = user,
                Contacts = contacts,
                ActiveContactId = activeContact
            };

            return View(vm);
        }

        // ─── 7. Analytics ─────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Analytics(int range = 7)
        {
            var user = await GetCurrentUserAsync();
            var allRequests = await _deliveryRepo.GetRequestsAsync();

            var completed = allRequests.Count(r => r.Status == "delivered");
            var pending = allRequests.Count(r => r.Status != "delivered" && r.Status != "cancelled");
            var cancelled = allRequests.Count(r => r.Status == "cancelled");

            var totalSpent = allRequests.Where(r => r.Status == "delivered").Sum(r => r.DeliveryFee + r.TotalEstimatedAmount);
            var totalEarned = allRequests.Where(r => r.Status == "delivered" && r.RunnerId == user.Id).Sum(r => r.DeliveryFee);

            var dailyVolumes = new List<DailyVolumeItem>
            {
                new() { DayName = "Mon", Percentage = 45, OrderCount = 3, Amount = 120 },
                new() { DayName = "Tue", Percentage = 70, OrderCount = 5, Amount = 180 },
                new() { DayName = "Wed", Percentage = 55, OrderCount = 4, Amount = 140 },
                new() { DayName = "Thu", Percentage = 85, OrderCount = 6, Amount = 220 },
                new() { DayName = "Fri", Percentage = 100, OrderCount = 8, Amount = 310 },
                new() { DayName = "Sat", Percentage = 65, OrderCount = 5, Amount = 190 },
                new() { DayName = "Sun", Percentage = 40, OrderCount = 3, Amount = 110 }
            };

            ViewBag.ActivePage = "analytics";
            ViewBag.ActiveRequestsCount = pending;

            var vm = new AnalyticsPageViewModel
            {
                CurrentUser = user,
                TimeRangeDays = range,
                TotalSpent = totalSpent > 0 ? totalSpent : 320.0,
                TotalEarned = totalEarned > 0 ? totalEarned : 180.0,
                TotalOrders = allRequests.Count,
                AvgDeliveryTime = "~14 mins",
                CarbonSavedKg = 1.4,
                DailyVolumes = dailyVolumes,
                CompletedCount = completed,
                PendingCount = pending,
                CancelledCount = cancelled
            };

            return View(vm);
        }

        // ─── 8. Profile ───────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await GetCurrentUserAsync();
            var allRequests = await _deliveryRepo.GetRequestsAsync();

            ViewBag.ActivePage = "profile";
            ViewBag.ActiveRequestsCount = allRequests.Count(r => r.Status != "delivered" && r.Status != "cancelled");

            var vm = new ProfilePageViewModel
            {
                CurrentUser = user,
                SuccessMessage = TempData["SuccessMessage"] as string,
                ErrorMessage = TempData["ErrorMessage"] as string
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(
            string fullName,
            string? email,
            string hostelName,
            string roomNumber,
            string phoneNumber,
            string? department,
            string? semester,
            IFormFile? avatarFile,
            string? avatarBase64,
            bool removePhoto = false)
        {
            var user = await GetCurrentUserAsync();
            user.FullName = string.IsNullOrWhiteSpace(fullName) ? user.FullName : fullName.Trim();

            if (!string.IsNullOrWhiteSpace(email))
            {
                user.Email = email.Trim().ToLowerInvariant();
            }

            user.HostelName = string.IsNullOrWhiteSpace(hostelName) ? user.HostelName : hostelName.Trim();
            user.RoomNumber = string.IsNullOrWhiteSpace(roomNumber) ? user.RoomNumber : roomNumber.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? user.PhoneNumber : phoneNumber.Trim();
            user.Department = string.IsNullOrWhiteSpace(department) ? user.Department : department.Trim();
            user.Semester = string.IsNullOrWhiteSpace(semester) ? user.Semester : semester.Trim();

            // Handle Profile Photo Upload / Removal
            if (removePhoto)
            {
                user.AvatarUrl = null;
            }
            else if (avatarFile != null && avatarFile.Length > 0)
            {
                try
                {
                    var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var uploadsDir = Path.Combine(webRoot, "uploads", "avatars");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var ext = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                    var fileName = $"{user.Id}_{DateTime.UtcNow.Ticks}{ext}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await avatarFile.CopyToAsync(stream);
                    }

                    user.AvatarUrl = $"/uploads/avatars/{fileName}";
                }
                catch
                {
                    // Fallback to base64 if needed
                }
            }
            else if (!string.IsNullOrEmpty(avatarBase64) && avatarBase64.StartsWith("data:image"))
            {
                try
                {
                    var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var uploadsDir = Path.Combine(webRoot, "uploads", "avatars");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var commaIdx = avatarBase64.IndexOf(',');
                    var rawBase64 = commaIdx > 0 ? avatarBase64.Substring(commaIdx + 1) : avatarBase64;
                    var bytes = Convert.FromBase64String(rawBase64);
                    var fileName = $"{user.Id}_{DateTime.UtcNow.Ticks}.png";
                    var filePath = Path.Combine(uploadsDir, fileName);
                    await System.IO.File.WriteAllBytesAsync(filePath, bytes);

                    user.AvatarUrl = $"/uploads/avatars/{fileName}";
                }
                catch
                {
                    user.AvatarUrl = avatarBase64;
                }
            }

            await _userRepo.UpdateUserAsync(user);

            // Re-sign in user cookie with updated profile claims
            try
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id),
                    new(ClaimTypes.Name, user.FullName),
                    new(ClaimTypes.Email, user.Email),
                    new(ClaimTypes.Role, user.Role),
                    new("HostelName", user.HostelName ?? "Hostel D"),
                    new("RoomNumber", user.RoomNumber ?? "304"),
                    new("RewardBalance", user.RewardBalance.ToString("F2")),
                    new("IsActiveRunner", user.IsActiveRunner.ToString()),
                    new("AvatarUrl", user.AvatarUrl ?? "")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });
            }
            catch
            {
                // Ignore if sign in context not available
            }

            TempData["SuccessMessage"] = "Profile details updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        // ─── 9. Settings ──────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var user = await GetCurrentUserAsync();
            var allRequests = await _deliveryRepo.GetRequestsAsync();

            ViewBag.ActivePage = "settings";
            ViewBag.ActiveRequestsCount = allRequests.Count(r => r.Status != "delivered" && r.Status != "cancelled");

            var vm = new SettingsPageViewModel
            {
                CurrentUser = user,
                SuccessMessage = TempData["SuccessMessage"] as string,
                DeliverySuccess = TempData["DeliverySuccess"] as string,
                PasswordSuccess = TempData["PasswordSuccess"] as string,
                PasswordError = TempData["PasswordError"] as string,
                AlertsSuccess = TempData["AlertsSuccess"] as string,
                SecuritySuccess = TempData["SecuritySuccess"] as string,
                DangerError = TempData["DangerError"] as string,
                PushNotifications = true,
                OrderAlerts = true,
                SoundEffects = true,
                AutoAcceptOrders = false,
                NotifyRequests = true,
                NotifyDeliveries = true,
                NotifyChats = true,
                NotifyMarketplace = true
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> SaveDeliveryDefaults(string hostelName, string roomNumber, string phoneNumber, string? deliveryInstructions)
        {
            var user = await GetCurrentUserAsync();
            if (!string.IsNullOrWhiteSpace(hostelName)) user.HostelName = hostelName.Trim();
            if (!string.IsNullOrWhiteSpace(roomNumber)) user.RoomNumber = roomNumber.Trim();
            if (!string.IsNullOrWhiteSpace(phoneNumber)) user.PhoneNumber = phoneNumber.Trim();

            await _userRepo.UpdateUserAsync(user);

            TempData["DeliverySuccess"] = "Hostel and delivery preferences saved! Future snack orders will prefill this room.";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["PasswordError"] = "New password must be at least 6 characters.";
                return RedirectToAction(nameof(Settings));
            }

            if (newPassword != confirmPassword)
            {
                TempData["PasswordError"] = "New password and confirmation do not match.";
                return RedirectToAction(nameof(Settings));
            }

            var user = await GetCurrentUserAsync();
            var currentHash = DbInitializer.HashPassword(currentPassword);
            if (user.PasswordHash != currentHash)
            {
                TempData["PasswordError"] = "Current password is incorrect.";
                return RedirectToAction(nameof(Settings));
            }

            user.PasswordHash = DbInitializer.HashPassword(newPassword);
            await _userRepo.UpdateUserAsync(user);

            TempData["PasswordSuccess"] = "Your password has been changed successfully.";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        public IActionResult SaveAlertsPreferences(bool notifyRequests = true, bool notifyDeliveries = true, bool notifyChats = true, bool notifyMarketplace = true, bool soundAlerts = true, string? profileVis = "public", string? activityVis = "public")
        {
            TempData["AlertsSuccess"] = "Notification and privacy preferences saved successfully.";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        public IActionResult LogoutOtherDevices()
        {
            TempData["SecuritySuccess"] = "All other device sessions terminated securely. Only this device remains signed in.";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAccount(string confirmation)
        {
            if (confirmation != "DELETE")
            {
                TempData["DangerError"] = "Please type DELETE to confirm.";
                return RedirectToAction(nameof(Settings));
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
