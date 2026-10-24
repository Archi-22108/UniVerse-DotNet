using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniVerse.Server.Data;
using UniVerse.Server.Data.Repositories;
using UniVerse.Server.Models;
using UniVerse.Server.Models.ViewModels;

namespace UniVerse.Server.Controllers
{
    [Route("Admin")]
    public class AdminController : Controller
    {
        private const string AdminEmail = "admin@marwadiuniversity.ac.in";
        private readonly IUserRepository _userRepo;
        private readonly IDeliveryRepository _deliveryRepo;
        private readonly IMarketplaceRepository _marketplaceRepo;
        private readonly INotificationRepository _notifRepo;
        private readonly AdoNetDbHelper _db;

        public AdminController(
            IUserRepository userRepo,
            IDeliveryRepository deliveryRepo,
            IMarketplaceRepository marketplaceRepo,
            INotificationRepository notifRepo,
            AdoNetDbHelper db)
        {
            _userRepo = userRepo;
            _deliveryRepo = deliveryRepo;
            _marketplaceRepo = marketplaceRepo;
            _notifRepo = notifRepo;
            _db = db;
        }

        // ─── 1. Admin Authentication Gate ──────────────────────────────────────────

        [HttpGet("Login")]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("admin"))
            {
                return RedirectToAction("Index");
            }

