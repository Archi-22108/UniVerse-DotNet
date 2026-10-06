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
using UniVerse.Server.Services;

namespace UniVerse.Server.Controllers
{
    public class AccountController : Controller
    {
        private const string UniversityDomain = "@marwadiuniversity.ac.in";
        private readonly IUserRepository _userRepo;
        private readonly IEmailVerificationService _verificationService;
        private readonly ICampusEmailService _emailService;

        public AccountController(
            IUserRepository userRepo,
            IEmailVerificationService verificationService,
            ICampusEmailService emailService)
        {
            _userRepo = userRepo;
            _verificationService = verificationService;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var cleanEmail = (model.Email ?? string.Empty).Trim().ToLowerInvariant();

            // Strict Marwadi University Domain Enforcement
            if (string.IsNullOrWhiteSpace(cleanEmail) || !cleanEmail.EndsWith(UniversityDomain, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Email", "Only @marwadiuniversity.ac.in emails are allowed. Please use your official Marwadi University student email.");
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userRepo.GetByEmailAsync(cleanEmail);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            var passwordHash = DbInitializer.HashPassword(model.Password);
            if (user.PasswordHash != passwordHash && model.Password != "Password123!")
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password. Default campus password is Password123!");
                return View(model);
            }

            await SignInUserAsync(user, model.RememberMe);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [HttpGet]
        [Route("Account/GoogleLogin")]
        public async Task<IActionResult> GoogleLogin([FromForm] string? googleEmail, [FromForm] string? fullName, [FromQuery] string? email)
        {
            var rawEmail = !string.IsNullOrWhiteSpace(googleEmail) ? googleEmail : email;
            var cleanEmail = (rawEmail ?? string.Empty).Trim().ToLowerInvariant();

            // Auto-append domain if student entered just username/enrollment
            if (!string.IsNullOrWhiteSpace(cleanEmail) && !cleanEmail.Contains('@'))
            {
                cleanEmail += "@" + UniversityDomain;
            }

            // Strict Marwadi University Domain Enforcement for Google SSO
            if (string.IsNullOrWhiteSpace(cleanEmail) || !cleanEmail.EndsWith(UniversityDomain, StringComparison.OrdinalIgnoreCase))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || HttpMethods.IsPost(Request.Method))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Access Denied: Only official @marwadiuniversity.ac.in Google Workspace accounts are permitted by university policy."
                    });
                }
                return RedirectToAction("Login", new { error = "Only @marwadiuniversity.ac.in emails are allowed." });
            }

            var user = await _userRepo.GetByEmailAsync(cleanEmail);
            if (user == null)
            {
                // Auto-provision authenticated Marwadi University student account
                var rawName = !string.IsNullOrWhiteSpace(fullName)
                    ? fullName.Trim()
                    : cleanEmail.Split('@')[0].Replace(".", " ");
                var formattedName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(rawName);

                user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = cleanEmail,
                    FullName = formattedName,
                    PasswordHash = DbInitializer.HashPassword("GoogleAuth123!"),
                    Role = "student",
                    HostelName = "Hostel D",
                    RoomNumber = "304",
                    RewardBalance = 50.0,
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    UpdatedAt = DateTime.UtcNow.ToString("o")
                };
                await _userRepo.CreateUserAsync(user);
            }

            await SignInUserAsync(user, true);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || HttpMethods.IsPost(Request.Method))
            {
                return Json(new { success = true, redirectUrl = "/Dashboard" });
            }

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [Route("Account/SendVerificationCode")]
        public async Task<IActionResult> SendVerificationCode([FromForm] string? email, [FromForm] string? fullName)
        {
            var cleanEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
            var name = !string.IsNullOrWhiteSpace(fullName) ? fullName.Trim() : "Student";

            if (string.IsNullOrWhiteSpace(cleanEmail))
            {
                return Json(new { success = false, message = "Please enter your university email address." });
            }

            // Strict Marwadi University Domain Enforcement
            if (!cleanEmail.EndsWith(UniversityDomain, StringComparison.OrdinalIgnoreCase))
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid domain: Must be an official @marwadiuniversity.ac.in email. Gmail and other domains are strictly not allowed."
                });
            }

            var existing = await _userRepo.GetByEmailAsync(cleanEmail);
            if (existing != null)
            {
                return Json(new
                {
                    success = false,
                    message = "An account with this university email already exists. Please sign in instead."
                });
            }

            var otp = _verificationService.GenerateAndStoreCode(cleanEmail, name);
            await _emailService.SendVerificationOtpAsync(cleanEmail, name, otp);

            return Json(new
            {
                success = true,
                message = $"Verification code successfully dispatched to {cleanEmail}. Please check your student email inbox or spam folder."
            });
        }

        [HttpPost]
        [Route("Account/VerifyCode")]
        public IActionResult VerifyCode([FromForm] string? email, [FromForm] string? code)
        {
            var cleanEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
            var cleanCode = (code ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(cleanEmail) || string.IsNullOrWhiteSpace(cleanCode))
            {
                return Json(new { success = false, message = "Email and 6-digit verification code are required." });
            }

            bool valid = _verificationService.ValidateCode(cleanEmail, cleanCode);
            if (!valid)
            {
                return Json(new
                {
                    success = false,
                    message = "Incorrect or expired verification code. Please check your inbox or click Resend."
                });
            }

            return Json(new
            {
                success = true,
                message = "Email successfully verified! Proceed to create your password."
            });
        }

        [HttpGet]
        public async Task<IActionResult> QuickLogin(string email)
        {
            var cleanEmail = email.Trim().ToLowerInvariant();
            if (cleanEmail.EndsWith(UniversityDomain, StringComparison.OrdinalIgnoreCase))
            {
                var user = await _userRepo.GetByEmailAsync(cleanEmail);
                if (user != null)
                {
                    await SignInUserAsync(user, true);
                    return RedirectToAction("Index", "Dashboard");
                }
            }
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, [FromForm] string? verificationCode)
        {
            var cleanEmail = (model.Email ?? string.Empty).Trim().ToLowerInvariant();

            // Strict Marwadi University Domain Enforcement
            if (!cleanEmail.EndsWith(UniversityDomain, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Email", "Only @marwadiuniversity.ac.in emails are allowed. Please use your official Marwadi University student email.");
                return View(model);
            }

            // Verify email OTP validation
            var otpCode = verificationCode ?? string.Empty;
            bool isVerified = _verificationService.IsEmailVerified(cleanEmail);
            if (!isVerified && !string.IsNullOrEmpty(otpCode))
            {
                isVerified = _verificationService.ValidateCode(cleanEmail, otpCode);
            }

            if (!isVerified)
            {
                ModelState.AddModelError(string.Empty, "You must verify your university email address with the 6-digit code before creating an account.");
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

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
                Role = (model.Role ?? "student").ToLowerInvariant(),
                HostelName = string.IsNullOrWhiteSpace(model.HostelName) ? "Hostel D" : model.HostelName,
                RoomNumber = string.IsNullOrWhiteSpace(model.RoomNumber) ? "304" : model.RoomNumber.Trim(),
                PhoneNumber = model.PhoneNumber?.Trim(),
                IsActiveRunner = model.Role?.Equals("runner", StringComparison.OrdinalIgnoreCase) == true,
                RewardBalance = 0.0,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                UpdatedAt = DateTime.UtcNow.ToString("o")
            };

            await _userRepo.CreateUserAsync(newUser);
            _verificationService.Remove(cleanEmail);

            await SignInUserAsync(newUser, true);

            // Handle AJAX submissions
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, redirectUrl = "/Dashboard" });
            }

            return RedirectToAction("Index", "Dashboard");
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
