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
    public IActionResult Profile()
    {
        return RedirectToAction("Index");
    }
}
