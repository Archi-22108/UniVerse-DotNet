using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UniVerse.Server.Data.Repositories;

namespace UniVerse.Server.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDeliveryRepository _deliveryRepo;
        private readonly IMarketplaceRepository _marketplaceRepo;
        private readonly IUserRepository _userRepo;

        public HomeController(
            IDeliveryRepository deliveryRepo, 
            IMarketplaceRepository marketplaceRepo, 
            IUserRepository userRepo)
        {
            _deliveryRepo = deliveryRepo;
            _marketplaceRepo = marketplaceRepo;
            _userRepo = userRepo;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userRepo.GetAllUsersAsync();
            var requests = await _deliveryRepo.GetRequestsAsync();
            var listings = await _marketplaceRepo.GetListingsAsync();
            var runners = await _userRepo.GetActiveRunnersAsync();

            ViewBag.TotalUsers = users.Count;
            ViewBag.TotalRequests = requests.Count;
            ViewBag.TotalListings = listings.Count;
            ViewBag.ActiveRunners = runners.Count;

            ViewBag.RecentRequests = requests;
            ViewBag.RecentListings = listings;

            return View();
        }
    }
}
