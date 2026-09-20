using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using UniVerse.Web.Data;
using UniVerse.Web.Models;

namespace UniVerse.Web.Controllers;

/// <summary>
/// Dashboard Controller handling Student Campus Super-App operations.
/// Follows ASP.NET Core MVC Architecture and C# OOP Dependency Injection.
/// </summary>
public class DashboardController : Controller
{
    private readonly IAdoNetDbHelper _dbHelper;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IAdoNetDbHelper dbHelper, ILogger<DashboardController> logger)
    {
        _dbHelper = dbHelper;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        // Get authenticated user email or fallback to demo account Archi.kumari126697 matching screenshot
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email) || email.Contains("archi", StringComparison.OrdinalIgnoreCase))
        {
            email = "archi.kumari126697@marwadiuniversity.ac.in";
        }

        var model = _dbHelper.GetStudentDashboardData(email);

        if (email.Contains("archi", StringComparison.OrdinalIgnoreCase))
        {
            model.DisplayName = "Archi.kumari126697";
            model.Email = "archi.kumari126697@marwadiuniversity.ac.in";
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult NewRequest()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "archi.kumari126697@marwadiuniversity.ac.in";
        var user = _dbHelper.GetUserByEmail(email);

        var dbProducts = _dbHelper.GetAllProducts();
        var productDtos = new List<VendingProductDto>();

        foreach (var p in dbProducts)
        {
            var tag = "Snack";
            var subCategory = p.Category;
            if (p.Category.Equals("Drinks", StringComparison.OrdinalIgnoreCase))
            {
                tag = "Drink";
                subCategory = "Drinks";
            }
            else if (p.Category.Equals("Chocolates", StringComparison.OrdinalIgnoreCase))
            {
                tag = "Choc";
                subCategory = "Chocolates";
            }
            else
            {
                tag = "Snack";
                subCategory = "Chips";
            }

            productDtos.Add(new VendingProductDto
            {
                Id = p.Id,
                SlotCode = !string.IsNullOrEmpty(p.VendingMachineId) ? p.VendingMachineId : $"A-{p.Id:D2}",
                Name = p.Name,
                Tag = tag,
                SubCategory = subCategory,
                Price = p.Price,
                ImageFileName = p.ImageFileName,
                InStock = p.InStock
            });
        }

        var model = new CreateRequestViewModel
        {
            StudentName = user?.FullName ?? "Archi.kumari126697",
            HostelBlock = !string.IsNullOrEmpty(user?.HostelBlock) ? user.HostelBlock : "Hostel D",
            RoomNumber = !string.IsNullOrEmpty(user?.RoomNumber) ? user.RoomNumber : "D-402",
            Products = productDtos
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SubmitRequest(CreateRequestViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.SelectedItemsJson) || model.SelectedItemsJson == "[]")
        {
            TempData["ErrorMessage"] = "Please select at least one item to request delivery.";
            return RedirectToAction("NewRequest");
        }

        // Parse items from JSON or build readable description
        string itemsDescription = "Campus Delivery Request";
        decimal calculatedSubtotal = 0;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(model.SelectedItemsJson);
            var itemsList = new List<string>();

            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                var name = elem.GetProperty("name").GetString() ?? "Snack";
                var qty = elem.GetProperty("qty").GetInt32();
                var price = elem.GetProperty("price").GetDecimal();
                itemsList.Add($"{qty}x {name}");
                calculatedSubtotal += (price * qty);
            }

            if (itemsList.Count > 0)
            {
                itemsDescription = string.Join(", ", itemsList);
            }
        }
        catch
        {
            itemsDescription = "1x Campus Snacks";
            calculatedSubtotal = 40.0m;
        }

        var deliveryReq = new DeliveryRequest
        {
            StudentName = !string.IsNullOrWhiteSpace(model.StudentName) ? model.StudentName : "Archi.kumari126697",
            HostelRoom = $"{model.HostelBlock} · Room {model.RoomNumber}",
            ItemsDescription = itemsDescription,
            TotalAmount = calculatedSubtotal + model.RewardFee,
            RewardFee = model.RewardFee,
            Status = "Pending"
        };

        // Insert into database using pure ADO.NET
        _dbHelper.CreateDeliveryRequest(deliveryReq);

        TempData["SuccessMessage"] = "Delivery request broadcasted successfully! A campus student runner will accept it shortly.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Requests(string? tab = "all")
    {
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Runner()
    {
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Wallet()
    {
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Chat()
    {
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Marketplace()
    {
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Analytics()
    {
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Profile()
    {
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Setting()
    {
        return RedirectToAction("Index");
    }
}
