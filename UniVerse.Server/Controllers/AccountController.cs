using System;
using System.Collections.Generic;
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
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepo;

        public AccountController(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userRepo.GetByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            var passwordHash = DbInitializer.HashPassword(model.Password);
            if (user.PasswordHash != passwordHash)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            await SignInUserAsync(user, model.RememberMe);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> QuickLogin(string email)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user != null)
            {
                await SignInUserAsync(user, true);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var cleanEmail = model.Email.Trim().ToLowerInvariant();
            var existing = await _userRepo.GetByEmailAsync(cleanEmail);
            if (existing != null)
            {
                ModelState.AddModelError("Email", "An account with this email is already registered.");
                return View(model);
            }

            var newUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = cleanEmail,
                PasswordHash = DbInitializer.HashPassword(model.Password),
                FullName = model.FullName.Trim(),
                EnrollmentNumber = model.EnrollmentNumber?.Trim(),
                Role = model.Role.ToLowerInvariant(),
                HostelName = model.HostelName,
                RoomNumber = model.RoomNumber.Trim(),
                PhoneNumber = model.PhoneNumber?.Trim(),
                IsActiveRunner = model.Role.Equals("runner", StringComparison.OrdinalIgnoreCase),
                RewardBalance = 0.0,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                UpdatedAt = DateTime.UtcNow.ToString("o")
            };

            await _userRepo.CreateUserAsync(newUser);
            await SignInUserAsync(newUser, true);

            return RedirectToAction("Profile");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private async Task SignInUserAsync(User user, bool isPersistent)
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
                new("IsActiveRunner", user.IsActiveRunner.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }
    }
}
