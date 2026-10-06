using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniVerse.Server.Data.Repositories;
using UniVerse.Server.Models;

namespace UniVerse.Server.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationRepository _notifRepo;
        private readonly IUserRepository _userRepo;

        public NotificationController(INotificationRepository notifRepo, IUserRepository userRepo)
        {
            _notifRepo = notifRepo;
            _userRepo = userRepo;
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string? userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userRepo.GetByIdAsync(userId);
                if (user != null) return user;
            }

            if (!string.IsNullOrEmpty(userEmail))
            {
                return await _userRepo.GetByEmailAsync(userEmail);
            }

            return null;
        }

        [HttpGet]
        [Route("api/notifications")]
        [Route("Notification/GetNotifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "Not authenticated" });
            }

            var notifications = await _notifRepo.GetByUserIdAsync(user.Id, 30);

            // Auto-provision welcome campus notifications if student has none yet
            if (notifications.Count == 0)
            {
                var initialNotifs = new List<Notification>
                {
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = user.Id,
                        Title = "Welcome to UniVerse Campus Hub",
                        Message = "Your Marwadi University verified student account is connected to the live campus delivery network.",
                        Type = "system",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-5).ToString("o")
                    },
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = user.Id,
                        Title = "Hostel Delivery Corridor Active",
                        Message = "Peer runners are standing by in Hostel D & MB-1 Canteen for quick delivery requests.",
                        Type = "runner",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-2).ToString("o")
                    },
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = user.Id,
                        Title = "Campus Canteen Live Radar",
                        Message = "Stationery, library prints, and snacks can be requested with 100% hostel delivery OTP verification.",
                        Type = "delivery",
                        IsRead = true,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-1).ToString("o")
                    }
                };

                foreach (var notif in initialNotifs)
                {
                    await _notifRepo.CreateAsync(notif);
                }

                notifications = await _notifRepo.GetByUserIdAsync(user.Id, 30);
            }

            int unreadCount = await _notifRepo.GetUnreadCountAsync(user.Id);

            return Json(new
            {
                success = true,
                unreadCount,
                notifications
            });
        }

        [HttpPost]
        [Route("api/notifications/mark-read/{id}")]
        [Route("Notification/MarkRead/{id}")]
        public async Task<IActionResult> MarkRead(string id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            await _notifRepo.MarkAsReadAsync(id, user.Id);
            int unread = await _notifRepo.GetUnreadCountAsync(user.Id);

            return Json(new { success = true, unreadCount = unread });
        }

        [HttpPost]
        [Route("api/notifications/mark-all-read")]
        [Route("Notification/MarkAllRead")]
        public async Task<IActionResult> MarkAllRead()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            await _notifRepo.MarkAllAsReadAsync(user.Id);
            return Json(new { success = true, unreadCount = 0 });
        }

        [HttpPost]
        [Route("api/notifications/delete/{id}")]
        [Route("Notification/Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            await _notifRepo.DeleteAsync(id, user.Id);
            int unread = await _notifRepo.GetUnreadCountAsync(user.Id);

            return Json(new { success = true, unreadCount = unread });
        }

        [HttpPost]
        [Route("api/notifications/clear-all")]
        [Route("Notification/ClearAll")]
        public async Task<IActionResult> ClearAll()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            await _notifRepo.ClearAllAsync(user.Id);
            return Json(new { success = true, unreadCount = 0 });
        }
    }
}
