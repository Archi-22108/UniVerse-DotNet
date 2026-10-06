using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UniVerse.Server.Data.Repositories;
using UniVerse.Server.Models.ViewModels;

namespace UniVerse.Server.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IDeliveryRepository _deliveryRepo;
        private readonly IUserRepository _userRepo;
        private readonly IMarketplaceRepository _marketRepo;

        public DashboardController(
            IDeliveryRepository deliveryRepo,
            IUserRepository userRepo,
            IMarketplaceRepository marketRepo)
        {
            _deliveryRepo = deliveryRepo;
            _userRepo = userRepo;
            _marketRepo = marketRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Models.User? currentUser = null;

            if (!string.IsNullOrEmpty(userId))
            {
                currentUser = await _userRepo.GetByIdAsync(userId);
            }

            // Fallback for campus demo student if guest / not logged in
            if (currentUser == null)
            {
                var allUsers = await _userRepo.GetAllUsersAsync();
                currentUser = allUsers.FirstOrDefault() ?? new Models.User
                {
                    FullName = "Aarav Patel",
                    Email = "aarav.patel@marwadiuniversity.ac.in",
                    Role = "student",
                    HostelName = "Hostel D",
                    RoomNumber = "304",
                    RewardBalance = 240.0
                };
            }

            var requests = await _deliveryRepo.GetRequestsAsync();
            var myRequests = string.IsNullOrEmpty(userId)
                ? requests
                : requests.Where(r => r.RequesterId == userId || r.RunnerId == userId).ToList();

            if (!myRequests.Any() && requests.Any())
            {
                // Show campus feed so dashboard displays authentic orders
                myRequests = requests;
            }

            var listings = await _marketRepo.GetListingsAsync();

            var vm = new DashboardViewModel
            {
                CurrentUser = currentUser,
                AllRequests = myRequests,
                ActiveRequests = myRequests.Where(r => r.Status != "delivered" && r.Status != "cancelled").ToList(),
                CompletedRequests = myRequests.Where(r => r.Status == "delivered").ToList(),
                CancelledRequests = myRequests.Where(r => r.Status == "cancelled").ToList(),
                RecentListings = listings.Take(6).ToList()
            };

            return View(vm);
        }
    }
}
