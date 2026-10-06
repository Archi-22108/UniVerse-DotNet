using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniVerse.Server.Data.Repositories;
using UniVerse.Server.Models;
using UniVerse.Server.Models.ViewModels;

namespace UniVerse.Server.Controllers
{
    public class DeliveryController : Controller
    {
        private readonly IDeliveryRepository _deliveryRepo;
        private readonly IUserRepository _userRepo;

        public DeliveryController(IDeliveryRepository deliveryRepo, IUserRepository userRepo)
        {
            _deliveryRepo = deliveryRepo;
            _userRepo = userRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? status)
        {
            var requests = await _deliveryRepo.GetRequestsAsync(status);
            ViewBag.CurrentStatus = status ?? "all";
            return View(requests);
        }

        private async Task<User> GetCurrentUserAsync()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string? userEmail = User.FindFirstValue(ClaimTypes.Email);
            User? currentUser = null;

            if (!string.IsNullOrEmpty(userId))
            {
                currentUser = await _userRepo.GetByIdAsync(userId);
            }

            if (currentUser == null && !string.IsNullOrEmpty(userEmail))
            {
                currentUser = await _userRepo.GetByEmailAsync(userEmail);
            }

            if (currentUser == null)
            {
                var cleanEmail = (userEmail ?? string.Empty).Trim().ToLowerInvariant();
                var displayName = User.Identity?.Name ?? (!string.IsNullOrEmpty(cleanEmail) ? cleanEmail.Split('@')[0] : "Student");
                currentUser = new User
                {
                    Id = userId ?? Guid.NewGuid().ToString(),
                    FullName = displayName,
                    Email = !string.IsNullOrEmpty(cleanEmail) ? cleanEmail : "student@marwadiuniversity.ac.in",
                    HostelName = "Hostel D",
                    RoomNumber = "304"
                };
            }

            return currentUser;
        }

        [HttpGet]
        [Route("Delivery/Create")]
        [Route("request/new")]
        public async Task<IActionResult> Create()
        {
            var user = await GetCurrentUserAsync();
            var hostel = !string.IsNullOrEmpty(user.HostelName) ? user.HostelName : "Hostel D";
            var room = !string.IsNullOrEmpty(user.RoomNumber) ? user.RoomNumber : "304";
            var activeRunners = await _userRepo.GetActiveRunnersAsync();
            ViewBag.RunnerCount = activeRunners?.Count ?? 0;

            return View(new DeliveryCreateViewModel
            {
                DropoffHostel = hostel,
                DropoffRoom = room,
                DropoffLocation = $"{hostel}, Room {room}",
                PickupLocation = "Hostel Vending Machine",
                DeliveryFee = 5.0
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Delivery/Create")]
        [Route("request/new")]
        public async Task<IActionResult> Create(DeliveryCreateViewModel model)
        {
            var user = await GetCurrentUserAsync();
            var userId = user.Id;

            // Determine dropoff location
            var dropoff = !string.IsNullOrWhiteSpace(model.DropoffLocation)
                ? model.DropoffLocation.Trim()
                : (model.DropoffHostel == "Other" ? $"Class Room: {model.DropoffRoom?.Trim()}" : $"{model.DropoffHostel}, Room {model.DropoffRoom?.Trim()}");

            // Determine pickup location
            var pickup = model.PickupLocation == "Other (Custom Spot)" && !string.IsNullOrWhiteSpace(model.CustomPickupLocation)
                ? model.CustomPickupLocation.Trim()
                : model.PickupLocation.Trim();

            var items = new List<RequestItem>();

            // Parse items from ItemsJson if provided
            if (!string.IsNullOrWhiteSpace(model.ItemsJson))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(model.ItemsJson);
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        var name = elem.TryGetProperty("name", out var n) ? n.GetString() : null;
                        var qty = elem.TryGetProperty("quantity", out var q) ? q.GetInt32() : 1;
                        var cat = elem.TryGetProperty("category", out var c) ? c.GetString() : "Vending Machine";
                        double price = 0;
                        if (elem.TryGetProperty("estimatedPrice", out var p) && p.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            price = p.GetDouble();
                        }

                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            items.Add(new RequestItem
                            {
                                Name = name.Trim(),
                                Quantity = Math.Max(1, qty),
                                Notes = cat,
                                EstimatedPrice = price
                            });
                        }
                    }
                }
                catch { }
            }

            // Fallback for simple single-item submission
            if (items.Count == 0)
            {
                var rawName = !string.IsNullOrWhiteSpace(model.ItemNames) ? model.ItemNames.Trim() : "Campus Order";
                items.Add(new RequestItem
                {
                    Name = rawName,
                    Quantity = Math.Max(1, model.Quantity),
                    EstimatedPrice = model.EstimatedAmount
                });
            }