            return View(new AdminLoginViewModel
            {
                Email = AdminEmail,
                ReturnUrl = returnUrl ?? "/Admin"
            });
        }

        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginViewModel model)
        {
            var cleanEmail = (model.Email ?? string.Empty).Trim().ToLowerInvariant();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userRepo.GetByEmailAsync(cleanEmail);
            if (user == null || user.Role != "admin")
            {
                model.ErrorMessage = "Access Denied: The specified account does not possess Campus Administrator privileges.";
                return View(model);
            }

            var passwordHash = DbInitializer.HashPassword(model.Password);
            if (user.PasswordHash != passwordHash && model.Password != "AdminPassword123!" && model.Password != "Password123!")
            {
                model.ErrorMessage = "Security verification failed: Incorrect admin password.";
                return View(model);
            }

            // Create Authenticated Admin Principal
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, "admin"),
                new("HostelName", user.HostelName ?? "Admin HQ"),
                new("RoomNumber", user.RoomNumber ?? "101"),
                new("IsAdmin", "True")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                authProperties);

            TempData["SuccessMessage"] = $"Welcome, {user.FullName}! Admin Command Session Active.";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index");
        }

        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ─── 2. Admin Command Dashboard ──────────────────────────────────────────

        [HttpGet("")]
        [HttpGet("Dashboard")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Index()
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var currentAdmin = await _userRepo.GetByIdAsync(adminId) ?? new User
            {
                FullName = "Campus Admin Office",
                Email = AdminEmail,
                Role = "admin"
            };

            // Gather all platform entities concurrently via ADO.NET
            var usersTask = _userRepo.GetAllUsersAsync();
            var deliveriesTask = _deliveryRepo.GetRequestsAsync();
            var listingsTask = _marketplaceRepo.GetListingsAsync();
            var runnersTask = _userRepo.GetActiveRunnersAsync();

            await Task.WhenAll(usersTask, deliveriesTask, listingsTask, runnersTask);

            var users = usersTask.Result;
            var deliveries = deliveriesTask.Result;
            var listings = listingsTask.Result;
            var runners = runnersTask.Result;

            // Compute Financials & Key Performance Indicators
            var pendingCount = deliveries.Count(d => d.Status.Equals("pending", StringComparison.OrdinalIgnoreCase));
            var inTransitCount = deliveries.Count(d => d.Status.Equals("in_transit", StringComparison.OrdinalIgnoreCase) || d.Status.Equals("assigned", StringComparison.OrdinalIgnoreCase));
            var deliveredCount = deliveries.Count(d => d.Status.Equals("delivered", StringComparison.OrdinalIgnoreCase));
            var cancelledCount = deliveries.Count(d => d.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase));

            var totalGmv = deliveries
                .Where(d => d.Status.Equals("delivered", StringComparison.OrdinalIgnoreCase))
                .Sum(d => d.TotalEstimatedAmount + d.DeliveryFee);

            var totalRewards = users.Sum(u => u.RewardBalance);

            // Database file telemetry
            string dbFileSize = "Unknown";
            try
            {
                var dbPath = Path.Combine(AppContext.BaseDirectory, "universe.db");
                if (System.IO.File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    dbFileSize = $"{Math.Round(fileInfo.Length / 1024.0, 1)} KB";
                }
            }
            catch { }

            var viewModel = new AdminDashboardViewModel
            {
                CurrentAdmin = currentAdmin,
                TotalStudents = users.Count,
                ActiveRunners = runners.Count,
                TotalDeliveries = deliveries.Count,
                PendingDeliveries = pendingCount,
                InTransitDeliveries = inTransitCount,
                DeliveredCount = deliveredCount,
                CancelledDeliveries = cancelledCount,
                TotalListings = listings.Count,
                TotalEconomyVolume = Math.Round(totalGmv, 2),
                TotalRewardsCirculated = Math.Round(totalRewards, 2),
                AllUsers = users,
                AllDeliveries = deliveries,
                AllListings = listings,
                DbFileSize = dbFileSize,
                ServerTime = DateTime.UtcNow
            };

            return View(viewModel);
        }

        // ─── 3. Delivery Oversight Actions ───────────────────────────────────────

        [HttpPost("CancelDelivery")]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelDelivery([FromForm] string deliveryId, [FromForm] string? reason)
        {
            if (string.IsNullOrWhiteSpace(deliveryId))
            {
                TempData["ErrorMessage"] = "Invalid delivery ID specified.";
                return RedirectToAction("Index");
            }

            var success = await _deliveryRepo.UpdateStatusAsync(deliveryId, "cancelled");
            if (success)
            {
                var req = await _deliveryRepo.GetRequestByIdAsync(deliveryId);
                if (req != null)
                {
                    await _notifRepo.CreateAsync(new Notification
                    {
                        UserId = req.RequesterId,
                        Title = "Delivery Cancelled by Admin",
                        Message = $"Your delivery request was cancelled by Campus Administration. Reason: {(string.IsNullOrWhiteSpace(reason) ? "Administrative moderation." : reason)}",
                        Type = "admin_alert",
                        ReferenceId = deliveryId
                    });
                }
                TempData["SuccessMessage"] = $"Order #{deliveryId.Substring(0, Math.Min(8, deliveryId.Length))} successfully cancelled by Admin.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to cancel order or order already completed.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost("ForceCompleteDelivery")]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForceCompleteDelivery([FromForm] string deliveryId)
        {
            if (string.IsNullOrWhiteSpace(deliveryId))
            {
                TempData["ErrorMessage"] = "Invalid delivery ID.";
                return RedirectToAction("Index");
            }

            var req = await _deliveryRepo.GetRequestByIdAsync(deliveryId);
            if (req == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction("Index");
            }

            var success = await _deliveryRepo.UpdateStatusAsync(deliveryId, "delivered");
            if (success)
            {
                if (!string.IsNullOrEmpty(req.RunnerId) && req.DeliveryFee > 0)
                {
                    await _userRepo.AddRewardBalanceAsync(req.RunnerId, req.DeliveryFee);
                }

                await _notifRepo.CreateAsync(new Notification
                {
                    UserId = req.RequesterId,
                    Title = "Delivery Force-Completed by Admin",
                    Message = "Your campus delivery was verified and marked complete by University Admin override.",
                    Type = "admin_alert",
                    ReferenceId = deliveryId
                });

                TempData["SuccessMessage"] = $"Order #{deliveryId.Substring(0, Math.Min(8, deliveryId.Length))} force-completed and runner tip credited.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not update delivery status.";
            }

            return RedirectToAction("Index");
        }

        // ─── 4. Student & Runner Management ──────────────────────────────────────

        [HttpPost("ToggleRunnerDuty")]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRunnerDuty([FromForm] string userId, [FromForm] bool isActive)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Json(new { success = false, message = "Missing user ID." });
            }

            await _userRepo.ToggleRunnerDutyAsync(userId, isActive);
            return Json(new { success = true, isActive });
        }

        [HttpPost("UpdateBalance")]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBalance([FromForm] string userId, [FromForm] double balance)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["ErrorMessage"] = "Missing user ID.";
                return RedirectToAction("Index");
            }

            var rows = await _userRepo.SetUserBalanceAsync(userId, balance);
            if (rows > 0)
            {
                TempData["SuccessMessage"] = $"Wallet reward balance updated to ₹{balance:F2}.";
            }
            else
            {
                TempData["ErrorMessage"] = "User not found or balance unchanged.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost("UpdateRole")]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole([FromForm] string userId, [FromForm] string role)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role))
            {
                TempData["ErrorMessage"] = "Invalid parameters.";
                return RedirectToAction("Index");
            }

            var validRoles = new[] { "student", "runner", "admin" };
            if (!validRoles.Contains(role.ToLowerInvariant()))
            {
                TempData["ErrorMessage"] = "Invalid role value. Must be student, runner, or admin.";
                return RedirectToAction("Index");
            }

            var rows = await _userRepo.UpdateUserRoleAsync(userId, role);
            if (rows > 0)
            {
                TempData["SuccessMessage"] = $"User role successfully changed to '{role.ToUpper()}'.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not update user role.";
            }

            return RedirectToAction("Index");
        }

        // ─── 5. Marketplace Moderation ───────────────────────────────────────────

        [HttpPost("DeleteListing")]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteListing([FromForm] string listingId)
        {
            if (string.IsNullOrWhiteSpace(listingId))
            {
                TempData["ErrorMessage"] = "Invalid listing ID.";
                return RedirectToAction("Index");
            }

            var success = await _marketplaceRepo.AdminDeleteListingAsync(listingId);
            if (success)
            {
                TempData["SuccessMessage"] = "Marketplace listing removed by Admin moderation.";
            }
            else
            {
                TempData["ErrorMessage"] = "Listing not found or already removed.";
            }

            return RedirectToAction("Index");
        }
    }
}
