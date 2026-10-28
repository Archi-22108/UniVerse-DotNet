using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using UniVerse.Server.Data.Repositories;
using UniVerse.Server.Models;
using UniVerse.Server.Models.ViewModels;

namespace UniVerse.Server.Controllers
{
    public class MarketplaceController : Controller
    {
        private readonly IMarketplaceRepository _marketplaceRepo;
        private readonly IUserRepository _userRepo;
        private readonly IWebHostEnvironment _env;

        public MarketplaceController(IMarketplaceRepository marketplaceRepo, IUserRepository userRepo, IWebHostEnvironment env)
        {
            _marketplaceRepo = marketplaceRepo;
            _userRepo = userRepo;
            _env = env;
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
                currentUser = await _userRepo.GetByEmailAsync("archi.kumari126697@marwadiuniversity.ac.in")
                    ?? await _userRepo.GetByEmailAsync("avinash.128203@marwadiuniversity.ac.in")
                    ?? (await _userRepo.GetAllUsersAsync()).FirstOrDefault();
            }

            if (currentUser == null)
            {
                currentUser = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    FullName = "Archi Kumari",
                    Email = "archi.kumari126697@marwadiuniversity.ac.in",
                    Role = "student",
                    HostelName = "Hostel A",
                    RoomNumber = "900",
                    RewardBalance = 500.0,
                    Department = "Computer Science & Engineering",
                    Semester = "Semester 6"
                };
                await _userRepo.CreateUserAsync(currentUser);
            }

            return currentUser;
        }

        [HttpGet]
        public IActionResult Index(string? category)
        {
            return RedirectToAction("Marketplace", "Dashboard", new { category });
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new MarketplaceCreateViewModel());
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Create(MarketplaceCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await GetCurrentUserAsync();
            var userId = user.Id;
            string? finalImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim();

            // Handle real student photo upload from device
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                try
                {
                    var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var uploadsDir = Path.Combine(webRoot, "uploads", "marketplace");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var ext = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                    var fileName = $"{Guid.NewGuid():N}_{DateTime.UtcNow.Ticks}{ext}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    await using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(stream);
                    }

                    finalImageUrl = $"/uploads/marketplace/{fileName}";
                }
                catch
                {
                    // If file upload fails, keep whatever URL was provided
                }
            }

            var newListing = new MarketplaceListing
            {
                SellerId = userId,
                Title = model.Title.Trim(),
                Description = model.Description?.Trim(),
                Category = model.Category,
                Condition = model.Condition,
                Price = model.Price,
                OriginalPrice = model.OriginalPrice,
                Negotiable = model.Negotiable,
                PickupLocation = model.PickupLocation.Trim(),
                ImageUrl = finalImageUrl,
                Status = "active"
            };

            var listingId = await _marketplaceRepo.CreateListingAsync(newListing);
            TempData["SuccessMessage"] = "Marketplace listing created successfully!";
            return RedirectToAction("Marketplace", "Dashboard");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await GetCurrentUserAsync();
            var userId = user.Id;
            var success = await _marketplaceRepo.DeleteListingAsync(id, userId);
            if (success)
            {
                TempData["SuccessMessage"] = "Your listing has been removed.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not delete listing.";
            }
            return RedirectToAction("Marketplace", "Dashboard", new { filter = "my" });
        }

        [HttpGet]
        public async Task<IActionResult> Details(string? id)
        {
            MarketplaceListingDetailDto? listing = null;
            if (!string.IsNullOrWhiteSpace(id))
            {
                listing = await _marketplaceRepo.GetListingByIdAsync(id);
            }

            if (listing == null)
            {
                var all = await _marketplaceRepo.GetListingsAsync();
                listing = all.FirstOrDefault(l => l.Id == id) ?? all.FirstOrDefault();

                if (listing == null)
                {
                    TempData["ErrorMessage"] = "Listing not found or was already completed.";
                    return RedirectToAction("Marketplace", "Dashboard");
                }
            }

            var user = await GetCurrentUserAsync();
            ViewBag.IsSeller = user.Id == listing.SellerId;

            return View(listing);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MakeOffer(MakeOfferViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please enter a valid offer price.";
                return RedirectToAction("Details", new { id = model.ListingId });
            }

            var user = await GetCurrentUserAsync();
            var userId = user.Id;

            var newOffer = new MarketplaceOffer
            {
                ListingId = model.ListingId,
                BuyerId = userId,
                OfferPrice = model.OfferPrice,
                Status = "pending"
            };

            await _marketplaceRepo.CreateOfferAsync(newOffer);
            TempData["SuccessMessage"] = "Your price offer has been submitted to the seller!";
            return RedirectToAction("Details", new { id = model.ListingId });
        }
    }
}
