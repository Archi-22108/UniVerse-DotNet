using Microsoft.AspNetCore.Authentication.Cookies;
using UniVerse.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add MVC Controllers and Views
builder.Services.AddControllersWithViews();

// Register ADO.NET Data Access Layer via Interface for C# OOP Dependency Injection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=universe.db";

var dbHelperInstance = new AdoNetDbHelper(connectionString);
builder.Services.AddSingleton<IAdoNetDbHelper>(dbHelperInstance);
builder.Services.AddSingleton<AdoNetDbHelper>(dbHelperInstance);

// Add ASP.NET Core Session Management (Course requirement)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add ASP.NET Core Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Auto-initialize SQLite schema and seed sample data on startup via ADO.NET
using (var scope = app.Services.CreateScope())
{
    var dbHelper = scope.ServiceProvider.GetRequiredService<IAdoNetDbHelper>();
    dbHelper.InitializeDatabase();
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Session Middleware (must be before Authentication/Authorization)
app.UseSession();

// Authentication & Authorization middlewares
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