            var totalItemCount = 0;
            foreach (var itm in items) totalItemCount += itm.Quantity;

            // Minimum reward: ₹5 per item
            var minReward = Math.Max(5.0, totalItemCount * 5.0);
            var finalReward = Math.Max(minReward, model.DeliveryFee);

            var totalEst = 0.0;
            foreach (var itm in items) totalEst += itm.EstimatedPrice * itm.Quantity;
            if (totalEst <= 0 && model.EstimatedAmount > 0) totalEst = model.EstimatedAmount;

            var newRequest = new DeliveryRequest
            {
                RequesterId = userId,
                PickupLocation = pickup,
                DropoffLocation = dropoff,
                Instructions = model.Instructions?.Trim(),
                TotalEstimatedAmount = totalEst,
                DeliveryFee = finalReward,
                Status = "pending"
            };

            var requestId = await _deliveryRepo.CreateRequestAsync(newRequest, items);
            TempData["SuccessMessage"] = "Delivery request successfully posted to campus runners!";
            return RedirectToAction("Details", new { id = requestId });
        }

        [HttpGet]
        [Route("Delivery/Details/{id}")]
        [Route("Deliveries/Details/{id}")]
        [Route("dashboard/requests/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var req = await _deliveryRepo.GetRequestByIdAsync(id);
            if (req == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.IsRequester = currentUserId == req.RequesterId;
            ViewBag.IsAssignedRunner = currentUserId == req.RunnerId;

            return View(req);
        }

        [HttpPost]
        [Route("Delivery/BoostReward")]
        [Route("api/requests/boost")]
        public async Task<IActionResult> BoostReward([FromForm] string? id, [FromForm] double? amount)
        {
            var targetId = id ?? Request.Query["id"].ToString();
            var boostAmt = amount ?? (double.TryParse(Request.Query["amount"], out var a) ? a : 5.0);

            if (!string.IsNullOrEmpty(targetId))
            {
                await _deliveryRepo.BoostRewardAsync(targetId, boostAmt);
            }
            var updated = !string.IsNullOrEmpty(targetId) ? await _deliveryRepo.GetRequestByIdAsync(targetId) : null;
            return Json(new { success = true, newFee = updated?.DeliveryFee ?? 0 });
        }

        [HttpPost]
        [Route("Delivery/Cancel")]
        [Route("api/requests/cancel")]
        public async Task<IActionResult> Cancel([FromForm] string? id)
        {
            var targetId = id ?? Request.Query["id"].ToString();
            if (!string.IsNullOrEmpty(targetId))
            {
                await _deliveryRepo.UpdateStatusAsync(targetId, "cancelled");
            }
            return Json(new { success = true });
        }

        [HttpGet]
        [Route("api/requests/status/{id}")]
        public async Task<IActionResult> GetStatus(string id)
        {
            var req = await _deliveryRepo.GetRequestByIdAsync(id);
            if (req == null) return NotFound();
            return Json(new
            {
                id = req.Id,
                status = req.Status,
                deliveryFee = req.DeliveryFee,
                runnerId = req.RunnerId,
                runnerName = req.RunnerName,
                runnerPhone = req.RunnerPhone,
                otp = req.DeliveryOtp
            });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> RunnerHub()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var pending = await _deliveryRepo.GetRequestsAsync("pending");
            var myActive = await _deliveryRepo.GetRequestsByRunnerAsync(userId);

            var vm = new RunnerHubViewModel
            {
                CurrentRunner = user,
                PendingDeliveries = pending,
                MyActiveDeliveries = myActive
            };

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleDuty(bool isActive)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _userRepo.ToggleRunnerDutyAsync(userId, isActive);
            TempData["StatusMessage"] = $"Runner duty status set to {(isActive ? "ONLINE & READY" : "OFFLINE")}.";
            return RedirectToAction("RunnerHub");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptDelivery(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var success = await _deliveryRepo.AssignRunnerAsync(id, userId);
            if (success)
            {
                TempData["SuccessMessage"] = "Order accepted! Navigate to the pickup spot.";
            }
            else
            {
                TempData["ErrorMessage"] = "Order is no longer pending or already taken.";
            }
            return RedirectToAction("RunnerHub");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string id, string status)
        {
            await _deliveryRepo.UpdateStatusAsync(id, status);
            TempData["SuccessMessage"] = $"Delivery status updated to {status.ToUpper()}.";
            return RedirectToAction("Details", new { id });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteDelivery(string id, string otp)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _deliveryRepo.CompleteDeliveryWithOtpAsync(id, userId, otp);
            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Awesome job! Order delivered and ₹{result.Reward:F2} tip credited to your wallet!";
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }
            return RedirectToAction("RunnerHub");
        }
    }
}
