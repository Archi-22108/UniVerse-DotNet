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

        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            var hostel = User.FindFirst("HostelName")?.Value ?? "Hostel D";
            var room = User.FindFirst("RoomNumber")?.Value ?? "304";

            return View(new DeliveryCreateViewModel
            {
                DropoffLocation = $"{hostel}, Room {room}"
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DeliveryCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var newRequest = new DeliveryRequest
            {
                RequesterId = userId,
                PickupLocation = model.PickupLocation.Trim(),
                DropoffLocation = model.DropoffLocation.Trim(),
                Instructions = model.Instructions?.Trim(),
                TotalEstimatedAmount = model.EstimatedAmount,
                DeliveryFee = model.DeliveryFee,
                Status = "pending"
            };

            var items = new List<RequestItem>
            {
                new()
                {
                    Name = model.ItemNames.Trim(),
                    Quantity = model.Quantity,
                    EstimatedPrice = model.EstimatedAmount
                }
            };

            var requestId = await _deliveryRepo.CreateRequestAsync(newRequest, items);
            TempData["SuccessMessage"] = "Delivery request successfully posted to campus runners!";
            return RedirectToAction("Details", new { id = requestId });
        }

        [HttpGet]
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
