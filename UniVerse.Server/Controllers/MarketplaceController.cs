using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IWebHostEnvironment _env;

        public MarketplaceController(IMarketplaceRepository marketplaceRepo, IWebHostEnvironment env)
        {
            _marketplaceRepo = marketplaceRepo;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? category)
        {
            var listings = await _marketplaceRepo.GetListingsAsync(category);
            ViewBag.CurrentCategory = category ?? "All";
            return View(listings);
        }

        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new MarketplaceCreateViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MarketplaceCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
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
        public async Task<IActionResult> Details(string id)
        {
            var listing = await _marketplaceRepo.GetListingByIdAsync(id);
            if (listing == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.IsSeller = currentUserId == listing.SellerId;

            return View(listing);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeOffer(MakeOfferViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please enter a valid offer price.";
                return RedirectToAction("Details", new { id = model.ListingId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

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
