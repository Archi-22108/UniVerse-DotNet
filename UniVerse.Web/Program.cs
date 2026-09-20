using UniVerse.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Register ADO.NET Data Access Layer with Dependency Injection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=universe.db";

builder.Services.AddSingleton(new AdoNetDbHelper(connectionString));

var app = builder.Build();

// Auto-initialize SQLite schema and seed sample data on startup
using (var scope = app.Services.CreateScope())
{
    var dbHelper = scope.ServiceProvider.GetRequiredService<AdoNetDbHelper>();
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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
